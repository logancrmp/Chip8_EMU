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
            drawingContext.DrawImage(EmuRunner.C8_Screen.bitmap, rectStruct);
        }
    }

    class Screen
    {
        private MainWindow ParentWindow;
        internal delegate void InvokeDelegate();

        internal static PixelFormat pixelFormat = PixelFormats.Rgb24;

        internal byte[] FrameBuffer;
        internal int Stride = ((SystemConfig.DRAW_FRAME_WIDTH * pixelFormat.BitsPerPixel) + 7) / 8;
        internal byte[][] EMU_FRAME; // array of arrays was roughly 7-8 percentage points lower cpu usage that 2d array
        internal object __EmuFrame_Lock = new object();
        internal WriteableBitmap bitmap = new WriteableBitmap(SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT, 96, 96, pixelFormat, null);

        internal int ScreenTimerHandle = 0xFF;

        private BackgroundWorker PipelineWorker = new BackgroundWorker();
        private bool PipelineActive = false;
        private object __PipelineActive_Lock = new object();


        internal void InitScreen(MainWindow ParentWindow)
        {
            EmuRunner.C8_Screen.ParentWindow = ParentWindow;

            FrameBuffer = new byte[SystemConfig.DRAW_FRAME_HEIGHT * Stride];
            EMU_FRAME = new byte[SystemConfig.EMU_SCREEN_HEIGHT][];

            for (int iter = 0; iter < SystemConfig.EMU_SCREEN_HEIGHT; iter += 1)
            {
                EMU_FRAME[iter] = new byte[SystemConfig.EMU_SCREEN_WIDTH];
            }

            PipelineWorker.DoWork += GraphicsPipeline;
            PipelineWorker.RunWorkerCompleted += PipelineComplete;

            ScreenTimerHandle = EmuRunner.C8_Clock.AddTimer(TriggerGraphicsPipeline);
            EmuRunner.C8_Clock.StartTimerCyclic(ScreenTimerHandle, (SystemConst.ONE_BILLION / SystemConfig.FRAME_RATE), true);
        }


        internal void TriggerGraphicsPipeline()
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
                    if (SystemConfig.PERFORMANCE_LEVEL > 0)
                    {
                        System.Threading.Thread.Sleep(SystemConfig.PERFORMANCE_LEVEL - 1);
                    }
                }
            }
        }


        internal void GraphicsPipeline(object sender, DoWorkEventArgs e)
        {
            CopyToFrameBuffer();

            SyncDrawFrameToScreen();
        }


        internal void PipelineComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (__PipelineActive_Lock)
            {
                PipelineActive = false;
            }
        }


        int ImgDivWidth = SystemConfig.DRAW_FRAME_WIDTH / SystemConfig.EMU_SCREEN_WIDTH;
        int ImgDivHeight = SystemConfig.DRAW_FRAME_HEIGHT / SystemConfig.EMU_SCREEN_HEIGHT;
        internal void CopyToFrameBuffer()
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


        double fps = 0;
        double TimeNow = 0;
        double LastTime = 0;
        long framecounter = 0;
        Int32Rect rect = new Int32Rect(0, 0, SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT);
        internal void SyncDrawFrameToScreen()
        {
            framecounter += 1;

            if (framecounter % 60 == 0)
            {
                TimeNow = EmuRunner.C8_Clock.GetRealTimeNow();
                fps = (60 * (long)SystemConst.ONE_BILLION) / ((TimeNow - LastTime) + 1);
                LastTime = TimeNow;
            }

            try
            {
                // copy the framebuffer to the output, and draw it to the screen
                ParentWindow.customRender.Dispatcher.Invoke(() =>
                {
                    bitmap.WritePixels(rect, FrameBuffer, Stride, 0);

                    if (framecounter % 60 == 0)
                    {
                        double CpuHz = EmuRunner.C8_CPU.IPS;
                        var CpuStr = "";
                        if (CpuHz >= 1000000)
                        {
                            CpuHz /= 1000000.0d;
                            CpuStr = "MHz";
                        }
                        else
                        if (CpuHz >= 1000)
                        {
                            CpuHz /= 1000.0d;
                            CpuStr = "KHz";
                        }
                        else
                        {
                            CpuHz = Math.Floor(CpuHz);
                        }

                        ParentWindow.SetLogText("FPS : " + fps.ToString("N2") + "\nFREQ: " + CpuHz.ToString("N2") + " " + CpuStr);
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            catch { }
        }
    }
}
