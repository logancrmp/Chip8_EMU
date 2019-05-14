using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Game8.Emulator
{
    internal class VideoFrame : FrameworkElement
    {
        Rect rectStruct = new Rect(0, 0, SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT);

        internal static WriteableBitmap bitmap = new WriteableBitmap(SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT, 96, 96, Screen.pixelFormat, null);

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(bitmap, rectStruct);
        }
    }

    class Screen
    {
        private MainWindow ParentWindow;
        private Chip8 System;

        internal readonly static PixelFormat pixelFormat = PixelFormats.Rgb24;
        internal readonly static int Stride = ((SystemConfig.DRAW_FRAME_WIDTH * pixelFormat.BitsPerPixel) + 7) / 8;

        internal byte[] FrameBuffer;
        internal byte[][] EMU_FRAME; // array of arrays was roughly 7-8 percentage points lower cpu usage than 2d array
        internal object __EmuFrame_Lock = new object();

        private int ScreenTimerHandle = 0xFF;

        // Vars for graphics thread
        private BackgroundWorker PipelineWorker = new BackgroundWorker();
        private bool PipelineActive = false;
        private object __PipelineActive_Lock = new object();

        private double fps = 0;
        private double TimeNow = 0;
        private double LastTime = 0;
        private long framecounter = 0;
        private Int32Rect rect = new Int32Rect(0, 0, SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT);

        private const int ImgDivWidth = SystemConfig.DRAW_FRAME_WIDTH / SystemConfig.EMU_SCREEN_WIDTH;
        private const int ImgDivHeight = SystemConfig.DRAW_FRAME_HEIGHT / SystemConfig.EMU_SCREEN_HEIGHT;


        internal Screen(Chip8 System, MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            this.System = System;

            FrameBuffer = new byte[SystemConfig.DRAW_FRAME_HEIGHT * Stride];
            EMU_FRAME = new byte[SystemConfig.EMU_SCREEN_HEIGHT][];

            for (int iter = 0; iter < SystemConfig.EMU_SCREEN_HEIGHT; iter += 1)
            {
                EMU_FRAME[iter] = new byte[SystemConfig.EMU_SCREEN_WIDTH];
            }

            PipelineWorker.DoWork += GraphicsPipeline;
            PipelineWorker.RunWorkerCompleted += PipelineComplete;
        }


        internal void SetupClocks()
        {
            ScreenTimerHandle = System.Clock.AddTimer(TriggerGraphicsPipeline);
            System.Clock.GetTimer(ScreenTimerHandle).SetTimerCyclic((SystemConst.ONE_BILLION / SystemConfig.FRAME_RATE), true);
            System.Clock.GetTimer(ScreenTimerHandle).StartTimer();
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
                        Thread.Sleep(SystemConfig.PERFORMANCE_LEVEL - 1);
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


        internal void SyncDrawFrameToScreen()
        {
            bool StatsUpdated = false;

            if (framecounter++ == SystemConfig.FRAME_RATE)
            {
                TimeNow = System.Clock.GetRealTimeNow();
                fps = (framecounter * SystemConst.ONE_BILLION) / ((TimeNow - LastTime) + 1);
                LastTime = TimeNow;

                framecounter = 0;
                StatsUpdated = true;
            }

            try
            {
                // copy the framebuffer to the output, and draw it to the screen (hopefully soonish maybe)
                ParentWindow.customRender.Dispatcher.Invoke(() =>
                {
                    VideoFrame.bitmap.WritePixels(rect, FrameBuffer, Stride, 0);

                    if (StatsUpdated)
                    {
                        double CpuHz = System.CPU.IPS;
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

                        // FWPS: Frame writes per second (not necessarily drawn).
                        // FREQ: Instructions executed per real time second.
                        ParentWindow.SetLogText("FWPS : " + fps.ToString("N2") + "\nFREQ: " + CpuHz.ToString("N2") + " " + CpuStr);
                    }
                }, DispatcherPriority.Render);
            }
            catch { }

            StatsUpdated = false;
        }
    }
}
