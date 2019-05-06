using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Chip8_EMU.Emulator
{
    internal class VideoFrame : FrameworkElement
    {
        Rect rectStruct = new Rect(0, 0, SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT);

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(Screen.bitmap, rectStruct);
        }
    }

    static class Screen
    {
        private static MainWindow ParentWindow;
        internal delegate void InvokeDelegate();

        internal static PixelFormat pixelFormat = PixelFormats.Rgb24;

        internal static byte[] FrameBuffer;
        internal static int Stride = ((SystemConfig.DRAW_FRAME_WIDTH * pixelFormat.BitsPerPixel) + 7) / 8;
        internal static byte[][] EMU_FRAME;
        internal static object __EmuFrame_Lock = new object();
        internal static WriteableBitmap bitmap = new WriteableBitmap(SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT, 96, 96, pixelFormat, null);

        internal static int ScreenTimerHandle = 0xFF;

        private static BackgroundWorker PipelineWorker = new BackgroundWorker();
        private static bool PipelineActive = false;
        private static object __PipelineActive_Lock = new object();


        static internal void InitScreen(MainWindow ParentWindow)
        {
            Screen.ParentWindow = ParentWindow;

            FrameBuffer = new byte[SystemConfig.DRAW_FRAME_HEIGHT * Stride];
            EMU_FRAME = new byte[SystemConfig.EMU_SCREEN_HEIGHT][];

            for (int iter = 0; iter < SystemConfig.EMU_SCREEN_HEIGHT; iter += 1)
            {
                EMU_FRAME[iter] = new byte[SystemConfig.EMU_SCREEN_WIDTH];
            }

            PipelineWorker.DoWork += GraphicsPipeline;
            PipelineWorker.RunWorkerCompleted += PipelineComplete;

            ScreenTimerHandle = Clock.AddTimer(TimerTypeEnum.TimerRepeating, SystemConfig.FRAME_RATE, TriggerGraphicsPipeline, false);
        }


        internal static void TriggerGraphicsPipeline()
        {
            bool PipelineActiveLocal = false;

            while (true)
            {
                lock (__PipelineActive_Lock)
                {
                    PipelineActiveLocal = PipelineActive;

                    if (PipelineActive == false)
                    {
                        // set true
                        PipelineActive = true;

                        // launch thread
                        PipelineWorker.RunWorkerAsync();
                        break;
                    }
                }

                if (PipelineActiveLocal == true)
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
        }


        internal static void GraphicsPipeline(object sender, DoWorkEventArgs e)
        {
            CopyToFrameBuffer();

            SyncDrawFrameToScreen();
        }


        internal static void PipelineComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (__PipelineActive_Lock)
            {
                PipelineActive = false;
            }
        }


        static int ImgDivWidth = SystemConfig.DRAW_FRAME_WIDTH / SystemConfig.EMU_SCREEN_WIDTH;
        static int ImgDivHeight = SystemConfig.DRAW_FRAME_HEIGHT / SystemConfig.EMU_SCREEN_HEIGHT;
        static internal void CopyToFrameBuffer()
        {
            lock (__EmuFrame_Lock)
            {
                // parallel threads for a memcpy? seems to help but might not be the cause
                Parallel.For(0, SystemConfig.EMU_SCREEN_HEIGHT, (y) =>
                {
                    for (int x = 0; x < SystemConfig.EMU_SCREEN_WIDTH; x += 1)
                    {
                        // scale each emulator pixel to ImgDivWidth * ImgDivHeight frame buffer pixels
                        for (int i = 0; i < ImgDivHeight; i += 1)
                        {
                            for (int j = 0; j < ImgDivWidth; j += 1)
                            {
                                FrameBuffer[(((y * ImgDivHeight) + i) * Stride) + (((x * ImgDivWidth) + j) * 3)] = EMU_FRAME[y][x];
                            }
                        }
                    }
                });
            }
        }


        static double fps = 0;
        static double TimeNow = 0;
        static double LastTime = 0;
        static long framecounter = 0;
        static Int32Rect rect = new Int32Rect(0, 0, SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT);
        static internal void SyncDrawFrameToScreen()
        {
            framecounter += 1;

            if (framecounter % 60 == 0)
            {
                TimeNow = Clock.GetRealTimeNow();
                fps = (60 * (long)SystemConfig.ONE_BILLION) / ((TimeNow - LastTime) + 1);
                LastTime = TimeNow;
            }

            try
            {
                ParentWindow.Dispatcher.Invoke(() =>
                {
                    bitmap.WritePixels(rect, FrameBuffer, Stride, 0);

                    if (framecounter % 60 == 0)
                    {
                        ParentWindow.SetLogText("fps: " + fps.ToString("N2") + "\nIPS: " + string.Format("{0:n0}", CPU.IPS));
                    }
                });
            }
            catch { }
        }
    }
}
