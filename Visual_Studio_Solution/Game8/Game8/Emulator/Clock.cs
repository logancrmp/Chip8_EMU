using System.Collections.Generic;
using System.Diagnostics;

namespace Game8.Emulator
{
    enum TimerTypeEnum
    {
        TimerOneShot,   // Count down to 0, trigger callback, halt
        TimerCyclic,    // Count down to 0, trigger callback, set timer for next period
    };

    enum ClockStateEnum
    {
        ClockNotStarted,
        ClockRunning,
        ClockPaused,
    };


    public delegate void TimerFncPtrType();


    internal class Clock
    {
        private Dictionary<int, Timer> Timers = new Dictionary<int, Timer>();
        private int TimerHandleCounter = 0;
        private const int MaxTimerExec = (SystemConfig.PERFORMANCE_LEVEL >= 2) ? 100 : 1000;

        private Stopwatch ClockSource = new Stopwatch();

        private ulong ClockTime = 0;

        internal ClockStateEnum ClockState { get; private set; } = ClockStateEnum.ClockNotStarted;


        internal int AddTimer(TimerFncPtrType TimerNotification)
        {
            Timer NewTimer = new Timer(this);

            NewTimer.TimerNotification = TimerNotification;
            NewTimer.TimerActive = false;
            NewTimer.TimerHandle = TimerHandleCounter;

            Timers[TimerHandleCounter] = NewTimer;

            return TimerHandleCounter++;
        }


        internal Timer GetTimer(int TimerHandle)
        {
            if (Timers.ContainsKey(TimerHandle))
            {
                return Timers[TimerHandle];
            }

            return null;
        }

        
        internal void PauseClock()
        {
            if (ClockState == ClockStateEnum.ClockRunning)
            {
                ClockState = ClockStateEnum.ClockPaused;

                // This is called from the GUI thread.
                // To avoid having to use locks, sleep
                // and give time for the clock thread
                // to recognize it has been paused
                System.Threading.Thread.Sleep(5);

                // at this point in time, ClockTime holds the most
                // recent executed nanosecond of the paused system. 

                foreach (var timer in Timers.Values)
                {
                    if (timer.TimerActive)
                    {
                        ulong NumberOfMissedDeadlines = (ClockTime - timer.NextDeadline) / timer.TimeoutValue;
                        ulong CompletedTimerTicks = (ClockTime - timer.NextDeadline) % timer.TimeoutValue;

                        if (timer.SkipMissedDeadlines)
                        {
                            NumberOfMissedDeadlines = 0;
                        }

                        timer.TimerActiveTicksBehindRealTime = (NumberOfMissedDeadlines * timer.TimeoutValue);
                        timer.TimerActiveNextDeadlineOffset = (timer.TimeoutValue - CompletedTimerTicks);
                    }

                    timer.TimerActiveResumeState = timer.TimerActive;
                    timer.TimerActive = false;
                }
            }
        }


        internal void ResumeClock()
        {
            if (ClockState == ClockStateEnum.ClockPaused)
            {
                ClockState = ClockStateEnum.ClockRunning;

                ClockTime = GetRealTimeNow();

                foreach (var timer in Timers.Values)
                {
                    if (timer.TimerActiveResumeState == true)
                    {
                        // resume the timer using the starting ClockTime, plus the portion
                        // of the timer that was not executed when the clock was paused,
                        // minus the timers real time offset from when it was paused
                        timer.NextDeadline = ClockTime;
                        timer.NextDeadline += timer.TimerActiveNextDeadlineOffset;
                        timer.NextDeadline -= timer.TimerActiveTicksBehindRealTime;

                        timer.DeadlineHandled = false;
                        timer.TimerActiveResumeState = false;
                        timer.TimerActive = true;
                    }
                }
            }
        }


        internal void RunClock()
        {
            int TimerExecCntr = 0;

            ClockState = ClockStateEnum.ClockRunning;
            ClockSource.Start();

            ulong StartTime = GetRealTimeNow();

            foreach (var timer in Timers.Values)
            {
                timer.NextDeadline = StartTime + timer.TimeoutValue;
                timer.DeadlineHandled = false;
            }

            while (true)
            {
                // Sleep and wait until the clock is resumed
                while (ClockState == ClockStateEnum.ClockPaused)
                {
                    System.Threading.Thread.Sleep(1);
                }

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
                    // has caught up with real time, or at most X ms of cpu time has been emulated
                    while (ClockTime >= timer.NextDeadline && TimerExecCntr < (SystemConfig.CPU_FREQ / MaxTimerExec))
                    {
                        TimerExecCntr++;
                        timer.DeadlineHandled = true;

                        timer.TimerNotification?.Invoke();

                        // if the timer is a repeating timer, reset it for its next deadline
                        if (timer.TimerType == TimerTypeEnum.TimerCyclic)
                        {
                            if (timer.SkipMissedDeadlines)
                            {
                                if ((timer.NextDeadline + timer.TimeoutValue) >= ClockTime)
                                {
                                    timer.NextDeadline += timer.TimeoutValue;
                                }
                                else
                                {
                                    timer.NextDeadline = timer.GetNextDeadline();
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


        /// <summary>
        /// Gets the number of elapsed nanseconds, as accurately as possible
        /// given the frequency of the underlying source clock.
        /// </summary>
        /// <returns>System real time in nanoseconds</returns>
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
        private Clock ParentClock;

        internal TimerTypeEnum TimerType;
        internal int TimerHandle;

        internal bool TimerActive;
        internal bool TimerActiveResumeState;
        internal ulong TimerActiveTicksBehindRealTime;
        internal ulong TimerActiveNextDeadlineOffset;

        // next deadline in absolute nanoseconds from time 0
        internal ulong NextDeadline;
        internal bool DeadlineHandled;

        // deadline time in nanoseconds from when the timer is started
        internal ulong TimeoutValue;

        internal bool SkipMissedDeadlines;

        internal TimerFncPtrType TimerNotification;


        internal Timer(Clock ParentClock)
        {
            this.ParentClock = ParentClock;
        }


        internal bool SetTimerOneShot(ulong TimeoutNanoSeconds)
        {
            bool Success = false;

            if (TimeoutNanoSeconds > 0)
            {
                Success = true;

                TimerType = TimerTypeEnum.TimerOneShot;
                SkipMissedDeadlines = false;
                TimeoutValue = TimeoutNanoSeconds;
            }

            return Success;
        }


        internal bool SetTimerCyclic(ulong TimeoutNanoSeconds, bool SkipMissedDeadlines)
        {
            bool Success = false;

            if (TimeoutNanoSeconds > 0)
            {
                Success = true;

                TimerType = TimerTypeEnum.TimerCyclic;
                this.SkipMissedDeadlines = SkipMissedDeadlines;
                TimeoutValue = TimeoutNanoSeconds;
            }

            return Success;
        }


        internal bool StartTimer()
        {
            bool Success = false;

            if (TimeoutValue > 0)
            {
                Success = true;

                NextDeadline = ParentClock.GetRealTimeNow() + TimeoutValue;
                DeadlineHandled = false;

                TimerActive = true;
            }

            return Success;
        }


        internal void StopTimer()
        {
            TimerActive = false;
        }


        internal ulong GetNextDeadline()
        {
            ulong Deadline = 0;

            ulong TimeNow = ParentClock.GetTimeNow();

            // find the next deadline for the timer that is greater than the current clock time
            var Offset = TimeNow - NextDeadline;

            Deadline = TimeNow + (TimeoutValue - (Offset % TimeoutValue));

            return Deadline;
        }
    }
}