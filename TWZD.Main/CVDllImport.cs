using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace TWZD.Main
{
    internal static class CVDllImport
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("cvmain.dll")]
        internal static extern void CVSetFrameEvent(FrameCallBack callback);

        [DllImport("cvmain.dll")]
        internal static extern void CVSetQuitEvent(QuitCallBack callback);

        [DllImport("cvmain.dll")]
        //[return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CVInit();

        [DllImport("cvmain.dll")]
        internal static extern void CVStart(string file);

        [DllImport("cvmain.dll")]
        internal static extern void CVQuit();

        [DllImport("cvmain.dll")]
        internal static extern void CVWaitForQuit();

        [DllImport("cvmain.dll")]
        internal static extern int CVGetCamCount();

        [DllImport("cvmain.dll")]
        internal static extern string CVGetCamName(int id);

        [DllImport("cvmain.dll")]
        //[return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CVTestCam(int id);
    }
}
