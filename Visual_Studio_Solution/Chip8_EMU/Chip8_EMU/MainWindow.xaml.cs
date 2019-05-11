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
        EmuRunner Runner = new EmuRunner();

        public MainWindow()
        {
            InitializeComponent();
        }


        public void SetLogText(string LogText)
        {
            Dispatcher.Invoke(new Action(() => outlog.Text = LogText));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Runner.RunEmulator(this);
        }


        private void Button_Click_Pause(object sender, RoutedEventArgs e)
        {
            if (Runner.System.Clock.ClockState == ClockStateEnum.ClockPaused)
            {
                Runner.System.Clock.ResumeClock();
            }
            else
            if (Runner.System.Clock.ClockState == ClockStateEnum.ClockRunning)
            {
                Runner.System.Clock.PauseClock();
            }
        }
    }
}
