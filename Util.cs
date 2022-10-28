using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextVideo
{
    public static class Util
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public override string ToString() => $"X: {Left}, Y:{Top}, Width: {Right - Left}, Height: {Bottom - Top}";
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x, y;

            public override string ToString() => $"X: {x}, Y: {y}";
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public uint Length;
            public uint Flags;
            public uint ShowCmd;
            public POINT MinPosition;
            public POINT MaxPosition;
            public RECT NormalPosition;
            public static WINDOWPLACEMENT Default
            {
                get {
                    WINDOWPLACEMENT instance = new WINDOWPLACEMENT();
                    instance.Length = (uint)Marshal.SizeOf(instance);
                    return instance;
                }
            }
        }

        static IntPtr hWnd;

        static Util()
        {
            hWnd = GetConsoleWindow();
        }

        public static WINDOWPLACEMENT GetConsolePos()
        {
            WINDOWPLACEMENT placement = WINDOWPLACEMENT.Default;
            GetWindowPlacement(hWnd, ref placement);
            return placement;
        }

        public static void GetCharPosUnderMouse(out int charX, out int charY, int fontSize = 14)
        {
            int x = Cursor.Position.X;
            int y = Cursor.Position.Y;
            WINDOWPLACEMENT placement = GetConsolePos();
            int wx = placement.NormalPosition.Left;
            int wy = placement.NormalPosition.Top;
            x -= wx + 70;
            y -= wy;

            charX = x / (fontSize / 2);
            charY = (y - 32) / fontSize;
        }
    }
}
