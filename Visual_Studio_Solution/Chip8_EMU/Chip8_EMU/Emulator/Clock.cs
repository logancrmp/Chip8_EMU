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
        TimerOneShot,    // Count down to 0, trigger callback, halt
        TimerRepeating,  // Count down to 0, trigger callback, set timer for next period
    };


    public delegate void TimerFncPtrType();


    static class Clock
    {
        private static Random random = new Random();

        private static Dictionary<int, Timer> Timers = new Dictionary<int, Timer>();
        private static int TimerHandleCounter = 0;

        private static DateTime TimeZeroOffset = DateTime.UtcNow;
        private static Stopwatch ClockSource = new Stopwatch();

        private static ulong ClockTime = 0;


        internal static byte GetRandByte()
        {
            return (byte)random.Next(0, 256);
        }


        internal static int AddTimer(TimerTypeEnum TimerType, ulong FrequencyHZ, TimerFncPtrType TimerNotification, bool SkipMissedDeadlines)
        {
            if (!Timers.ContainsKey(TimerHandleCounter))
            {
                Timer NewTimer = new Timer();

                NewTimer.TimerType = TimerType;
                NewTimer.TimerNotification = TimerNotification;
                NewTimer.SkipMissedDeadlines = SkipMissedDeadlines;
                NewTimer.TimeoutValue = ((ulong)(((double)SystemConfig.ONE_BILLION) / ((double)FrequencyHZ)));

                Timers[TimerHandleCounter] = NewTimer;
            }

            return TimerHandleCounter++;
        }


        internal static void SetTimer(int TimerHandle, ulong TimeoutNanoSeconds)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                if (Timers[TimerHandle].TimerType == TimerTypeEnum.TimerOneShot)
                {
                    Timers[TimerHandle].NextDeadline = GetRealTimeNow() + TimeoutNanoSeconds;
                    Timers[TimerHandle].DeadlineHandled = false;
                }
            }
        }


        internal static ulong GetTimerDeadline(int TimerHandle)
        {
            return Timers[TimerHandle].NextDeadline;
        }


        internal static void StopTimer(int TimerHandle)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                if (Timers[TimerHandle].TimerType == TimerTypeEnum.TimerOneShot)
                {
                    Timers[TimerHandle].NextDeadline = 0;
                    Timers[TimerHandle].DeadlineHandled = true;
                }
            }
        }


        internal static void RunClock()
        {
            foreach (var timer in Timers.Values)
            {
                if (timer.TimerType == TimerTypeEnum.TimerRepeating)
                {
                    timer.NextDeadline = GetRealTimeNow() + timer.TimeoutValue;
                    timer.DeadlineHandled = false;
                }
                else
                {
                    timer.NextDeadline = 0;
                    timer.DeadlineHandled = true;
                }
            }

            ClockSource.Start();
            int TimerExecCntr = 0;

            while (true)
            {
                // set ClockTime to the number of nanoseconds since time 0
                ClockTime = GetRealTimeNow();

                foreach (var timer in Timers.Values)
                {
                    TimerExecCntr = 0;

                    // timer will execute as many deadlines as possible until either the timer has caught
                    // up with real time, or (1/X0)th of a second of cpu time has been emulated
                    while (ClockTime >= timer.NextDeadline && TimerExecCntr < (SystemConfig.CPU_FREQ / (SystemConfig.FRAME_RATE * 2)))
                    {
                        TimerExecCntr++;
                        timer.DeadlineHandled = true;

                        timer.TimerNotification?.Invoke();

                        // if the timer is a repeating timer, reset it for its next deadline
                        if (timer.TimerType == TimerTypeEnum.TimerRepeating)
                        {
                            if (timer.SkipMissedDeadlines)
                            {
                                // increment from time now
                                timer.NextDeadline = (ClockTime + timer.TimeoutValue);
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
                System.Threading.Thread.Sleep(1);
            }
        }


        internal static ulong GetRealTimeNow()
        {
            return (ulong)(( ClockSource.ElapsedTicks * SystemConfig.ONE_BILLION ) / Stopwatch.Frequency);
        }


        internal static ulong GetTimeNow()
        {
            return ClockTime;
        }
    }


    class Timer
    {
        internal TimerTypeEnum TimerType;

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