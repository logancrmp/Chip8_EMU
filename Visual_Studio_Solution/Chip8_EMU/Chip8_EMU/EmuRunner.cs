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
        private BackgroundWorker EmuWorker = null;

        internal Chip8 System;

        internal void RunEmulator(MainWindow DisplayWindow)
        {
            if (EmuWorker == null)
            {
                System = new Chip8(DisplayWindow);

                EmuWorker = new BackgroundWorker();
                EmuWorker.DoWork += DoWork;
                EmuWorker.RunWorkerAsync();
            }
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            System.Run();
        }
    }
}
