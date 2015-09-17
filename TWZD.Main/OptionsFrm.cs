using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Threading;

namespace TWZD.Main
{
    public partial class OptionsFrm : Form
    {
        static FrameCallBack frameCallBack;
        static QuitCallBack quitCallBack;

        public OptionsFrm()
        {
            InitializeComponent();
            OptionsFrm.frameCallBack = new FrameCallBack(this.OnData);
            OptionsFrm.quitCallBack = new QuitCallBack(this.OnQuit);
            CVDllImport.CVSetFrameEvent(OptionsFrm.frameCallBack);
            CVDllImport.CVSetQuitEvent(OptionsFrm.quitCallBack);
            CVDllImport.CVInit();
            getCamList();

            textBox1.Text = Variable.AlertIntervalMin.ToString();
            textBox2.Text = Variable.ShowIntervalMin.ToString();
            textBox3.Text = Variable.FadeIntervalSec.ToString();
            textBox4.Text = Variable.AlertOpacity.ToString();
            textBox5.Text = Variable.PhraseOpacity.ToString();
            trackBar1.Value = Variable.Sensitivity;
            textBox6.Text = Variable.Sensitivity.ToString();
            comboBox1.SelectedIndex = Variable.WebCamID;
            checkBox1.Checked = Variable.DisplayCam;

            richTextBox1.LoadFile("FAQ.rtf");
        }

        private void OnData(float x, float y, float interval, IntPtr Image, bool OFState)
        {
            pictureBox1.Image = new Bitmap(320, 240, 960,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb, Image);
        }

        private void OnQuit(bool isCamError)
        {
        }

        private void getCamList()
        {
            try
            {
                comboBox1.Items.Clear();
                int count = CVDllImport.CVGetCamCount();
                if (0 == count)
                {
                    throw new ApplicationException();
                }

                for (int i = 0; i < count; i++)
                {
                    comboBox1.Items.Add(CVDllImport.CVGetCamName(i));
                }
                comboBox1.SelectedIndex = 0; //make dafault to first cam
            }
            catch (ApplicationException)
            {
                comboBox1.Items.Add("沒有攝像頭");
                comboBox1.SelectedIndex = 0;
                Variable.WebCamID = 0;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            CVDllImport.CVQuit();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            CVDllImport.CVQuit();
            CVDllImport.CVWaitForQuit();
            string id = comboBox1.SelectedIndex.ToString();
            ThreadPool.QueueUserWorkItem(delegate
            {
                CVDllImport.CVStart(id);
            });
        }

        private void accept_Click(object sender, EventArgs e)
        {
            Variable.AlertIntervalMin = int.Parse(textBox1.Text);
            Variable.ShowIntervalMin = int.Parse(textBox2.Text);
            Variable.FadeIntervalSec = int.Parse(textBox3.Text);
            Variable.AlertOpacity = double.Parse(textBox4.Text);
            Variable.PhraseOpacity = double.Parse(textBox5.Text);
            Variable.Sensitivity = this.trackBar1.Value;
            Variable.WebCamID = this.comboBox1.SelectedIndex;
            Variable.DisplayCam = this.checkBox1.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox6.Text = trackBar1.Value.ToString();
        }
    }
}
