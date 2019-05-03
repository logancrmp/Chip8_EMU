﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Chip8_EMU.Emulator
{
    internal class VideoFrame : FrameworkElement
    {
        //DrawingGroup drawGroup = new DrawingGroup();
        System.Windows.Rect rectStruct = new System.Windows.Rect(0, 0, SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT);

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

            //bitmap = ParentWindow.VideoFrame.Source as WriteableBitmap;
            //bitmap = new WriteableBitmap(SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT, 96, 96, pixelFormat, null);

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
                    System.Threading.Thread.Sleep(0);
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
        static int ax = 0;
        static int ay = 0;
        static int bx = 0;
        static int by = 0;
        static int c = 0;
        static bool up = false;
        static bool left = false;
        static int framecountthing = 0;
        static internal void CopyToFrameBuffer()
        {
            lock (__EmuFrame_Lock)
            {
                if (framecountthing++ == 2)
                {
                    framecountthing = 0;

                    int rate = Math.Max((240 / SystemConfig.FRAME_RATE), 1);
                    EMU_FRAME[by][bx] = 0x00;
                    EMU_FRAME[by][(bx + 1 < SystemConfig.EMU_SCREEN_WIDTH ? bx + 1 : bx)] = 0x00;
                    EMU_FRAME[(by + 1 < SystemConfig.EMU_SCREEN_HEIGHT ? by + 1 : by)][bx] = 0x00;
                    EMU_FRAME[(by + 1 < SystemConfig.EMU_SCREEN_HEIGHT ? by + 1 : by)][(bx + 1 < SystemConfig.EMU_SCREEN_WIDTH ? bx + 1 : bx)] = 0x00;
                    EMU_FRAME[ay][ax] = 0xFF;
                    EMU_FRAME[ay][(ax + 1 < SystemConfig.EMU_SCREEN_WIDTH ? ax + 1 : ax)] = 0xFF;
                    EMU_FRAME[(ay + 1 < SystemConfig.EMU_SCREEN_HEIGHT ? ay + 1 : ay)][ax] = 0xFF;
                    EMU_FRAME[(ay + 1 < SystemConfig.EMU_SCREEN_HEIGHT ? ay + 1 : ay)][(ax + 1 < SystemConfig.EMU_SCREEN_WIDTH ? ax + 1 : ax)] = 0xFF;

                    if (up)
                    {
                        by = ay;
                        ay -= rate;

                        if (ay <= 0)
                        {
                            ay = 0;
                            up = false;
                        }
                    }
                    else
                    {
                        by = ay;
                        ay += rate;

                        if (ay >= SystemConfig.EMU_SCREEN_HEIGHT)
                        {
                            ay = SystemConfig.EMU_SCREEN_HEIGHT - 1;
                            up = true;
                        }
                    }

                    if (left)
                    {
                        bx = ax;
                        ax -= (rate + 3);

                        if (ax <= 0)
                        {
                            ax = 0;
                            left = false;
                        }
                    }
                    else
                    {
                        bx = ax;
                        ax += (rate + 3);

                        if (ax >= SystemConfig.EMU_SCREEN_WIDTH)
                        {
                            ax = SystemConfig.EMU_SCREEN_WIDTH - 1;
                            left = true;
                        }
                    }
                }
                


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
        static System.Windows.Int32Rect rect = new System.Windows.Int32Rect(0, 0, SystemConfig.DRAW_FRAME_WIDTH, SystemConfig.DRAW_FRAME_HEIGHT);
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
                    ParentWindow.SetLogText("fps: " + fps.ToString("N2") + "\nIPS: " + string.Format("{0:n0}", CPU.IPS));
                });
            }
            catch { }
        }
    }
}
