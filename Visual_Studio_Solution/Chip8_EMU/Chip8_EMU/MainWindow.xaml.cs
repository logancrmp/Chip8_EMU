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
            EmuRunner Runner = new EmuRunner();

            Runner.RunEmulator(this);
        }
    }
}
