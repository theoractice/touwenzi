using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace TWZD.Main
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    class DetectFullScreen
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowRect(IntPtr hwnd, out RECT rc);

        private IntPtr desktopHandle;
        //Window handle for the desktop  
        private IntPtr shellHandle;
        //Window handle for the shell  
        //Get the handles for the desktop and shell now.  

        bool runningFullScreen = false;
        RECT appBounds;
        RECT deskBounds;
        IntPtr hWnd;
        //Rectangle screenBounds;

        internal bool Detect()
        {
            desktopHandle = GetDesktopWindow();
            GetWindowRect(desktopHandle, out deskBounds);
            shellHandle = GetShellWindow();
            //get the dimensions of the active window 
            hWnd = GetForegroundWindow();
            if (hWnd != null && !hWnd.Equals(IntPtr.Zero))
            {
                //Check we haven't picked up the desktop or the shell  
                if (!(hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)))
                {
                    GetWindowRect(hWnd, out appBounds);

                    if (appBounds.Left <= deskBounds.Left
                        && appBounds.Top <= deskBounds.Top
                        && appBounds.Right >= deskBounds.Right
                        && appBounds.Bottom >= deskBounds.Bottom)
                    {
                        runningFullScreen = true;
                    }

                    ////determine if window is fullscreen 
                    //screenBounds = Screen.FromHandle(hWnd).Bounds;
                    //if ((appBounds.Bottom - appBounds.Top) == screenBounds.Height && (appBounds.Right - appBounds.Left) == screenBounds.Width)
                    //{
                    //    runningFullScreen = true;
                    //}
                }
            }

            return runningFullScreen;
        }
    }
}
