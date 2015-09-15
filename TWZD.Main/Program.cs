using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TWZD.Main
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Variable.MainForm = new MainFrm();
            Application.Run(Variable.MainForm);
        }
    }
}
