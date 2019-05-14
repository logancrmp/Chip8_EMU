using Game8.Emulator;
using System.ComponentModel;

namespace Game8
{
    class EmuRunner
    {
        private BackgroundWorker EmuWorker = null;
        internal bool EmuRunning { get; private set; } = false;

        internal Gameboy System { get; private set; }


        internal void RunEmulator(MainWindow DisplayWindow)
        {
            if (EmuRunning == false)
            {
                System = new Gameboy(DisplayWindow);

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
