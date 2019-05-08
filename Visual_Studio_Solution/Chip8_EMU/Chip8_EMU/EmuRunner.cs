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

        internal static MMU C8_MMU = new MMU();
        internal static CPU C8_CPU = new CPU();
        internal static Screen C8_Screen = new Screen();
        internal static Keyboard C8_Keyboard = new Keyboard();
        internal static Clock C8_Clock = new Clock();

        internal static void RunEmulator(MainWindow DisplayWindow)
        {
            if (EmuWorker == null)
            {
                EmuWorker = new BackgroundWorker();

                EmuWorker.DoWork += DoWork;

                bool RomLoaded = C8_MMU.InitMemory();
                C8_CPU.InitCPU();
                C8_Screen.InitScreen(DisplayWindow);
                C8_Keyboard.InitKeyboard();

                EmuWorker.RunWorkerAsync();
            }
        }

        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            C8_Clock.RunClock();
        }
    }
}
