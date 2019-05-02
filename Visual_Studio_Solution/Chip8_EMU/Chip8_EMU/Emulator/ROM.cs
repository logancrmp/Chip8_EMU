using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8_EMU.Emulator
{
    static class ROM
    {
        internal static bool LoadRom(string FileName)
        {
            bool RomLoaded = false;

            // Copy the internal ROM to memory
            MMU.MemCpyFromPtr(Internal_ROM, 0x0000, SystemConfig.ROM_SIZE);

            // open the binary file if it exists
            if (File.Exists(FileName))
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(FileName);

                    if (fileBytes.Length < (SystemConfig.MEMORY_SIZE - SystemConfig.HARDWARE_PC_INIT_ADDRESS))
                    {
                        // Copy the external ROM to memory
                        MMU.MemCpyFromPtr(fileBytes, SystemConfig.HARDWARE_PC_INIT_ADDRESS, (ushort)fileBytes.Length);

                        RomLoaded = true;
                    }
                }
                catch { }
            }

            return RomLoaded;
        }


        internal static byte[] Internal_ROM = new byte[]
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0,
            0x20, 0x60, 0x20, 0x20, 0x70,
            0xF0, 0x10, 0xF0, 0x80, 0xF0,
            0xF0, 0x10, 0xF0, 0x10, 0xF0,
            0x90, 0x90, 0xF0, 0x10, 0x10,
            0xF0, 0x80, 0xF0, 0x10, 0xF0,
            0xF0, 0x80, 0xF0, 0x90, 0xF0,
            0xF0, 0x10, 0x20, 0x40, 0x40,
            0xF0, 0x90, 0xF0, 0x90, 0xF0,
            0xF0, 0x90, 0xF0, 0x10, 0xF0,
            0xF0, 0x90, 0xF0, 0x90, 0x90,
            0xE0, 0x90, 0xE0, 0x90, 0xE0,
            0xF0, 0x80, 0x80, 0x80, 0xF0,
            0xE0, 0x90, 0x90, 0x90, 0xE0,
            0xF0, 0x80, 0xF0, 0x80, 0xF0,
            0xF0, 0x80, 0xF0, 0x80, 0x80,
        };
    }
}
