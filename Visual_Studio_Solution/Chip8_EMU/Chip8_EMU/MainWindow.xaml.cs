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

            CPU_Instructions.ParentWindow = this;
        }


        public void SetLogText(string LogText)
        {
            Dispatcher.Invoke(new Action(() => outlog.Text = LogText));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // if not run before
            {
                // open browse dialog

                // select rom

                // send rom to MMU, load it

                // get everything ready

                Run();
            }
        }

        
        private void Run()
        {
            EmuRunner.RunEmulator(this);
        }
    }
}
