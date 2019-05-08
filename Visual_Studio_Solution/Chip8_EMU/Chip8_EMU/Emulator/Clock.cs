using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8_EMU.Emulator
{
    enum TimerTypeEnum
    {
        TimerInvalid,
        TimerOneShot,   // Count down to 0, trigger callback, halt
        TimerCyclic,    // Count down to 0, trigger callback, set timer for next period
    };


    public delegate void TimerFncPtrType();


    static class Clock
    {
        private static Dictionary<int, Timer> Timers = new Dictionary<int, Timer>();
        private static int TimerHandleCounter = 0;

        private static DateTime TimeZeroOffset = DateTime.UtcNow;
        private static Stopwatch ClockSource = new Stopwatch();

        private static ulong ClockTime = 0;


        internal static int AddTimer(TimerFncPtrType TimerNotification)
        {
            Timer NewTimer = new Timer();

            NewTimer.TimerNotification = TimerNotification;
            NewTimer.TimerActive = false;

            Timers[TimerHandleCounter] = NewTimer;

            return TimerHandleCounter++;
        }


        internal static void StartTimerOneShot(int TimerHandle, ulong TimeoutNanoSeconds)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                Timers[TimerHandle].TimerType = TimerTypeEnum.TimerOneShot;
                Timers[TimerHandle].SkipMissedDeadlines = false;
                Timers[TimerHandle].TimeoutValue = 0;
                Timers[TimerHandle].TimerActive = true;

                Timers[TimerHandle].NextDeadline = GetRealTimeNow() + TimeoutNanoSeconds;
                Timers[TimerHandle].DeadlineHandled = false;
            }
        }


        internal static void StartTimerCyclic(int TimerHandle, ulong TimeoutNanoSeconds, bool SkipMissedDeadlines)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                Timers[TimerHandle].TimerType = TimerTypeEnum.TimerCyclic;
                Timers[TimerHandle].SkipMissedDeadlines = SkipMissedDeadlines;
                Timers[TimerHandle].TimeoutValue = TimeoutNanoSeconds;
                Timers[TimerHandle].TimerActive = true;

                Timers[TimerHandle].NextDeadline = GetRealTimeNow() + TimeoutNanoSeconds;
                Timers[TimerHandle].DeadlineHandled = false;
            }
        }


        internal static ulong GetNextRealtimeDeadline(int TimerHandle)
        {
            ulong Deadline = 0;

            if (Timers.ContainsKey(TimerHandle))
            {
                // find the next deadline for the timer that is greater than the current clock time
                var ClockMod = ClockTime % Timers[TimerHandle].TimeoutValue;

                if (ClockMod == 0)
                {
                    Deadline = ClockTime + Timers[TimerHandle].TimeoutValue;
                }
                else
                {
                    Deadline = ClockTime + (Timers[TimerHandle].TimeoutValue - ClockMod);
                }
            }

            return Deadline;
        }


        internal static void StartTimer(int TimerHandle)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                Timers[TimerHandle].TimerActive = true;
            }
        }


        internal static void StopTimer(int TimerHandle)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                Timers[TimerHandle].TimerActive = false;
            }
        }


        internal static void RunClock()
        {
            int TimerExecCntr = 0;

            ClockSource.Start();

            foreach (var timer in Timers.Values)
            {
                if (timer.TimerType == TimerTypeEnum.TimerCyclic)
                {
                    timer.NextDeadline = GetRealTimeNow() + timer.TimeoutValue;
                    timer.DeadlineHandled = false;
                }
                else
                {
                    timer.NextDeadline = GetRealTimeNow() + timer.TimeoutValue;
                    timer.DeadlineHandled = false;
                }
            }
            
            while (true)
            {
                // set ClockTime to the number of nanoseconds since time 0
                ClockTime = GetRealTimeNow();

                foreach (var timer in Timers.Values)
                {
                    if (timer.TimerActive == false)
                    {
                        continue;
                    }

                    TimerExecCntr = 0;

                    // timer will execute as many deadlines as possible until either the timer has caught
                    // up with real time, or (1/X0)th of a second of cpu time has been emulated
                    while (ClockTime >= timer.NextDeadline && TimerExecCntr < (SystemConfig.CPU_FREQ >> 7))
                    {
                        TimerExecCntr++;
                        timer.DeadlineHandled = true;

                        timer.TimerNotification?.Invoke();

                        // if the timer is a repeating timer, reset it for its next deadline
                        if (timer.TimerType == TimerTypeEnum.TimerCyclic)
                        {
                            if (timer.SkipMissedDeadlines)
                            {
                                // find the next deadline for the timer that is greater than the current clock time
                                var ClockMod = ClockTime % timer.TimeoutValue;

                                if (ClockMod == 0)
                                {
                                    timer.NextDeadline = ClockTime + timer.TimeoutValue;
                                }
                                else
                                {
                                    timer.NextDeadline = ClockTime + (timer.TimeoutValue - ClockMod);
                                }
                            }
                            else
                            {
                                // increment from previous deadline
                                timer.NextDeadline += timer.TimeoutValue;
                            }

                            // clear deadline handled flag
                            timer.DeadlineHandled = false;
                        }
                        else
                        {
                            // only continue looping if the current timer is not a repeating timer
                            break;
                        }
                    }
                }

                // sleep for a few ms. Gives graphics thread time to
                // run, and cpu will catch up on next timer exec
                if (SystemConfig.PERFORMANCE_LEVEL > 0)
                {
                    System.Threading.Thread.Sleep(SystemConfig.PERFORMANCE_LEVEL - 1);
                }
            }
        }


        internal static ulong GetRealTimeNow()
        {
            return (ulong)(( ClockSource.ElapsedTicks * SystemConst.ONE_BILLION ) / Stopwatch.Frequency);
        }


        internal static ulong GetTimeNow()
        {
            return ClockTime;
        }
    }


    class Timer
    {
        internal TimerTypeEnum TimerType;

        internal bool TimerActive;

        // next deadline in absolute nanoseconds from time 0
        internal ulong NextDeadline;
        internal bool DeadlineHandled;

        // deadline time in nanoseconds from when the timer is started
        internal ulong TimeoutValue;

        // 
        internal bool SkipMissedDeadlines;

        internal TimerFncPtrType TimerNotification;
    }
}