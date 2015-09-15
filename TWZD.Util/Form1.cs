using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TWZD.Data;
using System.Threading;
using System.IO;
using Microsoft.VisualBasic;

namespace TWZD.Util
{
    public partial class Form1 : Form
    {
        SQLiteMgr _mgr;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            _mgr = new SQLiteMgr(".", "twz", typeof(TWZDData));

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                _mgr.ResetDB();
            });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                int val = 0;
                Dictionary<string, string> wordList = new Dictionary<string, string>();
                Dictionary<string, string> strokeList = new Dictionary<string, string>();

                using (System.IO.StreamReader file =
                        new System.IO.StreamReader(string.Format(@"dict-revised.json")))
                {
                    string allFile = file.ReadToEnd();
                    string[] allData = Regex.Split(allFile, "\t},\n\t{\n\t\t");

                    foreach (string tmp in allData)
                    {
                        var title = Regex.Match(tmp, "\"title\": \"([^\"]*)\"").Groups[1].Value;
                        var def = Regex.Match(tmp, "\"def\": \"([^\"]*)\"").Groups[1].Value;

                        wordList.Add(title, def);
                    }
                }

                using (System.IO.StreamReader file =
                        new System.IO.StreamReader(string.Format(@"stroke.txt")))
                {
                    string allFile = file.ReadToEnd();
                    string[] allData = Regex.Split(allFile, "]\r\n");

                    foreach (string tmp in allData)
                    {
                        var def = Regex.Split(tmp, "\\[");
                        if (def.Length<2)
                        {
                            continue;
                        }
                        if (!strokeList.ContainsKey(def[0]))
                        {
                            strokeList.Add(def[0], def[1]);
                        }
                    }
                }

                using (System.IO.StreamReader file =
                    new System.IO.StreamReader(string.Format(@"phrase.txt")))
                {
                    while (false == file.EndOfStream)
                    {
                        string[] phraseInfo = Regex.Split(file.ReadLine(), "\t");

                        Phrase item = new Phrase();
                        item.词语 = phraseInfo[0];
                        if (!wordList.ContainsKey(item.词语))
                        {
                            continue;
                        }
                        item.注音 = phraseInfo[1];
                        item.释义 = wordList[item.词语];
                        item.单字位置 = phraseInfo[2];

                        int pos = int.Parse(item.单字位置) - 1;
                        string currentChar = new string(item.词语[pos], 1);

                        if (!strokeList.ContainsKey(currentChar))
                        {
                            continue;
                        }
                        else
                        {
                            StrokeOrder so = new StrokeOrder();
                            so.汉字 = currentChar;
                            so.笔顺数据 = strokeList[currentChar];
                            so.备注 = "";

                            _mgr.AddToDB(so);
                            _mgr.AddToDB(item);
                        }

                        this.Text = val++.ToString();
                    }
                }
            });
        }

        private void button5_Click(object sender, EventArgs e)
        {
            UserConfig cfg = new UserConfig();
            cfg.工作时间 = "60";
            cfg.休息时间 = "5";
            cfg.显示时间 = "2";
            cfg.提示透明度 = "0.3";
            cfg.文字透明度 = "0.6";
            cfg.体感灵敏度 = "50";
            cfg.摄像机编号 = "0";
            cfg.小窗显示 = "false";

            _mgr.ResetTable(typeof(UserConfig));
            _mgr.AddToDB(cfg);
        }
    }
}
