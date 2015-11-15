using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoBot
{
    public static class Macro
    {
        //===================================================================== API
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        //===================================================================== STRUCTS
        [Flags]
        private enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        //===================================================================== VARIABLES
        private static Random _randGen = new Random();

        //===================================================================== FUNCTIONS
        private static int Rand(int low, int high)
        {
            return _randGen.Next(low, high + 1);
        }

        public static void Wait(long ms)
        {
            // set now time
            long time = DateTime.Now.Ticks;
            // loop until specified wait time has passed
            do
            {
                Application.DoEvents();
            } while (time + ms * 10000 > DateTime.Now.Ticks);
        }
        public static void WaitUntilColor(Point position, Color color, int timeout = 10000)
        {
            long time = DateTime.Now.Ticks;
            do
            {
                Application.DoEvents();
            } while (GetPixel(position) != color && time + timeout * 10000 > DateTime.Now.Ticks);
        }
        public static void WaitWhileColor(Point position, Color color, int timeout = 30000)
        {
            long time = DateTime.Now.Ticks;
            do
            {
                Application.DoEvents();
            } while (GetPixel(position) == color && time + timeout * 10000 > DateTime.Now.Ticks);
        }
        public static void WaitUntilBrightness(Point position, int brightness, int timeout = 10000)
        {
            long time = DateTime.Now.Ticks;
            do
            {
                Application.DoEvents();
            } while (GetPixel(position).GetBrightness() * 100 < brightness && time + timeout * 10000 > DateTime.Now.Ticks);
        }
        public static void WaitWhileBrightness(Point position, int brightness, int timeout = 10000)
        {
            long time = DateTime.Now.Ticks;
            do
            {
                Application.DoEvents();
            } while (GetPixel(position).GetBrightness() * 100 > brightness && time + timeout * 10000 > DateTime.Now.Ticks);
        }

        public static void CursorMove(Point endPt, int speed = 20)
        {
            Point startPt = new Point((Size)Cursor.Position); // starting point

            PointF nextPt = new PointF(0, 0); // stores next point to move
            Size differencePt;

            double magnitude; // magnitude of total path
            SizeF moveSize = new SizeF(); // Stores size of next movement

            if (startPt == endPt) return;

            do
            {
                magnitude = Math.Sqrt(Math.Pow(endPt.X - Cursor.Position.X, 2) + Math.Pow(endPt.Y - Cursor.Position.Y, 2));

                // give the step size a magnitude of 1
                moveSize.Width = (float)((endPt.X - Cursor.Position.X) / magnitude);
                moveSize.Height = (float)((endPt.Y - Cursor.Position.Y) / magnitude);

                nextPt = PointF.Add(new PointF(Cursor.Position.X + nextPt.X.GetDecimal(), Cursor.Position.Y + nextPt.Y.GetDecimal()), moveSize);

                // Get the movement size
                differencePt = Size.Subtract(new Size(Convert.ToInt32(nextPt.X), Convert.ToInt32(nextPt.Y)), (Size)Cursor.Position);

                // Move the cursor to its next step
                Cursor.Position = Point.Add(Cursor.Position, differencePt);

                if (Rand(1, speed) == 1) Wait(10);

                Application.DoEvents();
            } while (!(Cursor.Position.X == endPt.X && Cursor.Position.Y == endPt.Y));
        }
        public static void LeftDown()
        {
            mouse_event((uint)MouseEventFlags.LEFTDOWN, 0, 0, 0, 0);
        }
        public static void LeftUp()
        {
            mouse_event((uint)MouseEventFlags.LEFTUP, 0, 0, 0, 0);
        }
        public static void LeftClick(int wait = 50)
        {
            LeftDown();
            Wait(wait);
            LeftUp();
        }
        public static void RightClick(int wait = 50)
        {
            mouse_event((uint)MouseEventFlags.RIGHTDOWN, 0, 0, 0, 0);
            Wait(wait);
            mouse_event((uint)MouseEventFlags.RIGHTUP, 0, 0, 0, 0);
        }

        public static Color GetPixel(Point position)
        {
            Color pixel;
            Bitmap bitmap = new Bitmap(1, 1);
            Graphics g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(position, new Point(0, 0), new Size(1, 1));
            pixel = bitmap.GetPixel(0, 0);
            g.Dispose();
            bitmap.Dispose();
            return pixel;
        }
        public static Color GetPixelEx(Point position)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, position.X, position.Y);
            ReleaseDC(IntPtr.Zero, hdc);
            return ColorTranslator.FromOle((int)pixel);
        }
    }
}
