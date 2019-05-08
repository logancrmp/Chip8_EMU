using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chip8_EMU.Emulator
{
    static class CPU_Instructions
    {
        // need to add to a speaker component!!!
        internal static SoundPlayer simpleSound = new SoundPlayer("tone.wav");


        internal static byte Instruction_NOP(ushort Instruction)
        {
            // not implemented
            return 0;
        }


        internal static byte Instruction_CLEAR_THE_SCREEN(ushort Instruction)
        {
            lock (Screen.__EmuFrame_Lock)
            {
                for (int heightIter = 0; heightIter < SystemConfig.EMU_SCREEN_HEIGHT; heightIter += 1)
                {
                    for (int widthIter = 0; widthIter < SystemConfig.EMU_SCREEN_WIDTH; widthIter += 1)
                    {
                        Screen.EMU_FRAME[heightIter][widthIter] = 0x00;
                    }
                }
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_RETURN_FROM_SUBROUTINE(ushort Instruction)
        {
           // set default return address to a safe value
            ushort ReturnAddress = SystemConfig.HARDWARE_PC_INIT_ADDRESS;

            // dec stack ptr, get value from top of stack
            bool Underflow = MMU.PopFromStack(ref ReturnAddress);

            // if no underflow
            if (Underflow == false)
            {
                // set PC to value from stack
                CPU.Registers.PC = ReturnAddress;

                // Set the jump flag to not increment the program counter after the instruction is complete
                CPU.Registers.J_JumpFlag = 1;
            }
            else
            {
                CPU.EnterTrap(TrapSourceEnum.StackUnderflow, Instruction);
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_JUMP_TO_ADDRESS(ushort Instruction)
        {
            // Mask out the opcode, get the address from the instruction
            ushort Address = (ushort)(Instruction & 0x0FFF);

            // Set the program counter to the jump location
            CPU.Registers.PC = Address;

            // Set the jump flag to not increment the program counter after the instruction is complete
            CPU.Registers.J_JumpFlag = 1;

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_CALL_SUBROUTINE_AT_ADDRESS(ushort Instruction)
        {
            // Mask out the opcode, get the address from the instruction
            ushort SubRoutineAddress = (ushort)(Instruction & 0x0FFF);

            // Set the return address to the next instruction
            ushort ReturnAddress = (ushort)(CPU.Registers.PC + 2);

            // inc stack ptr
            bool Underflow = MMU.PushToStack(ReturnAddress);

            if (Underflow == false)
            {
                // Set the program counter to the jump location
                CPU.Registers.PC = SubRoutineAddress;

                // Set the jump flag to not increment the program counter after the instruction is complete
                CPU.Registers.J_JumpFlag = 1;

            }
            else
            {
                CPU.EnterTrap(TrapSourceEnum.StackOverflow, Instruction);
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SKIP_IF_VX_EQUALS_NN(ushort Instruction)
        {
            // mask out instruction values
            byte NN = (byte)(Instruction & 0xFF);
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // if equal
            if (NN == CPU.Registers.GetVXRegValue(X))
            {
                // move program counter one instruction forward
                // cpu will auto increment the program counter another instruction
                CPU.Registers.PC += 2;
            }
            else
            {
                // dont need to do anything, cpu will handle everything
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SKIP_IF_VX_NOT_EQUAL_NN(ushort Instruction)
        {
            // mask out instruction values
            byte NN = (byte)(Instruction & 0xFF);
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // if not equal
            if (NN != CPU.Registers.GetVXRegValue(X))
            {
                // move program counter one instructions forward
                // cpu will auto increment the program counter another instruction
                CPU.Registers.PC += 2;
            }
            else
            {
                // dont need to do anything, cpu will handle everything
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SKIP_IF_VX_EQUAL_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            // if equal
            if (CPU.Registers.GetVXRegValue(X) == CPU.Registers.GetVXRegValue(Y))
            {
                // move program counter one instructions forward
                // cpu will auto increment the program counter another instruction
                CPU.Registers.PC += 2;
            }
            else
            {
                // dont need to do anything, cpu will handle everything
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_NN(ushort Instruction)
        {
            // mask out instruction values
            byte NN = (byte)(Instruction & 0xFF);
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // set VX to NN
            CPU.Registers.SetVXRegValue(X, NN);

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_ADD_NN_TO_VX(ushort Instruction)
        {
            // mask out instruction values
            byte NN = (byte)(Instruction & 0xFF);
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // VX = VX + NN
            CPU.Registers.SetVXRegValue(X, (byte)(CPU.Registers.GetVXRegValue(X) + NN));

            // Carry flag is not changed in this instruction

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            CPU.Registers.SetVXRegValue(X, CPU.Registers.GetVXRegValue(Y));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VX_OR_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            byte VX = CPU.Registers.GetVXRegValue(X);
            byte VY = CPU.Registers.GetVXRegValue(Y);

            CPU.Registers.SetVXRegValue(X, (byte)(VX | VY));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VX_AND_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            byte VX = CPU.Registers.GetVXRegValue(X);
            byte VY = CPU.Registers.GetVXRegValue(Y);

            CPU.Registers.SetVXRegValue(X, (byte)(VX & VY));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VX_XOR_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            byte VX = CPU.Registers.GetVXRegValue(X);
            byte VY = CPU.Registers.GetVXRegValue(Y);

            CPU.Registers.SetVXRegValue(X, (byte)(VX ^ VY));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VX_PLUS_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            ushort VX = CPU.Registers.GetVXRegValue(X);
            ushort VY = CPU.Registers.GetVXRegValue(Y);

            ushort VXY = (ushort)(VX + VY);

            CPU.Registers.VF = (byte)((VXY > 0xFF) ? 1 : 0);

            CPU.Registers.SetVXRegValue(X, (byte)(VXY & 0xFF));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VX_MINUS_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            byte VX = CPU.Registers.GetVXRegValue(X);
            byte VY = CPU.Registers.GetVXRegValue(Y);

            CPU.Registers.VF = (byte)((VX > VY) ? 1 : 0);

            CPU.Registers.SetVXRegValue(X, (byte)(VX - VY));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VX_RSHIFT_1(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            byte VX = CPU.Registers.GetVXRegValue(X);

            CPU.Registers.VF = (byte)(VX & 0x1);

            CPU.Registers.SetVXRegValue(X, (byte)(VX >> 1));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VY_MINUS_VX(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            byte VX = CPU.Registers.GetVXRegValue(X);
            byte VY = CPU.Registers.GetVXRegValue(Y);

            CPU.Registers.VF = (byte)((VY > VX) ? 1 : 0);

            CPU.Registers.SetVXRegValue(X, (byte)(VY - VX));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_VX_TO_VX_LSHIFT_1(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            byte VX = CPU.Registers.GetVXRegValue(X);

            CPU.Registers.VF = (byte)(((VX & 0x80) > 0) ? 1 : 0);

            CPU.Registers.SetVXRegValue(X, (byte)(VX << 1));

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SKIP_IF_VX_NOT_EQUAL_VY(ushort Instruction)
        {
            // mask out instruction values
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte Y = (byte)((Instruction & 0x00F0) >> 4);

            // if not equal
            if (CPU.Registers.GetVXRegValue(X) != CPU.Registers.GetVXRegValue(Y))
            {
                // move program counter one instructions forward
                // cpu will auto increment the program counter another instruction
                CPU.Registers.PC += 2;
            }
            else
            {
                // dont need to do anything, cpu will handle everything
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_I_TO_ADDRESS(ushort Instruction)
        {
            // Mask out the opcode, get the address from the instruction
            ushort Address = (ushort)(Instruction & 0x0FFF);

            CPU.Registers.I = Address;

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_JUMP_TO_ADDRESS_PLUS_V0(ushort Instruction)
        {
            // Mask out the opcode, get the address from the instruction
            ushort Address = (ushort)(Instruction & 0x0FFF);

            // Get value of V0
            byte V0 = CPU.Registers.GetVXRegValue(0);

            // Set the program counter to the jump location
            CPU.Registers.PC = (ushort)(Address + V0);

            // Set the jump flag to not increment the program counter after the instruction is complete
            CPU.Registers.J_JumpFlag = 1;

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_VX_TO_RAND_AND_NN(ushort Instruction)
        {
            // mask out instruction values
            byte NN = (byte)(Instruction & 0xFF);
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            CPU.Registers.SetVXRegValue(X, (byte)(CPU.GetRandByte() & NN));

            // 0 extra bytes used
            return 0;
        }


        private static ulong RefreshDeadline = 0;
        private static int DrawSpriteInstructionState = 0;
        internal static byte Instruction_DRAW_SPRITE_AT_COORD(ushort Instruction)
        {
            if (DrawSpriteInstructionState == 0)
            {
                // block until 60Hz refresh
                RefreshDeadline = Clock.GetNextRealtimeDeadline(CPU.SyncTimerHandler);
                DrawSpriteInstructionState = 1;
                CPU.Registers.J_JumpFlag = 1;
            }
            else
            if (DrawSpriteInstructionState == 1)
            {
                if (Clock.GetTimeNow() >= RefreshDeadline)
                {
                    DrawSpriteInstructionState = 2;
                }

                CPU.Registers.J_JumpFlag = 1;
            }
            else
            {
                DrawSpriteInstructionState = 0;

                // mask out instruction values
                byte X = (byte)((Instruction & 0x0F00) >> 8);
                byte Y = (byte)((Instruction & 0x00F0) >> 4);
                byte N = (byte)(Instruction & 0x000F);

                // 0,0 is top left of screen. vx and vy are offsets from 0,0
                byte VX = CPU.Registers.GetVXRegValue(X);
                byte VY = CPU.Registers.GetVXRegValue(Y);

                // mask values per documentation at http://chip8.sourceforge.net/chip8-1.1.pdf
                VX &= 0x3F;
                VY &= 0x1F;

                // I's points to the memory location that contains the sprite
                int SpriteHeight = N;
                int SpriteWidth = 8;
                byte[] SpriteBits; // is of length SpriteHeight * SpriteWidth

                MMU.MemCpyToPtr(CPU.Registers.I, out SpriteBits, (ushort)(SpriteHeight * SpriteWidth));

                bool PixelErased = false;
                bool BitSet = false;

                lock (Screen.__EmuFrame_Lock)
                {
                    // loop over all bits int SpriteBits
                    for (int ByteIter = 0; ByteIter < SpriteHeight; ByteIter += 1)
                    {
                        for (int BitIter = 0; BitIter < 8; BitIter += 1)
                        {
                            BitSet = false;

                            if (((SpriteBits[ByteIter] >> (7 - BitIter)) & 0x1) > 0)
                            {
                                BitSet = true;
                            }

                            if
                            (
                                ((VX + BitIter) >= SystemConfig.EMU_SCREEN_WIDTH)
                                ||
                                ((VY + ByteIter) >= SystemConfig.EMU_SCREEN_HEIGHT)
                            )
                            {
                                break;
                            }

                            byte NewPixelValue = (byte)(BitSet ? 0xFF : 0x00);

                            if ((Screen.EMU_FRAME[VY + ByteIter][VX + BitIter] & NewPixelValue) > 0)
                            {
                                PixelErased = true;
                            }

                            Screen.EMU_FRAME[VY + ByteIter][VX + BitIter] ^= NewPixelValue;
                        }
                    }
                }

                CPU.Registers.VF = (byte)((PixelErased == true) ? 1 : 0);
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SKIP_IF_KEY_PRESSED(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // Get the key value from the lower 4 bits of the VX register
            byte Key = (byte) (CPU.Registers.GetVXRegValue(X) & 0x0F);

            // check if key was reported as pressed
            if (Keyboard.IsKeyPressed(Key) == true)
            {
                // move program counter one instructions forward
                // cpu will auto increment the program counter another instruction
                CPU.Registers.PC += 2;
            }
            else
            {
                // dont need to do anything, cpu will handle everything
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SKIP_IF_KEY_NOT_PRESSED(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // Get the key value from the lower 4 bits of the VX register
            byte Key = (byte)(CPU.Registers.GetVXRegValue(X) & 0x0F);

            // check if key was reported as not pressed
            if (Keyboard.IsKeyPressed(Key) == false)
            {
                // move program counter one instructions forward
                // cpu will auto increment the program counter another instruction
                CPU.Registers.PC += 2;
            }
            else
            {
                // dont need to do anything, cpu will handle everything
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_GET_DELAY_TIMER(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // Set VX to the value of the delay timer
            CPU.Registers.SetVXRegValue(X, CPU.Registers.DelayTimer);

            // 0 extra bytes used
            return 0;
        }


        static int CallCntr = 0;
        static int pressedkeyfncstate = 0;
        static byte PressedKey = 0xFF;
        internal static byte Instruction_GET_PRESSED_KEY(ushort Instruction)
        {
            if (pressedkeyfncstate == 0)
            {
                CallCntr++;

                if (CallCntr == (SystemConfig.CPU_FREQ / 100))
                {
                    CallCntr = 0;

                    for (byte Key = 0; Key < 0x10; Key += 1)
                    {
                        // check if key was reported as pressed
                        if (Keyboard.IsKeyPressed(Key) == true)
                        {
                            // need to add a speaker and interface for arbitrating start and stop across users
                            simpleSound.PlayLooping();

                            PressedKey = Key;

                            pressedkeyfncstate = 1;
                        }
                    }
                }

                // block the program counter from incrementing and run this instruction again!
                CPU.Registers.J_JumpFlag = 1;
            }
            else
            if (pressedkeyfncstate == 1)
            {
                CallCntr++;

                if (CallCntr == (SystemConfig.CPU_FREQ / 100))
                {
                    CallCntr = 0;

                    // check if key was reported as not pressed
                    if (Keyboard.IsKeyPressed(PressedKey) == false)
                    {
                        // need to add a speaker and interface for arbitrating start and stop across users
                        simpleSound.Stop();

                        pressedkeyfncstate = 0;
                        CallCntr = 0;
                    }
                    else
                    {
                        // key is still pressed, block the program counter from incrementing and run this instruction again!
                        CPU.Registers.J_JumpFlag = 1;
                    }
                }
                else
                {
                    // key is still pressed, block the program counter from incrementing and run this instruction again!
                    CPU.Registers.J_JumpFlag = 1;
                }
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_DELAY_TIMER(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            CPU.Registers.DelayTimer = CPU.Registers.GetVXRegValue(X);

            if (CPU.Registers.DelayTimer > 0)
            {
                Clock.StartTimerCyclic(CPU.DelayTimerHandle, SystemConst.SIXTY_HZ_TICK_NS, false);
            }
            else
            {
                Clock.StopTimer(CPU.DelayTimerHandle);
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_SET_SOUND_TIMER(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            CPU.Registers.SoundTimer = CPU.Registers.GetVXRegValue(X);

            if (CPU.Registers.SoundTimer > 0)
            {
                Clock.StartTimerCyclic(CPU.SoundTimerHandle, SystemConst.SIXTY_HZ_TICK_NS, false);

                // need to add a speaker and interface for arbitrating start and stop across users
                simpleSound.PlayLooping();
            }
            else
            {
                Clock.StopTimer(CPU.SoundTimerHandle);

                // need to add a speaker and interface for arbitrating start and stop across users
                simpleSound.Stop();
            }

            // 0 extra bytes used
            return 0;
        }

        internal static byte Instruction_ADD_VX_TO_I(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            // If arithmatic overflow set VF, else clear
            if (((uint)X + (uint)CPU.Registers.I) > 0xFFF)
            {
                CPU.Registers.VF = 1;
            }
            else
            {
                CPU.Registers.VF = 1;
            }

            // Add value of VX to I
            CPU.Registers.I += CPU.Registers.GetVXRegValue(X);

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_STORE_BINARY_CODED_DECIMAL(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);
            byte VX = CPU.Registers.GetVXRegValue(X);
            byte[] StoreByte = new byte[1];

            StoreByte[0] = (byte)((VX - (VX % 100)) / 100);
            MMU.MemCpyFromPtr(StoreByte, CPU.Registers.I, 1);
            VX -= (byte)(StoreByte[0] * 100);

            StoreByte[0] = (byte)((VX - (VX % 10)) / 10);
            MMU.MemCpyFromPtr(StoreByte, (ushort)(CPU.Registers.I + 1), 1);
            VX -= (byte)(StoreByte[0] * 10);

            StoreByte[0] = VX;
            MMU.MemCpyFromPtr(StoreByte, (ushort)(CPU.Registers.I + 2), 1);

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_STORE_REGISTERS(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            for (byte RegIter = 0; RegIter <= X; RegIter += 1)
            {
                MMU.MemCpyFromPtr(new byte[] { CPU.Registers.GetVXRegValue(RegIter) }, (ushort)(CPU.Registers.I + RegIter), 1);
            }

            // 0 extra bytes used
            return 0;
        }


        internal static byte Instruction_LOAD_REGISTERS(ushort Instruction)
        {
            // parse instruction
            byte X = (byte)((Instruction & 0x0F00) >> 8);

            byte[] OutByte = new byte[1];
            for (byte RegIter = 0; RegIter <= X; RegIter += 1)
            {
                MMU.MemCpyToPtr((ushort)(CPU.Registers.I + RegIter), out OutByte, 1);

                CPU.Registers.SetVXRegValue(RegIter, OutByte[0]);
            }

            // 0 extra bytes used
            return 0;
        }
    }
}
