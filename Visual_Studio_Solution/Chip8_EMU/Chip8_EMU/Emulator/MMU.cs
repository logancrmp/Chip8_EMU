using System;
using System.IO;

namespace Chip8_EMU.Emulator
{
    static class MMU
    {
        private static byte[] Memory = new byte[SystemConfig.MEMORY_SIZE];
        private static ushort[] Stack = new ushort[SystemConfig.STACK_SIZE];


        internal static bool InitMemory()
        {
            bool RomLoaded = false;

            // clear RAM
            MemClr(0, SystemConfig.MEMORY_SIZE);

            // Load the boot rom
            MemCpyFromPtr(ROM.Boot_ROM, 0x0000, SystemConfig.ROM_SIZE);

            // Load the external ROM
            RomLoaded = LoadRom("ROM.ch8");

            return RomLoaded;
        }


        internal static bool PushToStack(ushort Address)
        {
            bool Overflow = CPU.IncStackPointer();

            if (Overflow == false)
            {
                Stack[CPU.Registers.SP] = Address;
            }

            return Overflow;
        }


        internal static bool PopFromStack(ref ushort Address)
        {
            if (CPU.Registers.SP != SystemConst.STACK_EMPTY)
            {
                Address = Stack[CPU.Registers.SP];
            }

            bool Underflow = CPU.DecStackPointer();

            return Underflow;
        }


        internal static UInt16 ReadInstruction()
        {
            return (UInt16)((UInt16)(Memory[CPU.Registers.PC] << 8) | Memory[CPU.Registers.PC + 1]);
        }


        internal static bool MemCpyToPtr(ushort Src, out byte[] Dst, ushort NumBytes)
        {
            ushort SrcStart = Src;
            ushort SrcEnd = (ushort)(Src + NumBytes);

            Dst = new byte[0];

            if
            (
                (SrcEnd >= SystemConfig.MEMORY_SIZE)
                ||
                ((SystemConfig.MEMORY_SIZE - Src) < NumBytes)
            )
            {
                return false;
            }

            // add check permissions of memory region vs accessor

            ushort SrcIter = SrcStart;
            ushort DstIter = 0;

            Dst = new byte[NumBytes];

            while (SrcIter < SrcEnd)
            {
                Dst[DstIter] = Memory[SrcIter];

                SrcIter += 1;
                DstIter += 1;
            }

            return true;
        }


        internal static void MemCpyFromPtr(byte[] Src, ushort Dst, ushort NumBytes)
        {
            ushort DstStart = Dst;
            ushort DstEnd = (ushort)(Dst + NumBytes);

            if
            (
                (DstEnd > SystemConfig.MEMORY_SIZE)
                ||
                ((SystemConfig.MEMORY_SIZE - Dst) < NumBytes)
            )
            {
                CPU.EnterTrap(TrapSourceEnum.MemoryAccessOutOfBounds, ReadInstruction());
            }

            // add check permissions of memory region vs accessor

            ushort DstIter = DstStart;
            ushort SrcIter = 0;

            while (DstIter < DstEnd)
            {
                Memory[DstIter] = Src[SrcIter];

                DstIter += 1;
                SrcIter += 1;
            }
        }


        internal static bool MemCpy(ushort Src, ushort Dst, ushort NumBytes)
        {
            ushort SrcStart = Src;
            ushort SrcEnd = (ushort)(Src + NumBytes);

            ushort DstStart = Dst;
            ushort DstEnd = (ushort)(Dst + NumBytes);

            if
            (
                (SrcEnd >= SystemConfig.MEMORY_SIZE)
                ||
                (DstEnd >= SystemConfig.MEMORY_SIZE)
                ||
                ((SystemConfig.MEMORY_SIZE - Src) < NumBytes)
                ||
                ((SystemConfig.MEMORY_SIZE - Dst) < NumBytes)
            )
            {
                return false;
            }

            // add check permissions of memory region vs accessor

            ushort SrcIter = SrcStart;
            ushort DstIter = DstStart;

            while (SrcIter < SrcEnd)
            {
                Memory[DstIter] = Memory[SrcIter];

                DstIter += 1;
                SrcIter += 1;
            }

            return true;
        }


        internal static bool MemClr(ushort Dst, ushort NumBytes)
        {
            ushort DstStart = Dst;
            ushort DstEnd = (ushort)(Dst + NumBytes);

            if ((DstEnd > SystemConfig.MEMORY_SIZE) || ((SystemConfig.MEMORY_SIZE - Dst) < NumBytes))
            {
                return false;
            }

            ushort DstIter = DstStart;

            while (DstIter < DstEnd)
            {
                Memory[DstIter] = 0x00;

                DstIter += 1;
            }

            return true;
        }


        internal static bool LoadRom(string FileName)
        {
            bool RomLoaded = false;

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
    }
}
