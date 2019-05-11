using System.Collections.Generic;
using System.Diagnostics;

namespace Chip8_EMU.Emulator
{
    enum TimerTypeEnum
    {
        TimerInvalid,
        TimerOneShot,   // Count down to 0, trigger callback, halt
        TimerCyclic,    // Count down to 0, trigger callback, set timer for next period
    };


    public delegate void TimerFncPtrType();


    internal class Clock
    {
        private Dictionary<int, Timer> Timers = new Dictionary<int, Timer>();
        private int TimerHandleCounter = 0;

        private Stopwatch ClockSource = new Stopwatch();

        private ulong ClockTime = 0;


        internal int AddTimer(TimerFncPtrType TimerNotification)
        {
            Timer NewTimer = new Timer();

            NewTimer.TimerNotification = TimerNotification;
            NewTimer.TimerActive = false;
            NewTimer.TimerHandle = TimerHandleCounter;

            Timers[TimerHandleCounter] = NewTimer;

            return TimerHandleCounter++;
        }


        internal void StartTimerOneShot(int TimerHandle, ulong TimeoutNanoSeconds)
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


        internal void StartTimerCyclic(int TimerHandle, ulong TimeoutNanoSeconds, bool SkipMissedDeadlines)
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


        internal ulong GetNextRealtimeDeadline(int TimerHandle)
        {
            ulong Deadline = 0;

            if (Timers.ContainsKey(TimerHandle))
            {
                // find the next deadline for the timer that is greater than the current clock time
                var Offset = ClockTime - Timers[TimerHandle].NextDeadline;

                Deadline = ClockTime + (Timers[TimerHandle].TimeoutValue - (Offset % Timers[TimerHandle].TimeoutValue));
            }

            return Deadline;
        }


        internal void StartTimer(int TimerHandle)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                Timers[TimerHandle].TimerActive = true;
            }
        }


        internal void StopTimer(int TimerHandle)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                Timers[TimerHandle].TimerActive = false;
            }
        }

        /*
            Need to add a pause that pauses all timers in the system.
            The timers should be able to be unpaused, and upon resuming,
            should pick up where they left off. If a timer was 75% of the
            way to its timeout, when resumed it should be loaded with 25%
            of its timeout value

            
            void pause()
            {
                // save real time

                // loop over timers

                    // save previous active state
                    // set to inactive
                    // save offset from saved real time
            }

            void resume()
            {
                // set clock time to real time

                // loop over timers

                    // restore previous active state
                    // set timeout value = clock time - saved offset
            }
        */

        internal void RunClock()
        {
            int TimerExecCntr = 0;

            ClockSource.Start();

            foreach (var timer in Timers.Values)
            {
                timer.NextDeadline = GetRealTimeNow() + timer.TimeoutValue;
                timer.DeadlineHandled = false;
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

                    // timer will execute as many deadlines as possible until either the timer
                    // has caught up with real time, or at most 1ms of cpu time has been emulated
                    while (ClockTime >= timer.NextDeadline && TimerExecCntr < (SystemConfig.CPU_FREQ / 1000))
                    {
                        TimerExecCntr++;
                        timer.DeadlineHandled = true;

                        timer.TimerNotification?.Invoke();

                        // if the timer is a repeating timer, reset it for its next deadline
                        if (timer.TimerType == TimerTypeEnum.TimerCyclic)
                        {
                            if (timer.SkipMissedDeadlines)
                            {
                                timer.NextDeadline = GetNextRealtimeDeadline(timer.TimerHandle);
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


        internal ulong GetRealTimeNow()
        {
            return (ulong)((ClockSource.ElapsedTicks * SystemConst.ONE_BILLION) / Stopwatch.Frequency);
        }


        internal ulong GetTimeNow()
        {
            return ClockTime;
        }
    }


    class Timer
    {
        internal TimerTypeEnum TimerType;
        internal int TimerHandle;

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