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
            //处理UI线程异常
            Application.ThreadException += 
                new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //处理非UI线程异常
            AppDomain.CurrentDomain.UnhandledException += 
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Variable.MainForm = new MainFrm();
            Application.Run(Variable.MainForm);
        }

         static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
         {
             MessageBox.Show(
                 e.Exception.InnerException.Message, 
                 e.Exception.Message, 
                 MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
 
         static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
         {
             MessageBox.Show(
                 (e.ExceptionObject as Exception).InnerException.Message, 
                 (e.ExceptionObject as Exception).Message, 
                 MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
    }
}
