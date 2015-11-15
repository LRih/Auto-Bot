using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoBot
{
    public class Hotkey
    {
        //===================================================================== API
        [DllImport("user32.DLL")]
        private static extern short GetAsyncKeyState(Keys vKey);

        //===================================================================== FUNCTIONS
        public static bool IsKeyDown(Keys key)
        {
            return GetAsyncKeyState(key) != 0;
        }
        public static void WaitUntilKeyUp(Keys key)
        {
            do Application.DoEvents();
            while (IsKeyDown(key));
        }
    }
}
