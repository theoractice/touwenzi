using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using TWZD.Data;
using System.Threading;

namespace TWZD.Test
{
    public partial class CharForm : Form
    {
        public class BezierPoints
        {
            public PointF[] pts = new PointF[5];
            public float part = 0;
        }

        Graphics graphics;
        //创建路径区域
        GraphicsPath path = new GraphicsPath();
        Pen pen = new Pen(Color.Black);
        Brush brush = new SolidBrush(Color.Red);

        SQLiteMgr sqlMgr = new SQLiteMgr(".", "twz", typeof(TWZDData));

        //string strokeStr = "H,16;188,389;188";
        //string strokeStr = "S,200;20,200;362";
        //string strokeStr = "T,35;301,292;201";
        //string strokeStr = "D,117;95,172;138,199;178";
        //string strokeStr = "P,222;50,117;196,18;271";
        //string strokeStr = "DT,1;249,49;367,123;67";
        //string strokeStr = "WP,191;9,146;246,2;389";
        //string strokeStr = "SP,274;61,274;173,240;236,178;293";
        //string strokeStr = "N,91;143,182;301,378;380";
        //string strokeStr = "DN,";//NULL
        //string strokeStr = "PN,111;298,202;357,387;357";
        //string strokeStr = "HZ,46;228,180;230,180;390";
        //string strokeStr = "HG,51;37,326;37,204;109";
        //string strokeStr = "SG,206;97,206;307,167;335,127;313";
        //string strokeStr = "ST,141;290,137;379,238;348";
        //string strokeStr = "SZ,85;44,77;197,381;196";
        //string strokeStr = "PG,";//NULL
        //string strokeStr = "PD,106;5,90;79,55;146,128;157,182;191";
        //string strokeStr = "HZG,31;139,356;139,356;349,323;385,252;364";
        //string strokeStr = "HZZ,44;66,153;66,152;210,255;210";
        //string strokeStr = "HZT,111;171,155;171,154;355,219;271";
        //string strokeStr = "HP,45;120,321;120,193;244,20;354";
        //string strokeStr = "HXG,180;122,315;122,324;197,377;242,380;163";
        //string strokeStr = "SZZ,63;121,63;225,164;225,164;393";
        //string strokeStr = "SW,298;52,294;257,314;265,365;266";
        //string strokeStr = "SWG,272;12,269;358,278;375,372;372,372;285";
        //string strokeStr = "TPN,115;374,173;320,245;364,390;364";
        //string strokeStr = "XG,240;13,254;247,378;384,378;293";
        //string strokeStr = "WG,23;20,142;169,142;325,104;382,34;363";
        //string strokeStr = "SZP,271;13,188;247,348;247,267;338";
        //string strokeStr = "TN,21;352,85;304,170;359,385;359";
        //string strokeStr = "SZZG,264;55,229;177,358;177,343;325,287;373,240;356";
        //string strokeStr = "PT,173;88,130;143,80;208,159;206,221;205";
        //string strokeStr = "HZWG,54;63,319;63,72;298,88;358,349;358,349;251";
        //string strokeStr = "HZW,137;37,266;37,266;136,291;170,381;170";
        //string strokeStr = "HZZZ,141;36,257;36,257;180,346;180,346;389";
        //string strokeStr = "HPWG,120;134,183;134,148;203,181;263,170;322,131;314";
        //string strokeStr = "HZZP,18;45,150;45,65;175,158;175,109;308,17;394";
        //string strokeStr = "HZZZG,56;46,295;46,248;164,352;164,330;322,279;374,224;364";
        //string strokeStr = "O,";//NULL

        public CharForm()
        {
            InitializeComponent();
            graphics = this.CreateGraphics();

            //设定文本输出质量
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.Width = 10;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //25癞竖撇出错
            //81舴压缩了
            //157觞竖撇
            //336赭竖钩
            //469墩竖钩
            //994挝竖钩
            for (int id = 1; id <= 1149; id++)
            {
                Thread.Sleep(1000);
                graphics.Clear(Color.White);
                Application.DoEvents();

                DataTable dtPhrase = sqlMgr.SelectFromTable("Phrase", "rowid", id.ToString());
                string phrase = (string)dtPhrase.Rows[0]["词语"];
                int pos = int.Parse((string)dtPhrase.Rows[0]["单字位置"]) - 1;
                string currentChar = new string(phrase[pos], 1);
                this.Text = id.ToString()+currentChar;

                //currentChar = "鞭";

                DataTable dtStroke = sqlMgr.SelectFromTable("StrokeOrder", "汉字", currentChar);
                if (dtStroke.Rows.Count == 0)
                {
                    MessageBox.Show(id.ToString());
                    continue;
                }
                string[] charDesc = ((string)dtStroke.Rows[0]["笔顺数据"]).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string strokeStr in charDesc)
                {
                    BezierPoints[] beziers;
                    string[] strokeDesc = strokeStr.Split(new char[] { ',' });
                    string stroke = strokeDesc[0];
                    string[] keyPointList = new string[strokeDesc.Length - 1];
                    Array.Copy(strokeDesc, 1, keyPointList, 0, keyPointList.Length);

                    Type t = typeof(CharForm);

                    MethodInfo info = t.GetMethod("draw" + stroke, BindingFlags.NonPublic | BindingFlags.Instance);
                    beziers = (BezierPoints[])info.Invoke(this, new object[] { keyPointList });

                    for (int i = 0; i < beziers.Length; i++)
                    {
                        graphics.DrawBezier(pen
                            , beziers[i].pts[0]
                            , beziers[i].pts[1]
                            , beziers[i].pts[2]
                            , beziers[i].pts[3]);
                    }
                }
            }
        }

        private BezierPoints[] PreBeziers(int num)
        {
            BezierPoints[] beziers = new BezierPoints[num];
            for (int i = 0; i < num; i++)
            {
                beziers[i] = new BezierPoints();
            }
            return beziers;
        }

        private BezierPoints bezierLine(string[] keyPoints, int idxStart, int idxEnd)
        {
            BezierPoints line = new BezierPoints();

            line.pts[0] = SplitKeyPoints(keyPoints, idxStart);
            line.pts[1] = SplitKeyPoints(keyPoints, idxEnd);
            line.pts[2] = line.pts[1];
            line.pts[3] = line.pts[1];
            line.pts[4] = new PointF(1f, 1f);

            return line;
        }

        private BezierPoints bezierPie(string[] keyPoints, int idxStart, int idxEnd)
        {
            BezierPoints pie = new BezierPoints();

            pie.pts[0] = SplitKeyPoints(keyPoints, idxStart);
            pie.pts[3] = SplitKeyPoints(keyPoints, idxEnd);

            PointF base0 = SplitKeyPoints(keyPoints, idxStart + 1);

            pie.pts[1] = new PointF(
                pie.pts[0].X * 0.2f + base0.X * 0.8f,
                base0.Y);

            pie.pts[2] = new PointF(
                base0.X,
                base0.Y * 0.7f + pie.pts[3].Y * 0.3f);

            pie.pts[4] = new PointF(1f, 1f);
            return pie;
        }

        private PointF SplitKeyPoints(string[] keyPointList, int p)
        {
            string[] tmp = keyPointList[p].Split(new char[] { ';' });
            float[] fXs = new float[]
            {
                float.Parse(tmp[0])
                ,float.Parse(tmp[1])
            };
            return new PointF(fXs[0], fXs[1]);
        }

        private BezierPoints[] HSTPoints(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[]
            {
                new BezierPoints()
            };

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);

            beziers[0].pts[1] = SplitKeyPoints(keyPointList, 1);
            beziers[0].pts[2] = beziers[0].pts[1];
            beziers[0].pts[3] = beziers[0].pts[1];
            beziers[0].pts[4] = new PointF(1f, 1f);
            return beziers;
        }

        private BezierPoints[] drawH(string[] keyPointList)
        {
            return HSTPoints(keyPointList);
        }

        private BezierPoints[] drawS(string[] keyPointList)
        {
            return HSTPoints(keyPointList);
        }

        private BezierPoints[] drawT(string[] keyPointList)
        {
            return HSTPoints(keyPointList);
        }

        private BezierPoints[] drawD(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[]
            {
                new BezierPoints()
            };

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[3] = SplitKeyPoints(keyPointList, 2);

            beziers[0].pts[1] = new PointF(
                beziers[0].pts[0].X * 0.3f + beziers[0].pts[3].X * 0.7f,
                beziers[0].pts[0].Y * 0.5f + beziers[0].pts[3].Y * 0.5f);

            beziers[0].pts[2] = beziers[0].pts[1];

            beziers[0].pts[4] = new PointF(1f, 1f);
            return beziers;
        }

        private BezierPoints[] drawP(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[1];
            beziers[0] = bezierPie(keyPointList, 0, 2);
            return beziers;
        }

        private BezierPoints[] drawDT(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[]
            {
                new BezierPoints()
            };

            PointF base0 = SplitKeyPoints(keyPointList, 0);
            PointF base1 = SplitKeyPoints(keyPointList, 1);
            PointF base2 = SplitKeyPoints(keyPointList, 2);

            beziers[0].pts[0] = new PointF(
                base0.X * 0.6f + base1.X * 0.4f,
                base0.Y * 0.0f + base1.Y * 1.0f);

            beziers[0].pts[1] = new PointF(
                base1.X * 0.5f + base2.X * 0.5f,
                base1.Y * 0.4f + base2.Y * 0.6f);

            beziers[0].pts[2] = beziers[0].pts[1];
            beziers[0].pts[3] = beziers[0].pts[1];

            beziers[0].pts[4] = new PointF(1f, 1f);
            return beziers;
        }

        private BezierPoints[] drawWP(string[] keyPointList)
        {
            return drawP(keyPointList);
        }

        private BezierPoints[] drawSP(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[]
            {
                new BezierPoints()
                ,new BezierPoints()
            };

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[3] = SplitKeyPoints(keyPointList, 1);
            beziers[0].pts[1] = beziers[0].pts[3];
            beziers[0].pts[2] = beziers[0].pts[3];
            beziers[0].pts[4] = new PointF(1f, 1f);

            beziers[1].pts[0] = SplitKeyPoints(keyPointList, 1);
            beziers[1].pts[3] = SplitKeyPoints(keyPointList, 3);

            beziers[1].pts[1] = new PointF(
                beziers[1].pts[0].X,
                beziers[1].pts[0].Y * 0.4f + beziers[1].pts[3].Y * 0.6f);
            //beziers[1].pts[2] = beziers[1].pts[1];
            beziers[1].pts[2] = new PointF(
                beziers[1].pts[0].X * 0.7f + beziers[1].pts[3].X * 0.3f,
                beziers[1].pts[0].Y * 0.3f + beziers[1].pts[3].Y * 0.7f);

            beziers[1].pts[4] = new PointF(1f, 1f);
            return beziers;
        }

        private BezierPoints[] drawN(string[] keyPointList)
        {
            return drawP(keyPointList);
        }

        private BezierPoints[] drawPN(string[] keyPointList)
        {
            return drawP(keyPointList);
        }

        private BezierPoints[] drawHZ(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[]
            {
                new BezierPoints()
                ,new BezierPoints()
            };

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[1] = SplitKeyPoints(keyPointList, 1);
            beziers[0].pts[2] = beziers[0].pts[1];
            beziers[0].pts[3] = beziers[0].pts[1];
            beziers[0].pts[4] = new PointF(1f, 1f);

            beziers[1].pts[0] = SplitKeyPoints(keyPointList, 1);
            beziers[1].pts[1] = SplitKeyPoints(keyPointList, 2);
            beziers[1].pts[2] = beziers[1].pts[1];
            beziers[1].pts[3] = beziers[1].pts[1];
            beziers[1].pts[4] = new PointF(1f, 1f);
            return beziers;
        }

        private BezierPoints[] drawHG(string[] keyPointList)
        {
            return drawHZ(keyPointList);
        }

        private BezierPoints[] drawSG(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[]
            {
                new BezierPoints()
                ,new BezierPoints()
            };

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[1] = SplitKeyPoints(keyPointList, 2);
            beziers[0].pts[1].X = beziers[0].pts[0].X;
            beziers[0].pts[2] = beziers[0].pts[1];
            beziers[0].pts[3] = beziers[0].pts[1];
            beziers[0].pts[4] = new PointF(1f, 1f);

            beziers[1].pts[0] = beziers[0].pts[1];
            beziers[1].pts[3] = SplitKeyPoints(keyPointList, 3);
            beziers[1].pts[1] = beziers[1].pts[3];
            beziers[1].pts[2] = beziers[1].pts[3];
            beziers[1].pts[4] = new PointF(1f, 1f);
            return beziers;
        }

        private BezierPoints[] drawST(string[] keyPointList)
        {
            return drawHZ(keyPointList);
        }

        private BezierPoints[] drawSZ(string[] keyPointList)
        {
            return drawHZ(keyPointList);
        }

        private BezierPoints[] drawPD(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[]
            {
                new BezierPoints()
                ,new BezierPoints()
            };

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[3] = SplitKeyPoints(keyPointList, 2);

            PointF base0 = SplitKeyPoints(keyPointList, 1);

            beziers[0].pts[1] = new PointF(
                beziers[0].pts[0].X * 0.5f + base0.X * 0.5f,
                base0.Y);

            beziers[0].pts[2] = new PointF(
                base0.X,
                base0.Y * 0.5f + beziers[0].pts[3].Y * 0.5f);

            beziers[0].pts[4] = new PointF(1f, 1f);

            // diergepie
            beziers[1].pts[0] = SplitKeyPoints(keyPointList, 2);
            beziers[1].pts[3] = SplitKeyPoints(keyPointList, 4);

            PointF base01 = SplitKeyPoints(keyPointList, 3);

            beziers[1].pts[1] = new PointF(
                beziers[0].pts[0].X * 0.5f + base01.X * 0.5f,
                base01.Y);

            beziers[1].pts[2] = new PointF(
                base01.X,
                base01.Y * 0.5f + beziers[0].pts[3].Y * 0.5f);

            beziers[1].pts[4] = new PointF(1f, 1f);
            return beziers;
        }

        private BezierPoints[] drawHZG(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(3);

            beziers[0] = bezierLine(keyPointList, 0, 1);

            beziers[1].pts[0] = SplitKeyPoints(keyPointList, 1);
            beziers[1].pts[1] = SplitKeyPoints(keyPointList, 3);
            beziers[1].pts[1].X = beziers[1].pts[0].X;
            beziers[1].pts[2] = beziers[1].pts[1];
            beziers[1].pts[3] = beziers[1].pts[1];
            beziers[1].pts[4] = new PointF(1f, 1f);

            beziers[2].pts[0] = beziers[1].pts[1];
            beziers[2].pts[3] = SplitKeyPoints(keyPointList, 4);
            beziers[2].pts[1] = beziers[2].pts[3];
            beziers[2].pts[2] = beziers[2].pts[3];
            beziers[2].pts[4] = new PointF(1f, 1f);

            return beziers;
        }

        private BezierPoints[] drawHZZ(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[3];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierLine(keyPointList, 1, 2);
            beziers[2] = bezierLine(keyPointList, 2, 3);
            return beziers;
        }

        private BezierPoints[] drawHZT(string[] keyPointList)
        {
            return drawHZZ(keyPointList);
        }

        private BezierPoints[] drawHP(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[2];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierPie(keyPointList, 1, 3);
            return beziers;
        }

        private BezierPoints[] drawHXG(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[3];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierPie(keyPointList, 1, 3);
            beziers[2] = bezierLine(keyPointList, 3, 4);
            return beziers;
        }

        private BezierPoints[] drawSZZ(string[] keyPointList)
        {
            return drawHZZ(keyPointList);
        }

        private BezierPoints[] drawSW(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[2];
            beziers[0] = bezierPie(keyPointList, 0, 2);
            beziers[1] = bezierLine(keyPointList, 2, 3);
            return beziers;
        }

        private BezierPoints[] drawSWG(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[3];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierPie(keyPointList, 1, 3);
            beziers[2] = bezierLine(keyPointList, 3, 4);
            return beziers;
        }

        private BezierPoints[] drawTPN(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[2];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierPie(keyPointList, 1, 3);
            return beziers;
        }

        private BezierPoints[] drawXG(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[2];
            beziers[0] = bezierPie(keyPointList, 0, 2);
            beziers[1] = bezierLine(keyPointList, 2, 3);
            return beziers;
        }

        private BezierPoints[] drawWG(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(2);
            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[1] = SplitKeyPoints(keyPointList, 1);
            beziers[0].pts[1].X += (beziers[0].pts[1].X - beziers[0].pts[0].X) * 0.6f;
            beziers[0].pts[2] = SplitKeyPoints(keyPointList, 2);
            beziers[0].pts[2].X += (beziers[0].pts[2].X - beziers[0].pts[0].X) * 0.2f;
            beziers[0].pts[3] = SplitKeyPoints(keyPointList, 3);
            beziers[0].pts[4] = new PointF(1f, 1f);

            beziers[1] = bezierLine(keyPointList, 3, 4);
            return beziers;
        }

        private BezierPoints[] drawSZP(string[] keyPointList)
        {
            return drawHZZ(keyPointList);
        }

        private BezierPoints[] drawTN(string[] keyPointList)
        {
            return drawTPN(keyPointList);
        }

        private BezierPoints[] drawSZZG(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(4);

            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierLine(keyPointList, 1, 2);

            beziers[2].pts[0] = SplitKeyPoints(keyPointList, 2);
            beziers[2].pts[1] = SplitKeyPoints(keyPointList, 4);
            beziers[2].pts[1].X = beziers[2].pts[0].X;
            beziers[2].pts[2] = beziers[2].pts[1];
            beziers[2].pts[3] = beziers[2].pts[1];
            beziers[2].pts[4] = new PointF(1f, 1f);

            beziers[3].pts[0] = beziers[2].pts[1];
            beziers[3].pts[3] = SplitKeyPoints(keyPointList, 5);
            beziers[3].pts[1] = beziers[3].pts[3];
            beziers[3].pts[2] = beziers[3].pts[3];
            beziers[3].pts[4] = new PointF(1f, 1f);

            return beziers;
        }

        private BezierPoints[] drawPT(string[] keyPointList)
        {
            return drawPD(keyPointList);
        }

        private BezierPoints[] drawHZWG(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(4);
            beziers[0] = bezierLine(keyPointList, 0, 1);

            beziers[1].pts[0] = SplitKeyPoints(keyPointList, 1);
            beziers[1].pts[3] = SplitKeyPoints(keyPointList, 3);
            beziers[1].pts[1] = SplitKeyPoints(keyPointList, 2);
            beziers[1].pts[1].Y = beziers[1].pts[1].Y - (beziers[1].pts[1].Y - beziers[1].pts[0].Y) * 0.2f;
            beziers[1].pts[2].X = beziers[1].pts[1].X - (beziers[1].pts[0].X - beziers[1].pts[1].X) * 0.3f;
            beziers[1].pts[2].Y = beziers[1].pts[3].Y;
            beziers[1].pts[4] = new PointF(1f, 1f);

            beziers[2] = bezierLine(keyPointList, 3, 4);
            beziers[3] = bezierLine(keyPointList, 4, 5);

            return beziers;
        }

        private BezierPoints[] drawHZW(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[3];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierPie(keyPointList, 1, 3);
            beziers[2] = bezierLine(keyPointList, 3, 4);
            return beziers;
        }

        private BezierPoints[] drawHZZZ(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[4];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierLine(keyPointList, 1, 2);
            beziers[2] = bezierLine(keyPointList, 2, 3);
            beziers[3] = bezierLine(keyPointList, 3, 4);
            return beziers;
        }

        private BezierPoints[] drawHPWG(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(4);

            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierLine(keyPointList, 1, 2);

            beziers[2].pts[0] = SplitKeyPoints(keyPointList, 2);
            beziers[2].pts[1] = SplitKeyPoints(keyPointList, 3);
            beziers[2].pts[1].X += (beziers[2].pts[1].X - beziers[2].pts[0].X) * 0.2f;
            beziers[2].pts[2] = SplitKeyPoints(keyPointList, 3);
            beziers[2].pts[2].X += (beziers[2].pts[2].X - beziers[2].pts[0].X) * 0.2f;
            beziers[2].pts[3] = SplitKeyPoints(keyPointList, 4);
            beziers[2].pts[4] = new PointF(1f, 1f);

            beziers[3] = bezierLine(keyPointList, 4, 5);
            return beziers;
        }

        private BezierPoints[] drawHZZP(string[] keyPointList)
        {
            BezierPoints[] beziers = new BezierPoints[4];
            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierLine(keyPointList, 1, 2);
            beziers[2] = bezierLine(keyPointList, 2, 3);
            beziers[3] = bezierPie(keyPointList, 3, 5);
            return beziers;
        }

        private BezierPoints[] drawHZZZG(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(5);

            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierLine(keyPointList, 1, 2);
            beziers[2] = bezierLine(keyPointList, 2, 3);

            beziers[3].pts[0] = SplitKeyPoints(keyPointList, 3);
            beziers[3].pts[1] = SplitKeyPoints(keyPointList, 5);
            beziers[3].pts[1].X = beziers[3].pts[0].X;
            beziers[3].pts[2] = beziers[3].pts[1];
            beziers[3].pts[3] = beziers[3].pts[1];
            beziers[3].pts[4] = new PointF(1f, 1f);

            beziers[4].pts[0] = beziers[3].pts[1];
            beziers[4].pts[3] = SplitKeyPoints(keyPointList, 6);
            beziers[4].pts[1] = beziers[4].pts[3];
            beziers[4].pts[2] = beziers[4].pts[3];
            beziers[4].pts[4] = new PointF(1f, 1f);

            return beziers;
        }
    }
}
