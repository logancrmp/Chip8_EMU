using Chip8_EMU.Emulator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8_EMU
{
    static class EmuRunner
    {
        private static BackgroundWorker EmuWorker = null;

        internal static void RunEmulator(MainWindow DisplayWindow)
        {
            if (EmuWorker == null)
            {
                EmuWorker = new BackgroundWorker();

                EmuWorker.DoWork += DoWork;

                bool RomLoaded = MMU.InitMemory();
                CPU.InitCPU();
                Screen.InitScreen(DisplayWindow);
                Keyboard.InitKeyboard();

                EmuWorker.RunWorkerAsync();
            }
        }

        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            Clock.RunClock();
        }
    }
}
