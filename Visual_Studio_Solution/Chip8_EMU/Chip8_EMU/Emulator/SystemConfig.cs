using System.Linq;

namespace Chip8_EMU.Emulator
{
    static class SystemConfig
    {
        // CONSTANTS
        internal const ushort KILOBYTE = 0x0400;
        internal const uint ONE_BILLION = 1000000000;
        internal const uint SIXTY_HZ_TICK_NS = (ONE_BILLION / 60);
        internal const byte STACK_EMPTY = 0xFF;
        internal const double MS_TO_NS_FACTOR = 1000000.0;


        // CONFIGURATION
        internal static char[] InputKeyMap = new char[] { 'S', 'B', 'C', 'T', 'A', 'S', 'D', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P' };

        internal const int FRAME_RATE = 144;

        internal const int CPU_FREQ = 1000000;

        internal const int DRAW_FRAME_WIDTH = 1280;
        internal const int DRAW_FRAME_HEIGHT = 640;
        internal const int EMU_SCREEN_WIDTH = 640;
        internal const int EMU_SCREEN_HEIGHT = 320;

        internal const byte STACK_SIZE = 24;
        internal const ushort HARDWARE_PC_INIT_ADDRESS = 0x0200;

        internal const ushort MEMORY_SIZE = (4 * (KILOBYTE));
        internal static ushort ROM_SIZE = (ushort) ROM.Internal_ROM.Count();
    }
}
