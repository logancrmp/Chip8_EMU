using Game8.Emulator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game8
{
    class EmuRunner
    {
        private BackgroundWorker EmuWorker = null;
        internal bool EmuRunning { get; private set; } = false;

        internal Chip8 System;

        internal void RunEmulator(MainWindow DisplayWindow)
        {
            if (EmuRunning == false)
            {
                System = new Chip8(DisplayWindow);

                EmuRunning = true;

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
