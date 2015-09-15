using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using TWZD.Data;
using TWZD.Main.Properties;
using System.Timers;
using System.Threading;
using System.IO;

namespace TWZD.Main
{
    internal delegate void FrameCallBack(float x, float y, float interval, IntPtr image, bool update);
    internal delegate void QuitCallBack(bool isCamError);

    public partial class MainFrm : Form
    {
        static FrameCallBack frameCallBack;
        static QuitCallBack quitCallBack;

        int testID;
        bool quitFlag = false;

        System.Timers.Timer winTimer;
        System.Timers.Timer fadeTimer;
        WinState frmState;
        double frmOpacity = 0;
        Stopwatch watch;
        SQLiteMgr sqlMgr;
        StrokeMgr strokeMgr;
        string currentChar;
        Random rand;
        bool camUsable;
        Icon appIcon;

        public MainFrm()
        {
            this.InitializeComponent();
            CVDllImport.CVInit();
            Directory.SetCurrentDirectory(Application.StartupPath);
            this.frmState = MainFrm.WinState.Halt;
            this.sqlMgr = new SQLiteMgr(".", "twz", typeof(TWZDData));
            this.watch = new Stopwatch();
            this.rand = new Random();
            this.ReadConfig();
            Variable.MainFormNotify = new FormNotifier(this.DoNotify);
            if (Environment.OSVersion.Version.Major <= 5)
            {
                this.appIcon = Resources.Yong16color;
            }
            else
            {
                this.appIcon = Resources.Yong16bw;
            }
            this.notifyIcon1.Icon = this.appIcon;
            base.Opacity = 0.0;
            this.strokeMgr = new StrokeMgr("", this.pictureBox1);
            int initialStyle = CVDllImport.GetWindowLong(base.Handle, -20);
            int WS_EX_TOOLWINDOW = 128;
            int WS_EX_TRANSPARENT = 32;
            CVDllImport.SetWindowLong(base.Handle, -20, initialStyle | 524288 | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
            this.winTimer = new System.Timers.Timer((double)(Variable.AlertIntervalMin * 60 * 1000));
            this.fadeTimer = new System.Timers.Timer(50.0);
            this.winTimer.Elapsed += new ElapsedEventHandler(this.OnAlert);
            this.fadeTimer.Elapsed += new ElapsedEventHandler(this.DoAllTask);
            Control.CheckForIllegalCrossThreadCalls = false;
            this.winTimer.Enabled = true;
        }

        private void ReadConfig()
        {
            DataTable dtCfg = sqlMgr.SelectFromTable("UserConfig", "rowid", "1");
            Variable.AlertIntervalMin = int.Parse((string)dtCfg.Rows[0]["工作时间"]);
            Variable.ShowIntervalMin = int.Parse((string)dtCfg.Rows[0]["休息时间"]);
            Variable.FadeIntervalSec = int.Parse((string)dtCfg.Rows[0]["显示时间"]);
            Variable.AlertOpacity = double.Parse((string)dtCfg.Rows[0]["提示透明度"]);
            Variable.PhraseOpacity = double.Parse((string)dtCfg.Rows[0]["文字透明度"]);
            Variable.Sensitivity = int.Parse((string)dtCfg.Rows[0]["体感灵敏度"]);
            Variable.WebCamID = int.Parse((string)dtCfg.Rows[0]["摄像机编号"]);
            Variable.DisplayCam = bool.Parse((string)dtCfg.Rows[0]["小窗显示"]);
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            quitFlag = true;

            if (this.frmState == WinState.Working)
            {
                strokeMgr.Done = true;
            }
            else
            {
                Application.Exit();
            }
        }

        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsFrm optFrm = new OptionsFrm();
            optFrm.Icon = appIcon;
            optFrm.ShowDialog();
            if (optFrm.DialogResult == DialogResult.OK)
            {
                UserConfig cfg = new UserConfig();
                cfg.工作时间 = Variable.AlertIntervalMin.ToString();
                cfg.休息时间 = Variable.ShowIntervalMin.ToString();
                cfg.显示时间 = Variable.FadeIntervalSec.ToString();
                cfg.提示透明度 = Variable.AlertOpacity.ToString();
                cfg.文字透明度 = Variable.PhraseOpacity.ToString();
                cfg.体感灵敏度 = Variable.Sensitivity.ToString();
                cfg.摄像机编号 = Variable.WebCamID.ToString();
                cfg.小窗显示 = Variable.DisplayCam.ToString();
                sqlMgr.ResetTable(typeof(UserConfig));
                sqlMgr.AddToDB(cfg);

                winTimer.Stop();
                winTimer.Interval = Variable.AlertIntervalMin * 60 * 1000;
                winTimer.Start();
            }
        }

        private void 预览ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnAlert(sender, null);
        }

        private void OnData(float x, float y, float interval, IntPtr Image, bool update)
        {
            if (Variable.DisplayCam)
            {
                pictureBox2.Image = new Bitmap(320, 240, 960,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb, Image);
            }

            if (update)
            {
                strokeMgr.OnMove(x, y, interval);
            }
        }

        private void OnQuit(bool isCamError)
        {
            this.strokeMgr.Done = true;
        }

        private void PreAlert()
        {
            this.CheckCam();
            MainFrm.frameCallBack = new FrameCallBack(this.OnData);
            MainFrm.quitCallBack = new QuitCallBack(this.OnQuit);
            CVDllImport.CVSetFrameEvent(MainFrm.frameCallBack);
            CVDllImport.CVSetQuitEvent(MainFrm.quitCallBack);

            testID = rand.Next(10000, 100000000) % 861;
            //testID = 10;

            DataTable dtPhrase = sqlMgr.SelectFromTable("Phrase", "rowid", testID.ToString());
            label词语.Text = (string)dtPhrase.Rows[0]["词语"];
            label注音.Text = (string)dtPhrase.Rows[0]["注音"];
            
            string temp = (string)dtPhrase.Rows[0]["释义"];
            if (temp.Length > 50)
            {
                label释义.Text = temp.Substring(0, 50) + "...";
            }
            else
            {
                label释义.Text = temp;
            }

            int pos = int.Parse((string)dtPhrase.Rows[0]["单字位置"]) - 1;
            currentChar = new string(label词语.Text[pos], 1);

            DataTable dtStroke = sqlMgr.SelectFromTable("StrokeOrder", "汉字", currentChar);
            strokeMgr = new StrokeMgr((string)dtStroke.Rows[0]["笔顺数据"], this.pictureBox1);
            pictureBox1.Image = null;
            pictureBox2.Image = null;

            this.Opacity = 0;
            if (!camUsable)
            {
                this.frmState = WinState.PhraseIn;
            }
            else
            {
                this.frmState = WinState.AlertIn;
            }

            退出ToolStripMenuItem.Enabled = false;
            设置ToolStripMenuItem.Enabled = false;
            预览ToolStripMenuItem.Text = "不写了";
            fadeTimer.Enabled = true;
            watch.Reset();
            watch.Start();
        }

        private void CheckCam()
        {
            this.camUsable = CVDllImport.CVTestCam(Variable.WebCamID);
        }

        private void DoNotify(object sender, object param)
        {
            if (null == sender)
            {
                return;
            }

            switch (sender.ToString())
            {
                case "setopacity":
                    this.Opacity = (double)param;
                    break;
                case "draw":
                    strokeMgr.Draw();
                    break;
                default:
                    break;
            }
        }

        void OnAlert(object source, System.Timers.ElapsedEventArgs e)
        {
            if ((DateTime.Now.Hour < 8) || (DateTime.Now.Hour > 18))
            {
                if (typeof(ToolStripMenuItem) != source.GetType())
                    return;
            }

            if (new DetectFullScreen().Detect())
            {
                return;
            }

            if (预览ToolStripMenuItem.Text == "寫個字")
            {
                if (this.frmState != WinState.Halt)
                {
                    return;
                }

                PreAlert();
                return;
            }

            if (预览ToolStripMenuItem.Text == "不寫了")
            {
                if (this.frmState == WinState.Working)
                {
                    strokeMgr.Done = true;
                }
            }
        }

        void DoAllTask(object source, System.Timers.ElapsedEventArgs e)
        {
            switch (this.frmState)
            {
                case WinState.AlertIn:
                    panel1.Visible = false;
                    panel1.BackColor = this.BackColor;
                    //this.TransparencyKey = Color.White;
                    frmOpacity = Variable.AlertOpacity * (watch.ElapsedMilliseconds / (Variable.FadeIntervalSec * 500.0));
                    if (frmOpacity >= Variable.AlertOpacity)
                    {
                        frmState = WinState.AlertOut;
                        frmOpacity = Variable.AlertOpacity;

                        watch.Reset();
                        watch.Start();
                    }
                    break;
                case WinState.AlertOut:
                    frmOpacity = Variable.AlertOpacity - Variable.AlertOpacity * (watch.ElapsedMilliseconds / (Variable.FadeIntervalSec * 500.0));
                    if (frmOpacity <= 0)
                    {
                        frmState = WinState.PhraseIn;
                        frmOpacity = 0;
                        //this.TransparencyKey = this.BackColor;
                        watch.Reset();
                        watch.Start();
                    }
                    break;
                case WinState.PhraseIn:
                    label词语.Text = label词语.Text.Replace(currentChar, "＿");
                    panel1.Visible = true;
                    if (this.BackColor.B == 255)
                        panel1.BackColor = Color.FromArgb(this.BackColor.ToArgb() - 1);
                    else
                        panel1.BackColor = Color.FromArgb(this.BackColor.ToArgb() + 1);

                    //panel1.Height = label释义.Height + 96;
                    frmOpacity = Variable.PhraseOpacity * (watch.ElapsedMilliseconds / (Variable.FadeIntervalSec * 1000.0));
                    if (frmOpacity >= Variable.PhraseOpacity)
                    {
                        frmState = WinState.Working;
                        frmOpacity = Variable.PhraseOpacity;

                        watch.Reset();
                        watch.Start();

                        if (!camUsable)
                        {
                            break;
                        }

                        fadeTimer.Interval = 15;

                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            CVDllImport.CVStart(Variable.WebCamID.ToString());
                        });
                    }
                    break;
                case WinState.Working:
                    if (strokeMgr.Done
                        || (watch.ElapsedMilliseconds > Variable.ShowIntervalMin * 60 * 1000))
                    {
                        CVDllImport.CVQuit();
                        frmState = WinState.PhraseOut;
                        fadeTimer.Interval = 50;

                        watch.Reset();
                        watch.Start();
                    }
                    else
                    {
                        strokeMgr.OnDraw();
                    }
                    break;
                case WinState.PhraseOut:
                    label词语.Text = label词语.Text.Replace("＿", currentChar);
                    frmOpacity = Variable.PhraseOpacity - Variable.PhraseOpacity * (watch.ElapsedMilliseconds / (Variable.FadeIntervalSec * 1000.0));
                    if (frmOpacity <= 0)
                    {

                        if (quitFlag)
                        {
                            Application.Exit();
                            return;
                        }
                        else
                        {
                            退出ToolStripMenuItem.Enabled = true;
                            设置ToolStripMenuItem.Enabled = true;
                            预览ToolStripMenuItem.Text = "写个字";
                            frmState = WinState.Halt;
                            frmOpacity = 0;
                            watch.Reset();
                            watch.Stop();
                        }
                    }
                    break;
                case WinState.Halt:
                    watch.Reset();
                    watch.Stop();
                    fadeTimer.Enabled = false;
                    break;
                default:
                    break;
            }

            Variable.MainForm.Invoke(Variable.MainFormNotify,
                new object[] { "setopacity", frmOpacity });
        }

        enum WinState
        {
            Halt,
            AlertIn,
            AlertOut,
            PhraseIn,
            Working,
            PhraseOut
        }
    }
}
