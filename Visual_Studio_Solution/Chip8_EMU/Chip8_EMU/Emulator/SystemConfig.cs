using System.Linq;

namespace Chip8_EMU.Emulator
{
    static class SystemConst
    {
        internal const ushort KILOBYTE = 0x0400;
        internal const uint ONE_BILLION = 1000000000;
        internal const uint SIXTY_HZ_TICK_NS = (ONE_BILLION / 60);
        internal const byte STACK_EMPTY = 0xFF;
    }


    static class SystemConfig
    {
        internal static char[] InputKeyMap = new char[] { 'S', 'B', 'C', 'T', 'A', 'S', 'D', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P' };

        internal const int FRAME_RATE = 120;

        internal const uint CPU_FREQ = 20000000;

        // 3 - lowest, 0 - highest
        internal const int PERFORMANCE_LEVEL = 0;

        internal const int DRAW_FRAME_WIDTH = 1280;
        internal const int DRAW_FRAME_HEIGHT = 640;
        internal const int EMU_SCREEN_WIDTH = 64;
        internal const int EMU_SCREEN_HEIGHT = 32;

        internal const byte STACK_SIZE = 24;
        internal const ushort HARDWARE_PC_INIT_ADDRESS = 0x0200;

        internal const ushort MEMORY_SIZE = (4 * (SystemConst.KILOBYTE));
        internal static ushort ROM_SIZE = (ushort) ROM.Boot_ROM.Count();
    }
}
