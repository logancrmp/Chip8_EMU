using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chip8_EMU.Emulator
{
    internal class Keyboard
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        private ulong[] LastCheckTime = new ulong[0x10];
        private bool[] StoredKeyState = new bool[0x10];


        internal Keyboard()
        {
            for (int Iter = 0; Iter < 0x10; Iter += 1)
            {
                LastCheckTime[Iter] = 0;
                StoredKeyState[Iter] = false;
            }
        }


        internal bool IsKeyPressed(int Key)
        {
            ulong TimeNow = EmuRunner.C8_Clock.GetTimeNow();

            // If at least 10ms has passed since the last time the key state was updated, we can update the key state
            // Calling GetKeyState too rapidly causes the emulation thread to slow to a grind
            if ((TimeNow - LastCheckTime[Key]) > (SystemConst.ONE_BILLION / 100))
            {
                // Set last check time to current time
                LastCheckTime[Key] = TimeNow;

                // fetch the current state of the key
                short WindowsKeyState = GetKeyState(SystemConfig.InputKeyMap[Key]);

                // check state
                StoredKeyState[Key] = ((WindowsKeyState & 0x8000) > 0);
            }

            return StoredKeyState[Key];
        }
    }
}
