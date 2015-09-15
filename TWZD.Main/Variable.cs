using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace TWZD.Main
{
    public delegate void FormNotifier(object sender, object param);

    public static class Variable
    {
        public static int AlertIntervalMin = 60;
        public static int ShowIntervalMin = 5;
        public static int FadeIntervalSec = 2;
        public static double AlertOpacity = 0.3;
        public static double PhraseOpacity = 0.6;
        public static int Sensitivity = 50;
        public static int WebCamID = 0;
        public static bool DisplayCam = false;

        public static Form MainForm
        {
            get
            {
                return _mainForm;
            }
            set
            {
                _mainForm = value;
                _mainForm.StartPosition = FormStartPosition.Manual;
                _mainForm.Width = SystemInformation.WorkingArea.Width;
                _mainForm.Height = SystemInformation.WorkingArea.Height;
                _mainForm.Location = (Point)new Size(SystemInformation.WorkingArea.X,
                    SystemInformation.WorkingArea.Y);
                _mainForm.MinimumSize = _mainForm.MaximumSize = _mainForm.Size;
            }
        }
        public static FormNotifier MainFormNotify;
        static Form _mainForm;
    }
}
