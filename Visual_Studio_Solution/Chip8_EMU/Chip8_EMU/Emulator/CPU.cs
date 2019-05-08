using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

enum TrapSourceEnum
{
    StackUnderflow,
    StackOverflow,
    InvalidOpCode,
    ProgramCounterOutOfBounds,
    MemoryAccessOutOfBounds,
};


namespace Chip8_EMU.Emulator
{
    internal class CPU
    {
        internal RegisterMap Registers;

        private Random random = new Random();

        internal int DelayTimerHandle = 0xFF;
        internal int SoundTimerHandle = 0xFF;
        internal int CoreTimerHandle = 0xFF;
        internal int SyncTimerHandler = 0xFF;

        private ulong InstructionCounter = 0;
        private ulong SavedTime = 0;
        internal double IPS = 0;


        internal CPU()
        {
            InitRegisters();
        }


        internal void SetupClocks()
        {
            // Setup timers for delay registers
            DelayTimerHandle = EmuRunner.C8_Clock.AddTimer(DelayTimerCallback);
            SoundTimerHandle = EmuRunner.C8_Clock.AddTimer(SoundTimerCallback);

            // Setup timer for core clock
            CoreTimerHandle = EmuRunner.C8_Clock.AddTimer(ExecCycle);
            EmuRunner.C8_Clock.StartTimerCyclic(CoreTimerHandle, (SystemConst.ONE_BILLION / SystemConfig.CPU_FREQ), false);

            // Setup timer for 60 Hz instruction sync
            SyncTimerHandler = EmuRunner.C8_Clock.AddTimer(null);
            EmuRunner.C8_Clock.StartTimerCyclic(SyncTimerHandler, (SystemConst.ONE_BILLION / 60), true);
        }


        internal void InitRegisters()
        {
            Registers.ClearRegisters();

            /* Load PC With Init Value Of Where Rom Will Be Loaded */
            Registers.PC = SystemConfig.HARDWARE_PC_INIT_ADDRESS;
            Registers.SP = SystemConst.STACK_EMPTY;
        }


        internal void ExecCycle()
        {
            InstructionCounter += 1;

            if (InstructionCounter == SystemConfig.CPU_FREQ)
            {
                ulong TimeNow = EmuRunner.C8_Clock.GetRealTimeNow();
                IPS = (InstructionCounter) / ((double)(TimeNow - SavedTime) / SystemConst.ONE_BILLION);
                SavedTime = TimeNow;
                InstructionCounter = 0;
            }

            UInt16 Instruction = 0;
            byte BytesUsedDuringInstruction = 0;

            ExecuteInstruction();

            // Instructions may set the jump flag to avoid having the program counter be incremented (e.x. jump instructions)
            if (Registers.J_JumpFlag == 0)
            {
                int BytesToAdd = 2 + BytesUsedDuringInstruction;

                if ((Registers.PC + BytesToAdd) < SystemConfig.MEMORY_SIZE)
                {
                    // increment the pc depending on the opcode length plus the number of additional bytes used by the instruction
                    Registers.PC += (ushort)BytesToAdd;
                }
                else
                {
                    EnterTrap(TrapSourceEnum.ProgramCounterOutOfBounds, Instruction);
                }
            }

            // clear any clearable flags / states
            Registers.J_JumpFlag = 0;
        }


        internal bool IncStackPointer()
        {
            bool StackOverflow = false;

            if (Registers.SP == SystemConst.STACK_EMPTY)
            {
                // set to the first real stack entry
                Registers.SP = 0x00;
            }
            else
            if (Registers.SP + 1 < SystemConfig.STACK_SIZE)
            {
                // check against moving the SP past the end of the stack
                Registers.SP += 1;
            }
            else
            {
                // stack overflow!
                StackOverflow = true;
            }

            return StackOverflow;
        }


        internal bool DecStackPointer()
        {
            bool StackUnderflow = false;

            // check against moving the SP past the start of the stack
            if (Registers.SP == SystemConst.STACK_EMPTY)
            {
                // stack underflow!
                StackUnderflow = true;
            }
            else
            {
                if (Registers.SP > 0)
                {
                    Registers.SP -= 1;
                }
                else
                {
                    // stack is empty now!
                    Registers.SP = SystemConst.STACK_EMPTY;
                }
            }

            return StackUnderflow;
        }


        internal byte GetRandByte()
        {
            byte[] RandByte = new byte[1];
            random.NextBytes(RandByte);
            return RandByte[0];
        }


        internal void DelayTimerCallback()
        {
            if (Registers.DelayTimer > 0)
            {
                Registers.DelayTimer -= 1;

                // if transitioning from non-zero to zero
                if (Registers.DelayTimer == 0)
                {
                    EmuRunner.C8_Clock.StopTimer(DelayTimerHandle);
                }
            }
        }


        internal void SoundTimerCallback()
        {
            if (Registers.SoundTimer > 0)
            {
                Registers.SoundTimer -= 1;

                // if transitioning from non-zero to zero
                if (Registers.SoundTimer == 0)
                {
                    // need to add a speaker and interface for arbitrating start and stop across users
                    CPU_Instructions.simpleSound.Stop();
                    EmuRunner.C8_Clock.StopTimer(SoundTimerHandle);
                }
            }
        }


        internal void EnterTrap(TrapSourceEnum TrapSource, UInt16 TrapInstruction)
        {
            while (true) { }
        }


        internal void ExecuteInstruction()
        {
            // read memory at program counter location
            UInt16 Instruction = EmuRunner.C8_MMU.ReadInstruction();

            // Ref: https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
            switch (Instruction >> 12)
            {
                // decode sub-instruction
                case 0x0:
                {
                    switch (Instruction)
                    {
                        // Clear the screen
                        case 0x00E0:
                        {
                            CPU_Instructions.Instruction_CLEAR_THE_SCREEN(Instruction);
                            break;
                        }

                        // Return from subroutine
                        case 0x00EE:
                        {
                            CPU_Instructions.Instruction_RETURN_FROM_SUBROUTINE(Instruction);

                            break;
                        }

                        // Call RCA 1802 program at address NNN ?!?
                        default:
                        {
                            //EnterTrap(TrapSourceEnum.InvalidOpCode, Instruction);
                            break;
                        }
                    }

                    break;
                }

                // Jump to address
                case 0x1:
                {
                    CPU_Instructions.Instruction_JUMP_TO_ADDRESS(Instruction);

                    break;
                }

                // Call subroutine at address
                case 0x2:
                {
                    CPU_Instructions.Instruction_CALL_SUBROUTINE_AT_ADDRESS(Instruction);

                    break;
                }

                // Compare and skip if equal
                case 0x3:
                {
                    CPU_Instructions.Instruction_SKIP_IF_VX_EQUALS_NN(Instruction);

                    break;
                }

                // Compare and skip if not equal
                case 0x4:
                {
                    CPU_Instructions.Instruction_SKIP_IF_VX_NOT_EQUAL_NN(Instruction);

                    break;
                }

                // Compare and skip if equal
                case 0x5:
                {
                    CPU_Instructions.Instruction_SKIP_IF_VX_EQUAL_VY(Instruction);

                    break;
                }

                // Set VX to NN
                case 0x6:
                {
                    CPU_Instructions.Instruction_SET_VX_TO_NN(Instruction);

                    break;
                }

                // Add NN to VX
                case 0x7:
                {
                    CPU_Instructions.Instruction_ADD_NN_TO_VX(Instruction);

                    break;
                }

                // decode sub-instruction 
                case 0x8:
                {
                    switch (Instruction & 0x000F)
                    {
                        // Set VX to VY 
                        case 0x0:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VY(Instruction);

                            break;
                        }

                        // Set VX to VX | VY
                        case 0x1:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VX_OR_VY(Instruction);

                            break;
                        }

                        // Set VX to VX & VY
                        case 0x2:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VX_AND_VY(Instruction);

                            break;
                        }

                        // Set VX to VX ^ VY
                        case 0x3:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VX_XOR_VY(Instruction);

                            break;
                        }

                        // Set VX to VX + VY
                        case 0x4:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VX_PLUS_VY(Instruction);

                            break;
                        }

                        // Set VX to VX - VY
                        case 0x5:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VX_MINUS_VY(Instruction);

                            break;
                        }

                        // Set VX to VX >> 1
                        case 0x6:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VX_RSHIFT_1(Instruction);

                            break;
                        }

                        // Set VX to VY - VX
                        case 0x7:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VY_MINUS_VX(Instruction);

                            break;
                        }

                        // Set VX to VX << 1
                        case 0xE:
                        {
                            CPU_Instructions.Instruction_SET_VX_TO_VX_LSHIFT_1(Instruction);

                            break;
                        }

                        default:
                        {
                            EnterTrap(TrapSourceEnum.InvalidOpCode, Instruction);
                            break;
                        }
                    }

                    break;
                }

                // Skip if VX != VY
                case 0x9:
                {
                    CPU_Instructions.Instruction_SKIP_IF_VX_NOT_EQUAL_VY(Instruction);

                    break;
                }

                // Set I to address
                case 0xA:
                {
                    CPU_Instructions.Instruction_SET_I_TO_ADDRESS(Instruction);

                    break;
                }

                // Jump to address plus offset
                case 0xB:
                {
                    CPU_Instructions.Instruction_JUMP_TO_ADDRESS_PLUS_V0(Instruction);

                    break;
                }

                // Set VX to rand & NN
                case 0xC:
                {
                    CPU_Instructions.Instruction_VX_TO_RAND_AND_NN(Instruction);

                    break;
                }

                // Draw sprite at coord X of size  8xN
                case 0xD:
                {
                    CPU_Instructions.Instruction_DRAW_SPRITE_AT_COORD(Instruction);

                    break;
                }

                // decode sub-instruction 
                case 0xE:
                {
                    switch (Instruction & 0x00FF)
                    {
                        // skip if key pressed
                        case 0x9E:
                        {
                            CPU_Instructions.Instruction_SKIP_IF_KEY_PRESSED(Instruction);

                            break;
                        }

                        // Skip if key not pressed
                        case 0xA1:
                        {
                            CPU_Instructions.Instruction_SKIP_IF_KEY_NOT_PRESSED(Instruction);

                            break;
                        }

                        default:
                        {
                            EnterTrap(TrapSourceEnum.InvalidOpCode, Instruction);
                            break;
                        }
                    }

                    break;
                }

                // decode sub-instruction 
                case 0xF:
                {
                    switch (Instruction & 0x00FF)
                    {
                        // Get delay timer
                        case 0x07:
                        {
                            CPU_Instructions.Instruction_GET_DELAY_TIMER(Instruction);

                            break;
                        }

                        // Get key
                        case 0x0A:
                        {
                            CPU_Instructions.Instruction_GET_PRESSED_KEY(Instruction);

                            break;
                        }

                        // Set delay timer
                        case 0x15:
                        {
                            CPU_Instructions.Instruction_SET_DELAY_TIMER(Instruction);

                            break;
                        }

                        // Set sound timer
                        case 0x18:
                        {
                            CPU_Instructions.Instruction_SET_SOUND_TIMER(Instruction);

                            break;
                        }

                        // Add value of VX to I
                        case 0x1E:
                        {
                            CPU_Instructions.Instruction_ADD_VX_TO_I(Instruction);

                            break;
                        }

                        // Set I to VX hex sprite
                        case 0x29:
                        {
                            // InstructionFncPtr = ;

                            break;
                        }

                        // 
                        case 0x33:
                        {
                            CPU_Instructions.Instruction_STORE_BINARY_CODED_DECIMAL(Instruction);

                            break;
                        }

                        // Store VX registers to location at I
                        case 0x55:
                        {
                            CPU_Instructions.Instruction_STORE_REGISTERS(Instruction);

                            break;
                        }

                        // Load VX registers from location at I
                        case 0x65:
                        {
                            CPU_Instructions.Instruction_LOAD_REGISTERS(Instruction);

                            break;
                        }

                        default:
                        {
                            EnterTrap(TrapSourceEnum.InvalidOpCode, Instruction);
                            break;
                        }
                    }

                    break;
                }

                default:
                {
                    EnterTrap(TrapSourceEnum.InvalidOpCode, Instruction);
                    break;
                }
            }
        }
    }
}
