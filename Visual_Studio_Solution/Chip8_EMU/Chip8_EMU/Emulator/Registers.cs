using System;

namespace Chip8_EMU.Emulator
{
    internal class RegisterMap
    {
        // Program Counter
        internal UInt16 PC;

        // Stack Pointer
        internal byte SP;

        // Address Register
        internal UInt16 I;

        // Timer Registers
        internal byte DelayTimer;
        internal byte SoundTimer;

        // General Purpose Registers
        internal byte V0;
        internal byte V1;
        internal byte V2;
        internal byte V3;
        internal byte V4;
        internal byte V5;
        internal byte V6;
        internal byte V7;
        internal byte V8;
        internal byte V9;
        internal byte VA;
        internal byte VB;
        internal byte VC;
        internal byte VD;
        internal byte VE;
        internal byte VF;

        // flags
        internal byte J_JumpFlag;
        internal byte C_Carry;

        internal void ClearRegisters()
        {
            PC = 0;
            SP = 0;
            I = 0;
            DelayTimer = 0;
            SoundTimer = 0;
            V0 = 0;
            V1 = 0;
            V2 = 0;
            V3 = 0;
            V4 = 0;
            V5 = 0;
            V6 = 0;
            V7 = 0;
            V8 = 0;
            V9 = 0;
            VA = 0;
            VB = 0;
            VC = 0;
            VD = 0;
            VE = 0;
            VF = 0;
            J_JumpFlag = 0;
            C_Carry = 0;
        }

        internal byte GetVXRegValue(byte X)
        {
            byte VX = 0;

            switch(X)
            {
                case 0x0:
                {
                    VX = V0;
                    break;
                }

                case 0x1:
                {
                    VX = V1;
                    break;
                }

                case 0x2:
                {
                    VX = V2;
                    break;
                }

                case 0x3:
                {
                    VX = V3;
                    break;
                }

                case 0x4:
                {
                    VX = V4;
                    break;
                }

                case 0x5:
                {
                    VX = V5;
                    break;
                }

                case 0x6:
                {
                    VX = V6;
                    break;
                }

                case 0x7:
                {
                    VX = V7;
                    break;
                }

                case 0x8:
                {
                    VX = V8;
                    break;
                }

                case 0x9:
                {
                    VX = V9;
                    break;
                }

                case 0xA:
                {
                    VX = VA;
                    break;
                }

                case 0xB:
                {
                    VX = VB;
                    break;
                }

                case 0xC:
                {
                    VX = VC;
                    break;
                }

                case 0xD:
                {
                    VX = VD;
                    break;
                }

                case 0xE:
                {
                    VX = VE;
                    break;
                }

                case 0xF:
                {
                    VX = VF;
                    break;
                }
            }

            return VX;
        }

        internal void SetVXRegValue(byte X, byte Value)
        {
            switch(X)
            {
                case 0x0:
                {
                    V0 = Value;
                    break;
                }

                case 0x1:
                {
                    V1 = Value;
                    break;
                }

                case 0x2:
                {
                    V2 = Value;
                    break;
                }

                case 0x3:
                {
                    V3 = Value;
                    break;
                }

                case 0x4:
                {
                    V4 = Value;
                    break;
                }

                case 0x5:
                {
                    V5 = Value;
                    break;
                }

                case 0x6:
                {
                    V6 = Value;
                    break;
                }

                case 0x7:
                {
                    V7 = Value;
                    break;
                }

                case 0x8:
                {
                    V8 = Value;
                    break;
                }

                case 0x9:
                {
                    V9 = Value;
                    break;
                }

                case 0xA:
                {
                    VA = Value;
                    break;
                }

                case 0xB:
                {
                    VB = Value;
                    break;
                }

                case 0xC:
                {
                    VC = Value;
                    break;
                }

                case 0xD:
                {
                    VD = Value;
                    break;
                }

                case 0xE:
                {
                    VE = Value;
                    break;
                }

                case 0xF:
                {
                    VF = Value;
                    break;
                }
            }
        }
    }
}
