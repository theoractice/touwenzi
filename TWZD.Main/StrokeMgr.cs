using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TWZD.Main
{
    internal class BezierPoints
    {
        public PointF[] pts = new PointF[4];
    }

    internal class StrokeMgr
    {
        internal bool Done = false;

        internal float speed = 0;
        internal float dspeed = 0;

        internal bool bContinue = true;

        string _strokeStr = "";
        int strokeNum = 0;
        int curStroke = 0;
        int curStrokePart = 0;
        float curStrokeProgress = 0;
        Graphics graphics;
        Bitmap bm;
        List<BezierPoints[]> strokeBeziers = new List<BezierPoints[]>();
        // 繁体字的笔画要细一点，原因你懂的
        Pen frontPen = new Pen(Color.White, 16);
        Pen backPen = new Pen(Color.Black, 24);
        PictureBox _pbox;
        Matrix defMat;

        internal StrokeMgr(string strokeString, PictureBox pbox)
        {
            _strokeStr = strokeString;
            _pbox = pbox;
            _pbox.Image = null;
            bm = new Bitmap(pbox.Width, pbox.Height);
            graphics = Graphics.FromImage(bm);
            graphics.Clear(pbox.BackColor);
            graphics.Transform = defMat = new Matrix(1, 0, 0, 1, (pbox.Width - 400) / 2, (pbox.Height - 400) / 2);
            frontPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            frontPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            backPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            backPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            string[] charDesc = _strokeStr.Split(new char[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            strokeNum = charDesc.Length;

            BezierPoints[] beziers;
            foreach (string strokeStr in charDesc)
            {
                string[] strokeDesc = strokeStr.Split(new char[] { ',' });
                string stroke = strokeDesc[0];
                string[] keyPointList = new string[strokeDesc.Length - 1];
                Array.Copy(strokeDesc, 1, keyPointList, 0, keyPointList.Length);

                Type t = typeof(StrokeMgr);

                MethodInfo info = t.GetMethod("draw" + stroke,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                beziers = (BezierPoints[])info.Invoke(this, new object[] { keyPointList });
                strokeBeziers.Add(beziers);
            }

            curStroke = 0;
            curStrokePart = 0;
            curStrokeProgress = 0;
            Done = false;
        }

        internal void OnMove(float x, float y, float interval)
        {
            if (Done)
            {
                return;
            }

            //计算当前笔画点的斜率
            BezierPoints d1, d2;
            if (1 == curStrokeProgress)
            {
                d1 = LerpBezierCurve(strokeBeziers[curStroke][curStrokePart], 0.99f);
                d2 = LerpBezierCurve(strokeBeziers[curStroke][curStrokePart], 1.00f);
            }
            else
            {
                d1 = LerpBezierCurve(strokeBeziers[curStroke][curStrokePart], curStrokeProgress);
                d2 = LerpBezierCurve(strokeBeziers[curStroke][curStrokePart], curStrokeProgress + 0.01f);
            }


            if (curStrokeProgress <= 0.7)
            {
                if (Math.Abs(AngleBetween(
                    new PointF(x, y),
                    new PointF(d2.pts[3].X - d1.pts[3].X, d2.pts[3].Y - d1.pts[3].Y)))
                    > (0.6 + 0.4 * curStrokeProgress))
                {
                    bContinue = true;
                    return;
                }

                if (0 == (curStroke & curStrokePart & (int)curStrokeProgress))
                {
                    if (!bContinue)
                        return;
                }
            }

            bContinue = true;
            speed += interval * (0.05f / 0.111f);
            if (speed > 0.1f)
                speed = 0.1f;
        }

        internal void OnDraw()
        {
            speed = speed * 0.8f;
            Variable.MainForm.Invoke(new MethodInvoker(Draw));
        }

        private double AngleBetween(PointF vector1, PointF vector2)
        {
            double x = vector1.X * vector2.X + vector1.Y * vector2.Y;
            x = x / Math.Sqrt(Math.Pow(vector1.X, 2) + Math.Pow(vector1.Y, 2))
                / Math.Sqrt(Math.Pow(vector2.X, 2) + Math.Pow(vector2.Y, 2));
            return Math.Acos(x);
        }
        /// <summary>
        /// 逐步绘制贝塞尔曲线
        /// </summary>
        internal void Draw()
        {
            BezierPoints origPts = strokeBeziers[curStroke][curStrokePart];

            float lenFactor = 400f / (float)Math.Sqrt(Math.Pow((origPts.pts[0].X - origPts.pts[3].X), 2)
                + Math.Pow((origPts.pts[0].Y - origPts.pts[3].Y), 2));
            if (lenFactor > 3) lenFactor = 3;

            curStrokeProgress += speed * lenFactor * (0.1f + 0.9f * Variable.Sensitivity / 100.0f);
            if (curStrokeProgress >= 1)
            {
                curStrokeProgress = 1;
            }
            BezierPoints bPtsToDraw = LerpBezierCurve(origPts, curStrokeProgress);
            graphics.DrawBezier(frontPen
                , bPtsToDraw.pts[0]
                , bPtsToDraw.pts[1]
                , bPtsToDraw.pts[2]
                , bPtsToDraw.pts[3]);

            if (curStrokeProgress >= 1)
            {
                curStrokePart++;
                curStrokeProgress = 0;
                speed = 0.02f;
            }
            if (curStrokePart >= strokeBeziers[curStroke].Length)
            {
                curStroke++;
                curStrokePart = 0;
                curStrokeProgress = 0;
                speed = 0;
                bContinue = false;


                if (curStroke < strokeNum)
                {
                    for (int i = 0; i < strokeBeziers[curStroke].Length; i++)
                    {
                        graphics.DrawBezier(backPen
                            , strokeBeziers[curStroke][i].pts[0]
                            , strokeBeziers[curStroke][i].pts[1]
                            , strokeBeziers[curStroke][i].pts[2]
                            , strokeBeziers[curStroke][i].pts[3]);
                    }


                    for (int j = 0; j < curStroke; j++)
                    {
                        for (int i = 0; i < strokeBeziers[j].Length; i++)
                        {
                            graphics.DrawBezier(backPen
                                , strokeBeziers[j][i].pts[0]
                                , strokeBeziers[j][i].pts[1]
                                , strokeBeziers[j][i].pts[2]
                                , strokeBeziers[j][i].pts[3]);
                        }
                    }

                    for (int j = 0; j < curStroke; j++)
                    {
                        for (int i = 0; i < strokeBeziers[j].Length; i++)
                        {
                            graphics.DrawBezier(frontPen
                                , strokeBeziers[j][i].pts[0]
                                , strokeBeziers[j][i].pts[1]
                                , strokeBeziers[j][i].pts[2]
                                , strokeBeziers[j][i].pts[3]);
                        }
                    }
                }
            }
            if (curStroke >= strokeNum)
            {
                Done = true;
            }

            _pbox.Image = bm;
        }
        /// <summary>
        /// 按照匀速运动模式计算贝塞尔插值点
        /// </summary>
        /// <param name="bezier">控制点</param>
        /// <param name="t1">比例</param>
        /// <returns></returns>
        private BezierPoints LerpBezierCurve(BezierPoints bezier, float t1)
        {
            BezierPoints ret = new BezierPoints();

            float x1 = bezier.pts[0].X;
            float bx1 = bezier.pts[1].X;
            float bx2 = bezier.pts[2].X;
            float x2 = bezier.pts[3].X;

            float y1 = bezier.pts[0].Y;
            float by1 = bezier.pts[1].Y;
            float by2 = bezier.pts[2].Y;
            float y2 = bezier.pts[3].Y;

            float t0 = 0;

            float u0 = 1.0f - t0;
            float u1 = 1.0f - t1;

            float qxa = x1 * u0 * u0 + bx1 * 2 * t0 * u0 + bx2 * t0 * t0;
            float qxb = x1 * u1 * u1 + bx1 * 2 * t1 * u1 + bx2 * t1 * t1;
            float qxc = bx1 * u0 * u0 + bx2 * 2 * t0 * u0 + x2 * t0 * t0;
            float qxd = bx1 * u1 * u1 + bx2 * 2 * t1 * u1 + x2 * t1 * t1;

            float qya = y1 * u0 * u0 + by1 * 2 * t0 * u0 + by2 * t0 * t0;
            float qyb = y1 * u1 * u1 + by1 * 2 * t1 * u1 + by2 * t1 * t1;
            float qyc = by1 * u0 * u0 + by2 * 2 * t0 * u0 + y2 * t0 * t0;
            float qyd = by1 * u1 * u1 + by2 * 2 * t1 * u1 + y2 * t1 * t1;

            float xa = qxa * u0 + qxc * t0;
            float xb = qxa * u1 + qxc * t1;
            float xc = qxb * u0 + qxd * t0;
            float xd = qxb * u1 + qxd * t1;

            float ya = qya * u0 + qyc * t0;
            float yb = qya * u1 + qyc * t1;
            float yc = qyb * u0 + qyd * t0;
            float yd = qyb * u1 + qyd * t1;

            ret.pts[0].X = xa;
            ret.pts[1].X = xb;
            ret.pts[2].X = xc;
            ret.pts[3].X = xd;

            ret.pts[0].Y = ya;
            ret.pts[1].Y = yb;
            ret.pts[2].Y = yc;
            ret.pts[3].Y = yd;

            return ret;
        }

        #region 计算所有笔画的控制点
        private BezierPoints[] PreBeziers(int num)
        {
            BezierPoints[] beziers = new BezierPoints[num];
            for (int i = 0; i < num; i++)
            {
                beziers[i] = new BezierPoints();
            }
            return beziers;
        }

        private void LinearLerp(PointF start, PointF end, ref PointF p1, ref PointF p2)
        {
            p1.X = start.X * 0.67f + end.X * 0.33f;
            p1.Y = start.Y * 0.67f + end.Y * 0.33f;
            p2.X = start.X * 0.33f + end.X * 0.67f;
            p2.Y = start.Y * 0.33f + end.Y * 0.67f;
        }

        private BezierPoints bezierLine(string[] keyPoints, int idxStart, int idxEnd)
        {
            BezierPoints line = new BezierPoints();

            line.pts[0] = SplitKeyPoints(keyPoints, idxStart);
            line.pts[3] = SplitKeyPoints(keyPoints, idxEnd);

            if (Math.Abs(line.pts[3].X - line.pts[0].X) < 8)
                line.pts[3].X = line.pts[0].X;
            if (Math.Abs(line.pts[3].Y - line.pts[0].Y) < 8)
                line.pts[3].Y = line.pts[0].Y;

            LinearLerp(line.pts[0], line.pts[3], ref  line.pts[1], ref line.pts[2]);
            //line.pts[1].X = line.pts[0].X * 0.67f + line.pts[3].X * 0.33f;
            //line.pts[1].Y = line.pts[0].Y * 0.67f + line.pts[3].Y * 0.33f;
            //line.pts[2].X = line.pts[0].X * 0.33f + line.pts[3].X * 0.67f;
            //line.pts[2].Y = line.pts[0].Y * 0.33f + line.pts[3].Y * 0.67f;

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
            BezierPoints[] beziers = PreBeziers(1);

            beziers[0] = bezierLine(keyPointList, 0, 1);

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

            return beziers;
        }

        private BezierPoints[] drawWP(string[] keyPointList)
        {
            return drawP(keyPointList);
        }

        private BezierPoints[] drawSP(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(1);

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[3] = SplitKeyPoints(keyPointList, 3);
            beziers[0].pts[1] = SplitKeyPoints(keyPointList, 1);

            beziers[0].pts[2].X = beziers[0].pts[0].X;
            beziers[0].pts[2].Y = beziers[0].pts[0].Y * 0.2f + beziers[0].pts[3].Y * 0.8f;
            return beziers;

            //BezierPoints[] beziers = PreBeziers(2);

            //beziers[0] = bezierLine(keyPointList, 0, 1);

            //beziers[1].pts[0] = SplitKeyPoints(keyPointList, 1);
            //beziers[1].pts[3] = SplitKeyPoints(keyPointList, 3);

            //beziers[1].pts[1] = new PointF(
            //    beziers[1].pts[0].X,
            //    beziers[1].pts[0].Y * 0.4f + beziers[1].pts[3].Y * 0.6f);
            ////beziers[1].pts[2] = beziers[1].pts[1];
            //beziers[1].pts[2] = new PointF(
            //    beziers[1].pts[0].X * 0.7f + beziers[1].pts[3].X * 0.3f,
            //    beziers[1].pts[0].Y * 0.3f + beziers[1].pts[3].Y * 0.7f);

            //return beziers;
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
            BezierPoints[] beziers = PreBeziers(2);

            beziers[0] = bezierLine(keyPointList, 0, 1);
            beziers[1] = bezierLine(keyPointList, 1, 2);

            return beziers;
        }

        private BezierPoints[] drawHG(string[] keyPointList)
        {
            return drawHZ(keyPointList);
        }

        private BezierPoints[] drawSG(string[] keyPointList)
        {
            BezierPoints[] beziers = PreBeziers(2);

            beziers[0].pts[0] = SplitKeyPoints(keyPointList, 0);
            beziers[0].pts[3] = SplitKeyPoints(keyPointList, 2);
            beziers[0].pts[3].X = beziers[0].pts[0].X;
            LinearLerp(beziers[0].pts[0], beziers[0].pts[3],
                ref beziers[0].pts[1], ref beziers[0].pts[2]);


            beziers[1].pts[0] = beziers[0].pts[3];
            beziers[1].pts[3] = SplitKeyPoints(keyPointList, 3);
            LinearLerp(beziers[1].pts[0], beziers[1].pts[3],
                ref beziers[1].pts[1], ref beziers[1].pts[2]);

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
            BezierPoints[] beziers = PreBeziers(2);

            beziers[0] = bezierPie(keyPointList, 0, 2);
            beziers[1] = bezierPie(keyPointList, 2, 4);

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

            beziers[2].pts[0] = beziers[1].pts[1];
            beziers[2].pts[3] = SplitKeyPoints(keyPointList, 4);
            beziers[2].pts[1] = beziers[2].pts[3];
            beziers[2].pts[2] = beziers[2].pts[3];

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
            beziers[0] = bezierPie(keyPointList, 0, 2);
            beziers[1] = bezierLine(keyPointList, 2, 3);
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

            beziers[3].pts[0] = beziers[2].pts[1];
            beziers[3].pts[3] = SplitKeyPoints(keyPointList, 5);
            beziers[3].pts[1] = beziers[3].pts[3];
            beziers[3].pts[2] = beziers[3].pts[3];

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

            beziers[4].pts[0] = beziers[3].pts[1];
            beziers[4].pts[3] = SplitKeyPoints(keyPointList, 6);
            beziers[4].pts[1] = beziers[4].pts[3];
            beziers[4].pts[2] = beziers[4].pts[3];

            return beziers;
        }
        #endregion
    }
}
