using Chip8_EMU.Emulator;
using System;
using System.ComponentModel;
using System.Windows;

namespace Chip8_EMU
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        BackgroundWorker EmuRunner = null;



        public MainWindow()
        {
            InitializeComponent();

            CPU_Instructions.ParentWindow = this;
        }


        public void SetLogText(string LogText)
        {
            Dispatcher.Invoke(new Action(() => outlog.Text = LogText));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (EmuRunner == null)
            {
                EmuRunner = new BackgroundWorker();

                EmuRunner.DoWork += EmuRunner_DoWork;

                bool RomLoaded = MMU.InitMemory();
                CPU.InitCPU();
                Screen.InitScreen(this);
                Keyboard.InitKeyboard();

                EmuRunner.RunWorkerAsync();
            }
        }

        private void EmuRunner_DoWork(object sender, DoWorkEventArgs e)
        {
            Clock.RunClock();
        }
    }
}
