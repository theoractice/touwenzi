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
        public MainFrm()
        {
            InitializeComponent();

            Directory.SetCurrentDirectory(Application.StartupPath);

            sqlMgr = new SQLiteMgr(Environment.CurrentDirectory, "twz", typeof(TWZDData));
            strokeMgr = new StrokeMgr("", pictureBox1);
            watch = new Stopwatch();
            rand = new Random();
            winTimer = new System.Timers.Timer((double)(Variable.AlertIntervalMin * 60 * 1000));
            taskTimer = new System.Timers.Timer(50.0);
            winTimer.Elapsed += new ElapsedEventHandler(OnAlert);
            taskTimer.Elapsed += new ElapsedEventHandler(DoAllTask);
            winTimer.Enabled = true;

            frmState = MainFrm.WinState.Halt;
            appIcon = (Environment.OSVersion.Version.Major <= 5)
                ? Resources.Yong16color
                : Resources.Yong16bw;
            Opacity = 0.0;
            notifyIcon1.Icon = appIcon;

            ReadConfig();
            CVDllImport.CVInit();

            //鼠标穿透
            //int WS_EX_APPWINDOW = 0x00040000;
            int STYLE = CVDllImport.GetWindowLong(Handle, -20);
            int WS_EX_TOOLWINDOW = 0x00000080;
            int WS_EX_TRANSPARENT = 0x00000020;
            CVDllImport.SetWindowLong(Handle, -20,
                STYLE | 0x80000 | (WS_EX_TRANSPARENT) | WS_EX_TOOLWINDOW);
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

            if (frmState == WinState.Working)
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
            strokeMgr.Done = true;
        }

        private void PreAlert()
        {
            this.CheckCam();
            MainFrm.frameCallBack = new FrameCallBack(this.OnData);
            MainFrm.quitCallBack = new QuitCallBack(this.OnQuit);
            CVDllImport.CVSetFrameEvent(MainFrm.frameCallBack);
            CVDllImport.CVSetQuitEvent(MainFrm.quitCallBack);

            testID = rand.Next(phraseCount << 4, phraseCount << 5) % phraseCount;
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
            预览ToolStripMenuItem.Text = "不寫了";
            taskTimer.Enabled = true;
            watch.Reset();
            watch.Start();
        }

        private void CheckCam()
        {
            camUsable = CVDllImport.CVTestCam(Variable.WebCamID);
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
                    Invoke(new MethodInvoker(delegate()
                    {
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
                    }));
                    break;
                case WinState.AlertOut:
                    Invoke(new MethodInvoker(delegate()
                    {
                        frmOpacity = Variable.AlertOpacity - Variable.AlertOpacity * (watch.ElapsedMilliseconds / (Variable.FadeIntervalSec * 500.0));
                        if (frmOpacity <= 0)
                        {
                            frmState = WinState.PhraseIn;
                            frmOpacity = 0;
                            //this.TransparencyKey = this.BackColor;
                            watch.Reset();
                            watch.Start();
                        }
                    }));
                    break;
                case WinState.PhraseIn:
                    Invoke(new MethodInvoker(delegate()
                    {
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

                            if (camUsable)
                            {
                                taskTimer.Interval = 15;

                                ThreadPool.QueueUserWorkItem(delegate
                                {
                                    CVDllImport.CVStart(Variable.WebCamID.ToString());
                                });
                            }
                        }
                    }));
                    break;
                case WinState.Working:
                    Invoke(new MethodInvoker(delegate()
                    {
                        if (strokeMgr.Done
                            || (watch.ElapsedMilliseconds > Variable.ShowIntervalMin * 60 * 1000))
                        {
                            CVDllImport.CVQuit();
                            frmState = WinState.PhraseOut;
                            taskTimer.Interval = 50;

                            watch.Reset();
                            watch.Start();
                        }
                        else
                        {
                            strokeMgr.OnDraw();
                        }
                    }));
                    break;
                case WinState.PhraseOut:
                    Invoke(new MethodInvoker(delegate()
                    {
                        label词语.Text = label词语.Text.Replace("＿", currentChar);
                        frmOpacity = Variable.PhraseOpacity - Variable.PhraseOpacity * (watch.ElapsedMilliseconds / (Variable.FadeIntervalSec * 1000.0));
                        if (frmOpacity <= 0)
                        {

                            if (quitFlag)
                            {
                                Application.Exit();
                            }
                            else
                            {
                                退出ToolStripMenuItem.Enabled = true;
                                设置ToolStripMenuItem.Enabled = true;
                                预览ToolStripMenuItem.Text = "寫個字";
                                frmState = WinState.Halt;
                                frmOpacity = 0;
                                watch.Reset();
                                watch.Stop();
                            }
                        }
                    }));
                    break;
                case WinState.Halt:
                    Invoke(new MethodInvoker(delegate()
                    {
                        watch.Reset();
                        watch.Stop();
                        taskTimer.Enabled = false;
                    }));
                    break;
                default:
                    break;
            }

            Invoke(new MethodInvoker(delegate()
            {
                Opacity = frmOpacity;
            }));
        }

        static FrameCallBack frameCallBack;
        static QuitCallBack quitCallBack;

        int testID;
        int phraseCount = 625;
        bool quitFlag = false;

        System.Timers.Timer winTimer;
        System.Timers.Timer taskTimer;
        WinState frmState;
        double frmOpacity = 0;
        Stopwatch watch;
        SQLiteMgr sqlMgr;
        StrokeMgr strokeMgr;
        string currentChar;
        Random rand;
        bool camUsable;
        Icon appIcon;

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
