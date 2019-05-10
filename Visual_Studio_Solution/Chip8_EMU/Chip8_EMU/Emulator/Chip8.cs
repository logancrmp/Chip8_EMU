using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8_EMU.Emulator
{
    class Chip8
    {
        internal Clock Clock;
        internal CPU CPU;
        internal MMU MMU;
        internal Screen Screen;
        internal Keyboard Keyboard;

        internal Chip8(MainWindow DisplayWindow)
        {
            Clock = new Clock();

            CPU = new CPU(this);
            MMU = new MMU(this);
            Keyboard = new Keyboard(this);
            Screen = new Screen(this, DisplayWindow);

            CPU.SetupClocks();
            Screen.SetupClocks();

            MMU.LoadRom("ROM.ch8");
        }


        internal void Run()
        {
            Clock.RunClock();
        }
    }
}
