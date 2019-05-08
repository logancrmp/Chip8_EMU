using Chip8_EMU.Emulator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8_EMU
{
    class EmuRunner
    {
        private static BackgroundWorker EmuWorker = null;

        internal static MMU C8_MMU;
        internal static CPU C8_CPU;
        internal static Screen C8_Screen;
        internal static Keyboard C8_Keyboard;
        internal static Clock C8_Clock;

        internal static void RunEmulator(MainWindow DisplayWindow)
        {
            if (EmuWorker == null)
            {
                C8_MMU = new MMU();
                C8_CPU = new CPU();
                C8_Screen = new Screen(DisplayWindow);
                C8_Keyboard = new Keyboard();
                C8_Clock = new Clock();

                C8_CPU.SetupClocks();
                C8_Screen.SetupClocks();

                EmuWorker = new BackgroundWorker();

                EmuWorker.DoWork += DoWork;

                C8_MMU.LoadRom("ROM.ch8");

                EmuWorker.RunWorkerAsync();
            }
        }

        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            C8_Clock.RunClock();
        }
    }
}
