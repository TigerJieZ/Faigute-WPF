using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Face;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;

namespace Faigute_WPF
{

    class Faigute
    {

        //单位帧数
        static int MaxFrameNume = 3 * 30;

        //左/右眼的闭合帧数
        private MyQueue m_nNumeEyesLeftClosed = new MyQueue(30 * 15);
        private MyQueue m_nNumeEyesRightClosed = new MyQueue(30 * 15);

        //嘴部帧数记录,用于较长时间段的嘴部情况分析
        static int m_nNumeMouth = 30*60*30;

        //嘴部张帧数
        private MyQueue m_nNumeMouthOpen = new MyQueue(30*30);

        //疲劳值
        private double NumeFaigute = 0.0;

        //面部信息的四种情况
        static int YES = 3;
        static int MAYBE = 2;
        static int NO = 1;
        static int UNKNOW = 0;

        //帧计时器，眼部疲劳队列每更新一遍向文本中储存一下
        int i = 0;

        //警示框

        private DrawingGroup drawingGroupNew = null;
        private DrawingImage FaiguteImage = null;

        //警示框矩形
        Rect FaiguteImageRect = new Rect(new Point(0, 0), new Point(100, 263));

        public Faigute()
        {
            this.drawingGroupNew = new DrawingGroup();
            this.FaiguteImage = new DrawingImage(this.drawingGroupNew);
        }

        //眼部闭合帧记录
        public void NoteRightEyeClosed(int index)
        {
            //右眼闭合记录
            switch (index)
            {
                case 3:
                    this.m_nNumeEyesRightClosed.Push(YES);
                    break;
                case 2:
                    this.m_nNumeEyesRightClosed.Push(MAYBE);
                    break;
                case 1:
                    this.m_nNumeEyesRightClosed.Push(NO);
                    break;
                case 0:
                    this.m_nNumeEyesRightClosed.Push(UNKNOW);
                    break;
            }
        }

        public void NoteLeftEyeClosed(int index)
        {
            //左眼闭合记录
            switch (index)
            {
                case 3:
                    this.m_nNumeEyesLeftClosed.Push(YES);
                    break;
                case 2:
                    this.m_nNumeEyesLeftClosed.Push(MAYBE);
                    break;
                case 1:
                    this.m_nNumeEyesLeftClosed.Push(NO);
                    break;
                case 0:
                    this.m_nNumeEyesLeftClosed.Push(UNKNOW);
                    break;
            }

        }

        //嘴部闭合记录
        public void NoteMouthOpen(int index)
        {
            switch (index)
            {
                case 3:
                    m_nNumeMouthOpen.Push(YES);
                    break;
                case 2:
                    m_nNumeMouthOpen.Push(MAYBE);
                    break;
                case 1:
                    m_nNumeMouthOpen.Push(NO);
                    break;
                case 0:
                    m_nNumeMouthOpen.Push(UNKNOW);
                    break;
            }
        }

        //嘴部疲劳指数统计
        public double calculateMouthFaigute()
        {
            double faigute = 0.0f;

            int myOpen = this.m_nNumeMouthOpen.CountYes();
            faigute = myOpen / this.m_nNumeMouthOpen.getLength();

            return faigute;
        }

        //眼部疲劳值统计
        public double calculatFaigute()
        {
            ///当前队列中右眼闭眼帧数
            int rClosed = m_nNumeEyesRightClosed.CountYes();

            ///当前队列中左眼闭眼帧数
            int lClosed = m_nNumeEyesLeftClosed.CountYes();

            ///当前队列中嘴巴张开帧数
            int mClosed = m_nNumeMouthOpen.CountYes();

            ///权衡嘴部/眼部疲劳
            double faigute = (rClosed + lClosed) / (this.m_nNumeEyesLeftClosed.getLength() * (double)(2));

            return faigute;
        }

        private double calculateFaigute(double rEye,double lEye,double mMouth)
        {
            double faigute = 0.0f ;

            return faigute;
        }

        /// <summary>
        /// 输出当前队列中右眼的闭合频率
        /// </summary>
        /// <returns></returns>
        public double getRightEyesClosedFrequency()
        {
            int rClosed = m_nNumeEyesRightClosed.CountYes();
            double frequency = rClosed / this.m_nNumeEyesRightClosed.getLength();

            return frequency;
        }

        /// <summary>
        /// 输出当前队列中左眼的闭合频率
        /// </summary>
        /// <returns></returns>
        public double getLeftEyesClosedFrequency()
        {
            int lClosed = m_nNumeEyesRightClosed.CountYes();
            double frequency = lClosed / this.m_nNumeEyesLeftClosed.getLength();

            return frequency;
        }

        //疲劳主调度器
        public double Scheduler(FaceFrameResult faceFrameResult)
        {
            //记录异常帧
            Note(faceFrameResult);

            //绘制提醒框
            //DrawingFaigute();
            this.NumeFaigute = calculatFaigute();
            StreamWriter sw = new StreamWriter("NumeFaigute.txt", true);
            System.DateTime currentTime = new System.DateTime();
            currentTime = System.DateTime.Now;
            sw.WriteLine(/*currentTime.ToString()+"     "+*/this.NumeFaigute);
            sw.Close();

            return this.NumeFaigute;
        }

        public void Note(FaceFrameResult faceFrameResult)
        {
            foreach (var item in faceFrameResult.FaceProperties)
            {
                String s = item.Key.ToString();
                if (s.Equals("RightEyeClosed"))
                {
                    switch (item.Value)
                    {
                        case DetectionResult.Yes:
                            NoteRightEyeClosed(YES);
                            break;
                        case DetectionResult.Maybe:
                            NoteRightEyeClosed(MAYBE);
                            break;
                        case DetectionResult.No:
                            NoteRightEyeClosed(NO);
                            break;
                        case DetectionResult.Unknown:
                            NoteRightEyeClosed(UNKNOW);
                            break;
                    }
                }
                if (s.Equals("LeftEyeClosed"))
                {
                    switch (item.Value)
                    {
                        case DetectionResult.Yes:
                            NoteLeftEyeClosed(YES);
                            break;
                        case DetectionResult.Maybe:
                            NoteLeftEyeClosed(MAYBE);
                            break;
                        case DetectionResult.No:
                            NoteLeftEyeClosed(NO);
                            break;
                        case DetectionResult.Unknown:
                            NoteLeftEyeClosed(UNKNOW);
                            break;
                    }
                }
                if (s.Equals("MouthOpen"))
                {
                    switch (item.Value)
                    {
                        case DetectionResult.Yes:
                            NoteMouthOpen(YES);
                            break;
                        case DetectionResult.Maybe:
                            NoteMouthOpen(MAYBE);
                            break;
                        case DetectionResult.No:
                            NoteMouthOpen(NO);
                            break;
                        case DetectionResult.Unknown:
                            NoteMouthOpen(UNKNOW);
                            break;
                    }
                }
            }
            i++;
            if (i % 900 == 0)
            {
                StreamWriter swL = new StreamWriter("m_nNumeEyesLeftClosed.txt",true);
                int q = 0;
                foreach (int data in m_nNumeEyesLeftClosed.getData())
                {
                    q++;
                    swL.Write(data+" ");
                    if (q % 10 == 0)
                        swL.WriteLine();
                }
                swL.Close();

                q = 0;
                StreamWriter swR = new StreamWriter("m_nNumeEyesRightClosed.txt", true);
                foreach (int data in m_nNumeEyesRightClosed.getData())
                {
                    q++;
                    swR.Write(data+" ");
                    if (q % 10 == 0)
                        swR.WriteLine();
                }
                swR.Close();

                q = 0;
                StreamWriter swM = new StreamWriter("m_nNumeMouthOpen.txt", true);
                foreach (int data in m_nNumeMouthOpen.getData())
                {
                    q++;
                    swM.Write(data + " ");
                    if (q % 10 == 0)
                        swM.WriteLine();
                }
                swM.Close();
            }
                

        }

        //绘制提示图
        //public void DrawingFaigute()
        //{
        //    using (DrawingContext Ddc = this.drawingGroupNew.Open())
        //    {
        //        Ddc.DrawRectangle(Brushes.Black, null, new Rect(new Point(0, 0), new Point(100, 263)));
        //        Ddc.Close();
        //    }
        //}


            
        //播放警示音
        public void soundPlayer()
        {
            MediaPlayer mySound = new MediaPlayer();
            if (NumeFaigute >0.7)
            {
                mySound.Open(new Uri("sound\\3.wav", UriKind.Relative));
            }   else if (NumeFaigute > 0.5)
            {
                mySound.Open(new Uri("sound\\2.wav", UriKind.Relative));
            }   else if (NumeFaigute > 0.3)
            {
                mySound.Open(new Uri("sound\\1.wav", UriKind.Relative));
            }
            mySound.Play();
        }

        public ImageSource FaiguteImageSource
        {
            get
            {
                return this.FaiguteImage;
            }
        }
    }
}
