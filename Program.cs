using Accord.Video.FFMPEG;
using EventHook;
using EventHook.Hooks;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SystemPlus;
using SystemPlus.Extensions;
using SystemPlus.Utils;
using TextVideoViewer;
using static TextVideo.Util;
using Screen = TextVideoViewer.Screen;

namespace TextVideo
{
    public static class Program
    {
        const uint ENABLE_QUICK_EDIT = 0x0040;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static Screen screen;


        static Size vs;
        static int vt;
        static int vf;
        static string et;
        static bool ep;
        static string from = "";
        static string to = "";
        static int ex = 0;
        static int sumS = 1;
        static bool sumP = false;
        static int skip = 0;
        static byte fs = 1;

        static double vld = -1d;

        const int enterNumb = 7;

        private static void Draw(int state, int selected, bool pressed, bool[] bs, int enterState, bool clear = true)
        {
            screen.rect = Screen.SmallRect.Create(1, 1, 128, 36);
            if (clear)
                screen.Clear(ConsoleColor.Blue);

            ConsoleColor p = ConsoleColor.Cyan;
            ConsoleColor np = ConsoleColor.White;

            if (state == 0) {
                if (selected != 0)
                    screen.DrawBtn("Load From", np, ConsoleColor.Black, false, 6, 3);
                else
                    screen.DrawBtn("Load From", p, ConsoleColor.Black, pressed, 6, 3); // t + 8

                if (selected != 1)
                    screen.DrawBtn("Save To", np, ConsoleColor.Black, false, 23, 3);
                else
                    screen.DrawBtn("Save To", p, ConsoleColor.Black, pressed, 23, 3);

                if (selected != 2)
                    screen.DrawBtn("Square Pixels", np, ConsoleColor.Black, bs[2], 38, 3);
                else
                    screen.DrawBtn("Square Pixels", p, ConsoleColor.Black, bs[2], 38, 3);

                if (selected != 3)
                    screen.DrawBtn($"Size:{vs.Width}x{vs.Height}", np, ConsoleColor.Black, false, 59, 3);
                else
                    screen.DrawBtn($"Size:{vs.Width}x{vs.Height}", p, ConsoleColor.Black, pressed, 59, 3);

                if (selected != 4)
                    screen.DrawBtn($"Video Type:{vt}", np, ConsoleColor.Black, false, 79, 3);
                else
                    screen.DrawBtn($"Video Type:{vt}", p, ConsoleColor.Black, pressed, 79, 3);

                if (selected != 5)
                    screen.DrawBtn($"FPS:{vf}", np, ConsoleColor.Black, false, 99, 3);
                else
                    screen.DrawBtn($"FPS:{vf}", p, ConsoleColor.Black, pressed, 99, 3);

                if (selected != 6)
                    screen.DrawBtn("Exit", np, ConsoleColor.Black, false, 51, 30);
                else
                    screen.DrawBtn("Exit", p, ConsoleColor.Black, pressed, 51, 30);

                if (enterState == 0 && selected != enterNumb) {
                    screen.DrawBtn("Convert", ConsoleColor.Gray, ConsoleColor.Black, true, 67, 30);
                }
                else if (enterState == 0 && selected == enterNumb) {
                    screen.DrawBtn("Convert", ConsoleColor.Gray, ConsoleColor.Cyan, true, 67, 30);
                }
                else if (enterState == 1 && selected == enterNumb) {
                    screen.DrawBtn("Convert", p, ConsoleColor.Black, false, 67, 30);
                }
                else if (enterState == 1 && selected != enterNumb) {
                    screen.DrawBtn("Convert", np, ConsoleColor.Black, false, 67, 30);
                }
                else {
                    screen.DrawBtn("Convert", p, ConsoleColor.Black, true, 67, 30);
                }
            }
            else if (state == 1) {
                int width = et.Length + 4;
                if (width < 20)
                    width = 20;
                int x = 64 - width / 2;
                ex = x + width - 8;
                screen.FillRectC(ConsoleColor.White, x, 15, width, 9);
                screen.DrawString("Error", ConsoleColor.Red, x + 1, 15);
                screen.DrawString(et, ConsoleColor.Black, 64 - et.Length / 2, 18);
                screen.DrawBtn(" ok ", ConsoleColor.Cyan, ConsoleColor.Black, ep, x + width - 8, 19);
            }
            else if (state == 2) {
                int width = et.Length + 4;
                if (width < 20)
                    width = 20;
                int x = 64 - width / 2;
                ex = x + width - 8;
                screen.FillRectC(ConsoleColor.White, x, 15, width, 9);
                screen.DrawString("Info", ConsoleColor.Black, x + 1, 15);
                screen.DrawString(et, ConsoleColor.Black, 64 - et.Length / 2, 18);
                screen.DrawBtn(" ok ", ConsoleColor.Cyan, ConsoleColor.Black, ep, x + width - 8, 19);
            }
            else if (state == 3) {
                int width = 50;
                int height = 16;
                int x = 64 - width / 2;
                int y = 18 - height / 2;
                ex = x + width - 8;
                //Convert(from, to, vs, vf, skip, bs[2], (byte)vt, fs);
                screen.FillRectC(ConsoleColor.White, x, y, width, height);
                screen.DrawString("Summary", ConsoleColor.Black, x + 1, y);
                string f = Path.GetFileName(from);
                if (f.Length > 40)
                    f = "..." + f.Substring(f.Length - 37);
                screen.DrawString($"From: {f}", ConsoleColor.Black, x + 3, y + 2);
                string t = Path.GetFileName(to);
                if (t.Length > 40)
                    t = "..." + t.Substring(t.Length - 37);
                screen.DrawString($"To: {t}", ConsoleColor.Black, x + 3, y + 3);
                screen.DrawString($"Size: {vs}", ConsoleColor.Black, x + 3, y + 4);
                screen.DrawString($"FPS: {vf}", ConsoleColor.Black, x + 3, y + 5);
                screen.DrawString($"Video length: {MathPlus.Round(vld, 2)}s", ConsoleColor.Black, x + 3, y + 6);
                screen.DrawString($"Frames to skip: {skip}/{skip + 1}", ConsoleColor.Black, x + 3, y + 7);
                screen.DrawString($"Square pixels: {bs[2]}", ConsoleColor.Black, x + 3, y + 8);
                screen.DrawString($"Video type: {vt}", ConsoleColor.Black, x + 3, y + 9);
                string ps;
                switch (fs) {// 0 - 2, 1 - 6, 2 - 12, 3 - ?
                    case 0:
                        ps = "2";
                        break;
                    case 1:
                        ps = "6";
                        break;
                    case 2:
                        ps = "12";
                        break;
                    case 3:
                        ps = "??";
                        break;
                    default:
                        ps = "?";
                        break;
                }
                screen.DrawString($"Font size: {ps}", ConsoleColor.Black, x + 3, y + 10);

                if (sumS == 1)
                    screen.DrawBtn(" Ok ", p, ConsoleColor.Black, sumP, x + width - 8, y + height - 5);
                else
                    screen.DrawBtn(" Ok ", ConsoleColor.Gray, ConsoleColor.Black, false, x + width - 8, y + height - 5);
                if (sumS == 0)
                    screen.DrawBtn("Cancel", p, ConsoleColor.Black, sumP, x + width - 20, y + height - 5);
                else
                    screen.DrawBtn("Cancel", ConsoleColor.Gray, ConsoleColor.Black, false, x + width - 20, y + height - 5);
            }

            bool b = Screen.Draw(screen);
            Console.SetCursorPosition(0, 0);
            if (b == true)
                Console.Write($"{state}, {selected}, {pressed}  Drawn        ");
            else
                Console.Write($"{state}, {selected}, {pressed}  Noooo        ");
        }

        [STAThread]
        static void Main(string[] args)
        {
            Screen.Init();

            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (GetConsoleMode(consoleHandle, out consoleMode)) {

                // Clear the quick edit bit in the mode flags
                consoleMode &= ~ENABLE_QUICK_EDIT;

                // set the new mode
                SetConsoleMode(consoleHandle, consoleMode);
            }

            screen = new Screen(1, 1, 128, 36);
            ConsoleExtensions.SetFontSize(14);
            Console.SetBufferSize(130, 37);
            Console.SetWindowSize(130, 37);
            Console.OutputEncoding = Encoding.Unicode;
            Console.CursorVisible = false;


            vs = new Size(256, 144);
            vt = 0;
            vf = 15;
            et = "";
            ep = false;
            to = "";
            from = "";


            int ns = 8 - 1; // selectable count

            bool[] bs = new bool[ns + 1];
            bs[2] = true;
            int enterState = 0; // 0 - locked, 1 - unlocked, 2 - pressed

            int state = 0;
            int selected = 0;
            bool pressed = false;

            Draw(state, selected, pressed, bs, enterState);

            EventHookFactory factory = new EventHookFactory();

            bool free = false;
            bool simE = false;
            
            MouseWatcher watcher = factory.GetMouseWatcher();
            watcher.Start();
            watcher.OnMouseInput += (s, e) =>
            {
                if (e.Message == MouseMessages.WM_LBUTTONDOWN && free) {
                    Util.GetCharPosUnderMouse(out int chx, out int chy, 14);

                    if (chx >= 6 && chx <= 15 && chy >= 3 && chy <= 5 && state == 0) { // btn from
                        selected = 0;
                        simE = true;
                    }
                    else if (chx >= 23 && chx <= 31 && chy >= 3 && chy <= 5 && state == 0) { // btn to
                        selected = 1;
                        simE = true;
                    }
                    else if (chx >= 38 && chx <= 52 && chy >= 3 && chy <= 5 && state == 0 && bs[2] == false) { // btn square pixels realesed
                        selected = 2;
                        simE = true;
                    }
                    else if (chx >= 39 && chx <= 53 && chy >= 4 && chy <= 6 && state == 0 && bs[2] == true) { // btn square pixels pressed
                        selected = 2;
                        simE = true;
                    }
                    else if (chx >= 59 && chx < 59 + $"Size:{vs.Width}x{vs.Height}".Length + 2 && chy >= 3 && chy <= 5 && state == 0) { // btn size
                        selected = 3;
                        simE = true;
                    }
                    else if (chx >= 79 && chx < 93 && chy >= 3 && chy <= 5 && state == 0) { // btn video type
                        selected = 4;
                        simE = true;
                    }
                    else if (chx >= 99 && chx < 107 && chy >= 3 && chy <= 5 && state == 0) { // btn fps
                        selected = 5;
                        simE = true;
                    }
                    else if (chx >= 51 && chx < 57 && chy >= 30 && chy <= 32 && state == 0) { // btn close/exit
                        selected = enterNumb - 1;
                        simE = true;
                    }
                    else if (chx >= 68 && chx <= 76 && chy >= 30 && chy <= 32 && state == 0) { // btn convert
                        selected = enterNumb;
                        simE = true;
                    }
                    else if (chx >= ex && chx < ex + 6 && chy >= 19 && chy <= 21 && (state == 1 || state == 2)) { // btn error/info ok 
                        simE = true;
                    }
                    else if (state == 3) {
                        int width = 50;
                        int height = 16;
                        int x = 64 - width / 2;
                        int y = 18 - height / 2;
                        if (chx >= x + width - 20 && chx < x + width - 20 + 8 && chy >= y + height - 5 && chy <= y + height - 3) { // cancel
                            sumS = 0;
                            simE = true;
                        }
                        else if (chx >= x + width - 8 && chx < x + width - 8 + 6 && chy >= y + height - 5 && chy <= y + height - 3) { // ok
                            sumS = 1;
                            simE = true;
                        }
                    }
                }
            };

            loop:
            while (true) {
                if (to != "" && from != "" && enterState == 0)
                    enterState = 1;
                free = true;
                ConsoleKeyInfo info;
                while (!Console.KeyAvailable && !simE) { }
                if (Console.KeyAvailable)
                    info = Console.ReadKey(true);
                else
                    info = new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);
                ConsoleKey key = info.Key;
                while (Console.KeyAvailable) { Console.ReadKey(true); }

                free = false;
                if (state == 0) {
                    if (key == ConsoleKey.RightArrow) {
                        selected++;
                        if (selected > ns)
                            selected = 0;
                        pressed = false;
                    }
                    else if (key == ConsoleKey.LeftArrow) {
                        selected--;
                        if (selected < 0)
                            selected = ns;
                        pressed = false;
                    }
                    else if (key == ConsoleKey.Enter) {
                        pressed = !pressed;
                        bs[selected] = !bs[selected];


                        if (selected == 3) {
                            if (vs == new Size(256, 144))
                                vs = new Size(128, 72);
                            else
                                vs = new Size(256, 144);
                        }
                        else if (selected == 4) {
                            if (vt == 0)
                                vt = 1;
                            else
                                vt = 0;
                        }
                        else if (selected == 5) {
                            if (vf == 30)
                                vf = 15;
                            else
                                vf = 30;
                        }
                        else if (selected == enterNumb && enterState == 1)
                            enterState = 2;

                        Draw(state, selected, pressed, bs, enterState);

                        if (selected == 0) {
                            OpenFileDialog dialog = new OpenFileDialog()
                            {
                                CheckFileExists = true,
                                Multiselect = false,
                                SupportMultiDottedExtensions = true,
                                ShowHelp = false,
                                Title = "Select .mp4 file",
                                Filter = "Video|*.mp4",
                                CheckPathExists = true,
                            };
                            NativeWindow nw = NativeWindow.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
                            DialogResult res = dialog.ShowDialog(nw);
                            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

                            if (res != DialogResult.OK) {
                                state = 1;
                                et = "File not selected";
                                Draw(0, selected, pressed, bs, enterState);
                                Draw(state, selected, pressed, bs, enterState, false);
                                goto end;
                            }
                            else if (!File.Exists(dialog.FileName)) {
                                state = 1;
                                et = "File doesn't exist";
                                Draw(0, selected, pressed, bs, enterState);
                                Draw(state, selected, pressed, bs, enterState, false);
                                goto end;
                            }
                            else if (Path.GetExtension(dialog.FileName) != ".mp4") {
                                state = 1;
                                et = "File isn't mp4";
                                Draw(0, selected, pressed, bs, enterState);
                                Draw(state, selected, pressed, bs, enterState, false);
                                goto end;
                            }

                            from = dialog.FileName;

                            state = 2;
                            et = "Saved path";
                            Draw(0, selected, pressed, bs, enterState);
                            Draw(state, selected, pressed, bs, enterState, false);
                            goto end;
                        }
                        else if (selected == 1) {
                            SaveFileDialog dialog = new SaveFileDialog()
                            {
                                SupportMultiDottedExtensions = true,
                                ShowHelp = false,
                                Title = "Save result as",
                                Filter = "TextVideo|*.tvf",
                                CheckPathExists = true,
                                DefaultExt = ".tvf",
                                AddExtension = true,
                                OverwritePrompt = false
                            };
                            NativeWindow nw = NativeWindow.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
                            DialogResult res = dialog.ShowDialog(nw);
                            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

                            if (res != DialogResult.OK) {
                                state = 1;
                                et = "File not selected";
                                Draw(0, selected, pressed, bs, enterState);
                                Draw(state, selected, pressed, bs, enterState, false);
                                goto end;
                            }
                            else if (Path.GetExtension(dialog.FileName) != ".tvf") {
                                state = 1;
                                et = "Extension isn't .tvf";
                                Draw(0, selected, pressed, bs, enterState);
                                Draw(state, selected, pressed, bs, enterState, false);
                                goto end;
                            }

                            to = dialog.FileName;

                            state = 2;
                            et = "Saved path";
                            Draw(0, selected, pressed, bs, enterState);
                            Draw(state, selected, pressed, bs, enterState, false);
                            goto end;
                        }

                        if (selected == 0 || selected == 1 || selected == 3 || selected == 4 || selected == 5 || (selected == enterNumb && enterState == 2))
                            Thread.Sleep(250);
                        else
                            Thread.Sleep(100);

                        if (selected == enterNumb && enterState == 2)
                            goto escape;
                        else if (selected == 6)
                            Environment.Exit(0);

                        pressed = !pressed;
                    }
                    else if (key == ConsoleKey.Escape)
                        Environment.Exit(0);
                    Draw(state, selected, pressed, bs, enterState);
                    end: { }
                } else if (state == 1) {
                    Draw(0, selected, pressed, bs, enterState);
                    Draw(state, selected, pressed, bs, enterState, false);

                    if (key == ConsoleKey.Enter) {
                        ep = true;
                        Draw(0, selected, pressed, bs, enterState);
                        Draw(state, selected, pressed, bs, enterState, false);
                        Thread.Sleep(300);
                        ep = false;
                        state = 0;

                        if (selected == 0 || selected == 1 || selected == 3 || selected == 4 || selected == 5)
                            Thread.Sleep(250);

                        pressed = !pressed;

                        Draw(state, selected, pressed, bs, enterState);
                    }
                }
                else if (state == 2) {
                    Draw(0, selected, pressed, bs, enterState);
                    Draw(state, selected, pressed, bs, enterState, false);

                    if (key == ConsoleKey.Enter) {
                        ep = true;
                        Draw(0, selected, pressed, bs, enterState);
                        Draw(state, selected, pressed, bs, enterState, false);
                        Thread.Sleep(300);
                        ep = false;
                        state = 0;

                        if (selected == 0 || selected == 1 || selected == 3 || selected == 4 || selected == 5)
                            Thread.Sleep(250);

                        pressed = !pressed;

                        Draw(state, selected, pressed, bs, enterState);
                    }
                }
                else if (state == 3) {
                    if (key == ConsoleKey.LeftArrow || key == ConsoleKey.RightArrow) {
                        if (sumS == 0)
                            sumS = 1;
                        else
                            sumS = 0;
                    }
                    else if (key == ConsoleKey.Enter) {
                        sumP = true;
                        Draw(0, selected, pressed, bs, enterState);
                        Draw(state, selected, pressed, bs, enterState, false);
                        Thread.Sleep(250);
                        if (sumS == 0) {
                            sumP = false;
                            sumS = 1;
                            enterState = 1;
                            state = 0;
                        }
                        else {
                            Console.Clear();
                            Convert(from, to, vs, vf, skip, bs[2], (byte)vt, fs);

                            Console.Title = "Done";
                            Console.WriteLine("Press any key to exit...");
                            Console.ReadKey(true);
                            Environment.Exit(0);
                        }
                    }

                    Draw(0, selected, pressed, bs, enterState);
                    Draw(state, selected, pressed, bs, enterState, false);
                }

                simE = false;
                free = true;
                Thread.Sleep(10);
                while (Console.KeyAvailable) { Console.ReadKey(true); }
            }

            escape:

            int framerate = 30;

            try {
                VideoFileReader reader = new VideoFileReader();
                reader.Open(from);
                framerate = (int)reader.FrameRate.Value;
                vld = reader.FrameCount / reader.FrameRate.Value;
            } catch {
                framerate = -1;
            }
            
            if (framerate != -1) {
                fs = 1;
                if (vs == new Size(128, 72))
                    fs = 2;

                if (vf == framerate)
                    skip = 0;
                else {
                    if (framerate > 60) {
                        state = 1;
                        et = $"Video frame rate to high ({framerate})";
                        Draw(0, selected, pressed, bs, enterState);
                        Draw(state, selected, pressed, bs, enterState, false);
                        simE = false;
                        free = true;
                        Thread.Sleep(10);
                        while (Console.KeyAvailable) { Console.ReadKey(true); }
                        goto loop;
                    }

                    if (framerate > 30)
                        framerate = 60;
                    else if (framerate > 15)
                        framerate = 30;
                    else
                        framerate = 15;

                    if (framerate / 2 == vf)
                        skip = 1;
                    else if (framerate == vf)
                        skip = 0;
                    else if (framerate < vf) {
                        vf = framerate;
                        skip = 0;
                    }
                    else
                        skip = 3;
                }

                state = 3;
                Draw(0, selected, pressed, bs, enterState);
                Draw(state, selected, pressed, bs, enterState, false);
                sumS = 1;
                sumP = false;
                simE = false;
                free = true;
                Thread.Sleep(10);
                while (Console.KeyAvailable) { Console.ReadKey(true); }
                goto loop;
            } else {
                Console.WriteLine("Video coudn't be opened, may be corrupted");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        static unsafe void Convert(string path, string pathTo, Size size, int fps = -1, int framesToSkip = 0, bool squarePixels = true, byte type = 0, byte fontSize = 1)
        {
            VideoFileReader reader = new VideoFileReader();
            reader.Open(path);

            Size vSize = new Size(reader.Width, reader.Height);

            if (vSize.Width == 0 || vSize.Height == 0) {
                Console.WriteLine("Width or Height is zero");
                reader.Close();
                reader.Dispose();
                return;
            }

            if (type == 0)
                Convertor.bsc = Convertor.brightnesStringInv;
            else if (type == 1)
                Convertor.bsc = Convertor.brightnesString;

            Convertor.Init();

            double fpsd = 15d;

            if (fps == -1)
                fpsd = reader.FrameRate.Value;
            else
                fpsd = (double)fps;

            File.WriteAllBytes(pathTo, new byte[0]);

            FileStream stream = new FileStream(pathTo, FileMode.Open, FileAccess.Write, FileShare.Read);

            stream.Write(new byte[8], 0, 8); // leave place for checksum
            stream.Write(new byte[4], 0, 4); // leave place for frame count, reader.FrameCount not reliable
            stream.Write(BitConverter.GetBytes(fpsd), 0, 8); // write fps
            stream.Write(BitConverter.GetBytes((ushort)size.Width), 0, 2); // Width
            stream.Write(BitConverter.GetBytes((ushort)size.Height), 0, 2); // Height
            VideoConfig config = new VideoConfig(squarePixels, 0, type, fontSize);
            stream.WriteByte(config.Value); // video config


            Stopwatch watch = new Stopwatch();

            int c = 0;
            int cm = 15;
            if (fpsd < 15)
                cm = (int)fpsd;

            uint realFrameCount = 0;


            if (framesToSkip == 1)
                for (int i = 0; i < reader.FrameCount / 2; i++) {
                    watch.Start();
                    Bitmap _bm = reader.ReadVideoFrame();
                    _bm?.Dispose();
                    Bitmap bm = reader.ReadVideoFrame();
                    if (bm == null)
                        continue;

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    byte[] b = Convertor.ConvertToBytes(db, size.Width, size.Height, squarePixels);
                    stream.Write(b, 0, b.Length);

                    db.Dispose();
                    bm.Dispose();

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Saving FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)} {i}/{reader.FrameCount / 2 - 1}";
                    }

                    c++;

                    realFrameCount++;

                    watch.Reset();
                }
            else if (framesToSkip == 2)
                for (int i = 0; i < reader.FrameCount / 3; i++) {
                    watch.Start();
                    Bitmap _bm = reader.ReadVideoFrame();
                    _bm?.Dispose();
                    _bm = reader.ReadVideoFrame();
                    _bm?.Dispose();
                    Bitmap bm = reader.ReadVideoFrame();
                    if (bm == null)
                        continue;

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    byte[] b = Convertor.ConvertToBytes(db, size.Width, size.Height, squarePixels);
                    stream.Write(b, 0, b.Length);

                    db.Dispose();
                    bm.Dispose();

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Saving FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)} {i}/{reader.FrameCount / 3 - 1}";
                    }

                    c++;

                    realFrameCount++;

                    watch.Reset();
                }
            else if (framesToSkip >= 3)
                for (int i = 0; i < reader.FrameCount / 4; i++) {
                    watch.Start();
                    Bitmap _bm = reader.ReadVideoFrame(i * 3);
                    _bm?.Dispose();
                    _bm = reader.ReadVideoFrame(i * 3);
                    _bm?.Dispose();
                    _bm = reader.ReadVideoFrame(i * 3);
                    _bm?.Dispose();
                    Bitmap bm = reader.ReadVideoFrame(i * 3 + 2);
                    if (bm == null)
                        continue;

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    byte[] b = Convertor.ConvertToBytes(db, size.Width, size.Height, squarePixels);
                    stream.Write(b, 0, b.Length);

                    db.Dispose();
                    bm.Dispose();

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Saving FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)} {i}/{reader.FrameCount / 4}";
                    }

                    c++;

                    realFrameCount++;

                    watch.Reset();
                }
            else
                for (int i = 0; i < reader.FrameCount; i++) {
                    watch.Start();

                    Bitmap bm = reader.ReadVideoFrame();
                    if (bm == null)
                        continue;

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    byte[] b = Convertor.ConvertToBytes(db, size.Width, size.Height, squarePixels);
                    stream.Write(b, 0, b.Length);

                    db.Dispose();
                    bm.Dispose();

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Saving FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)} {i}/{reader.FrameCount - 1}";
                    }

                    c++;

                    realFrameCount++;

                    watch.Reset();
                }


            reader.Close();
            reader.Dispose();

            stream.Seek(8, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes(realFrameCount), 0, 4);

            stream.Flush();
            stream.Close();
            stream.Dispose();

            Console.Clear();
            ConsoleExtensions.SetFontSize(16);
            Console.WriteLine("Generating checksum...");

            byte[] bytes = File.ReadAllBytes(pathTo);

            ulong checksum = 0;

            for (int i = 0; i < bytes.Length; i++)
                checksum += bytes[i];

            stream = new FileStream(pathTo, FileMode.Open, FileAccess.Write, FileShare.Read);
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes(checksum), 0, 8);

            stream.Flush();
            stream.Close();
            stream.Dispose();

            Console.WriteLine("Done");
        }

        /*static unsafe void ConvertAndView(string path, Size size, int fps = -1, int framesToSkip = 0)
        {
            VideoFileReader reader = new VideoFileReader();
            reader.Open(path);

            Size vSize = new Size(reader.Width, reader.Height);

            if (vSize.Width == 0 || vSize.Height == 0) {
                Console.WriteLine("Width or Height is zero");
                reader.Close();
                reader.Dispose();
                return;
            }

            double fpsd = 15d;

            if (fps == -1)
                fpsd = reader.FrameRate.Value;
            else
                fpsd = (double)fps;

            long milis = (long)((1d / fpsd) * 1000d);

            Console.Clear();
            Console.WriteLine($"Loaded video: Fps: {MathPlus.Round(fpsd, 2)}({milis}milis/frame), size: {vSize}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);

            Stopwatch watch = new Stopwatch();

            int c = 0;
            int cm = 15;
            if (fpsd < 15)
                cm = (int)fpsd;

            if (framesToSkip == 1)
                for (int i = 0; i < reader.FrameCount; i++) {
                    watch.Start();
                    Bitmap _bm = reader.ReadVideoFrame(i * 2);
                    _bm.Dispose();
                    Bitmap bm = reader.ReadVideoFrame(i * 2 + 1);
                    if (bm == null)
                        continue;

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    Console.SetCursorPosition(0, 0);
                    Console.Write(new string(Convertor.ConvertNN(db, size.Width, size.Height, true)));

                    db.Dispose();
                    bm.Dispose();

                    while (watch.ElapsedMilliseconds < milis) { }

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Playback FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)}";
                    }

                    c++;

                    watch.Reset();
                }
            else if (framesToSkip == 2)
                for (int i = 0; i < reader.FrameCount; i++) {
                    watch.Start();
                    Bitmap _bm = reader.ReadVideoFrame(i * 3);
                    _bm.Dispose();
                    _bm = reader.ReadVideoFrame(i * 3);
                    _bm.Dispose();
                    Bitmap bm = reader.ReadVideoFrame(i * 3 + 2);

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    Console.SetCursorPosition(0, 0);
                    Console.Write(new string(Convertor.ConvertNN(db, size.Width, size.Height, true)));

                    db.Dispose();
                    bm.Dispose();

                    while (watch.ElapsedMilliseconds < milis) { }

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Playback FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)}";
                    }

                    c++;

                    watch.Reset();
                }
            else if (framesToSkip >= 3)
                for (int i = 0; i < reader.FrameCount; i++) {
                    watch.Start();
                    Bitmap _bm = reader.ReadVideoFrame(i * 3);
                    _bm.Dispose();
                    _bm = reader.ReadVideoFrame(i * 3);
                    _bm.Dispose();
                    _bm = reader.ReadVideoFrame(i * 3);
                    _bm.Dispose();
                    Bitmap bm = reader.ReadVideoFrame(i * 3 + 2);

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    Console.SetCursorPosition(0, 0);
                    Console.Write(new string(Convertor.ConvertNN(db, size.Width, size.Height, true)));

                    db.Dispose();
                    bm.Dispose();

                    while (watch.ElapsedMilliseconds < milis) { }

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Playback FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)}";
                    }

                    c++;

                    watch.Reset();
                }
            else
                for (int i = 0; i < reader.FrameCount; i++) {
                    watch.Start();

                    Bitmap bm = reader.ReadVideoFrame();

                    DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                    Console.SetCursorPosition(0, 0);
                    Console.Write(new string(Convertor.ConvertNN(db, size.Width, size.Height, true)));

                    db.Dispose();
                    bm.Dispose();

                    while (watch.ElapsedMilliseconds < milis) { }

                    if (c > cm) {
                        c = 0;
                        Console.Title = $"Playback FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 2)}";
                    }

                    c++;

                    watch.Reset();
                }

            reader.Close();
            reader.Dispose();
        }*/

        // l 500 - Avarage: 0,0137845044, Min: 0,0118371, Max: 0,049416 70/s
        // nl 500 - Avarage: 0,0129760244, Min: 0,0112829, Max: 0,0525752 75/s
        // nw 500 - Avarage: 0,0098821886, Min: 0,0083184, Max: 0,0308904, 101/s
        // relese 500 - Avarage: 0,0125115774, Min: 0,0110258, Max: 0,0265575, 79,9259732030271/s
        // relese debus s+ 500 - Avarage: 0,0145324256, Min: 0,013266, Max: 0,027478, 68,8116373360274/s

        // text - Avarage: 0,0168378002, Min: 0,0133566, Max: 0,1398259, 59,3901809097366/s
        // bytes - Avarage: 0,0161565572, Min: 0,0133376, Max: 0,1183355, 61,8943743782246/s

        static void Test(string path, string toPath, Size size, int fps)
        {
            double min = 1000d;
            double max = 0d;
            double avrage = 0d;
            int t = 0;
            Stopwatch watch = new Stopwatch();
            for (int i = 0; i < 500; i++) {
                VideoFileReader reader = new VideoFileReader();
                reader.Open(path);

                Size vSize = new Size(reader.Width, reader.Height);

                if (vSize.Width == 0 || vSize.Height == 0) {
                    Console.WriteLine("Width or Height is zero");
                    reader.Close();
                    reader.Dispose();
                    return;
                }

                watch.Start();

                Bitmap bm = reader.ReadVideoFrame(0);
                DirectBitmap db = DirectBitmap.LoadFromBm(bm, false);

                File.WriteAllBytes(toPath, Convertor.ConvertToBytes(db, size.Width, size.Height, false));

                db.Dispose();
                bm.Dispose();

                watch.Stop();

                avrage += watch.Elapsed.TotalSeconds;
                t++;
                if (watch.Elapsed.TotalSeconds < min)
                    min = watch.Elapsed.TotalSeconds;
                if (watch.Elapsed.TotalSeconds > max)
                    max = watch.Elapsed.TotalSeconds;

                watch.Reset();

                reader.Close();
                reader.Dispose();
            }

            avrage = avrage / (double)t;

            Console.WriteLine($"Avarage: {avrage}, Min: {min}, Max: {max}, {1d / avrage}/s");
        }
    }
}
