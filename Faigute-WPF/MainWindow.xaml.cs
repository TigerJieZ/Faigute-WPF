using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Faigute_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Thickness of face bounding box and face points
        /// </summary>
        // 
        private const double DrawFaceShapeThickness = 8;

        //字体属性大小
        private const double DrawTextFontSize = 40;

        //面部点圆的半径
        private const double FacePointRadius = 1.0;

        //x轴的偏移
        private const float TextLayoutOffsetX = -0.1f;

        //y轴的偏移
        private const float TextLayoutOffsetY = -0.15f;

        /// <summary>
        /// 彩色帧：1，深度帧：2；红外帧：3
        /// </summary>
        private const int ColorButton= 1;
        private const int DepthButton = 2;
        private const int InfraredButton = 3;
        private const int BodyIndexButton = 4;
        private int frameType = 1;

        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;

        //面旋转显示角度增量
        private const double FaceRotationIncrementInDegrees = 5.0;

        //申明文本以表示在视野中没有跟踪的人
        private FormattedText textFaceNotTracked = new FormattedText(
                        "...",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Georgia"),
                        DrawTextFontSize,
                        Brushes.White);

        //没有人脸跟踪情况下的文本布局
        private Point textLayoutFaceNotTracked = new Point(10.0, 10.0);

        //用于绘制人体的图像输出组
        private DrawingGroup drawingGroup;
        private DrawingGroup drawingGroup2;
        private DrawingGroup drawingGroup3;

        //脸图像和彩色图像/深度图像
        private DrawingImage imageSource = null;
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap infraredBitmap = null;
        private DrawingImage FaiguteImage = null;
        private WriteableBitmap depthBitmap = null;
        private DrawingImage bitmap = null;

        /// <summary>
        /// 深度数据数组用于转换成视觉图像
        /// </summary>
        private byte[] depthPixels = null;

        //疲劳监测器
        Faigute myClock=new Faigute();

        /// <summary>
        /// 各种帧的描述
        /// </summary>
        FrameDescription infraredFrameDescription = null;
        FrameDescription depthFrameDescription = null;

        //kinect主体
        private KinectSensor kinectSensor = null;

        //坐标映射器
        private CoordinateMapper coordinateMapper = null;

        //复源帧读取器
        private MultiSourceFrameReader multiFrameSourceReader = null;

        //骨骼帧
        private BodyFrameReader bodyFrameReader = null;

        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        //人体索引数组
        private Body[] bodies = null;

        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;

        //被捕捉到的人体的数
        private int bodyCount;

        //面部资源数组
        private FaceFrameSource[] faceFrameSources = null;

        /// <summary>
        /// 深度帧读取器
        /// </summary>
        private DepthFrameReader depthFrameReader = null;
        //彩色帧读取器
        private ColorFrameReader colorFrameReader = null;
        //红外帧读取器
        private InfraredFrameReader infraredFrameReader = null;
        //面部帧读取器
        private FaceFrameReader[] faceFrameReaders = null;
        //人体索引读取器
        private BodyIndexFrameReader bodyIndexFrameReader = null;

        //彩色帧和深度帧结合映射的点
        private DepthSpacePoint[] colorMappedToDepthPoints = null;

        //面部基本信息仓库数组
        private FaceFrameResult[] faceFrameResults = null;

        //显示的宽度
        private int displayWidth;

        //显示的高度
        private int displayHeight;

        //显示的矩形
        private Rect displayRect;

        //画笔列表（被跟踪的人）
        private List<Brush> faceBrush;

        //流到显示状态文本
        private string statusText = null;

        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;
       
        /// <summary>
        /// 驾驶员纠正权值 
        /// </summary>
        private double Correct_Weight = 0.0;

        /// <summary>
        /// 纠正模块运行抵达时间
        /// </summary>
        private int CorrectOkNumes = 30 * 60;

        private bool CorrectStatus = false;

        /// <summary>
        /// 纠正模块运行计时器
        /// </summary>
        private int CorrectNumes = 0;

        /// <summary>
        /// 模型创建的线程
        /// </summary>
        private BackgroundWorker CorrectModule;


        /// <summary>
        /// 驾驶员锁定模块的线程
        /// </summary>
        private BackgroundWorker runDriverIndex;

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private const int BytesPerPixel = 4;

        //人体索引
        MyQueue[] DriverIndex=new MyQueue[6]; 

        /// <summary>
        /// 索引数据转换成彩色图像
        /// </summary>
        private uint[] bodyIndexPixels = null;

        /// <summary>
        /// 人体索引位图
        /// </summary>
        private WriteableBitmap bodyIndexBitmap = null;

        //人体索引图像描述
        private FrameDescription bodyIndexFrameDescription = null;

        /// <summary>
        /// 驾驶员索引
        /// </summary>
        private int myBodyIndex = -1;

        /// <summary>
        /// 驾驶员平均深度值
        /// </summary>
        private double myBodyDepth = 0.0f;

        /// <summary>
        /// 收集用于显示BodyIndexFrame数据的颜色
        /// </summary>
        private static readonly uint[] BodyColor =
        {
            0x0000FF00,
            0x00FF0000,
            0xFFFF4000,
            0x40FFFF00,
            0xFF40FF00,
            0xFF808000,
        };

        //疲劳值
        private double NumeFaigute;

        //疲劳监测类
        Faigute myFaigute;

        public MainWindow()
        {
            // 获取一个默认的传感器
            this.kinectSensor = KinectSensor.GetDefault();

            //获取坐标映射器
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            //获取帧描述
            FrameDescription frameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            //设置显示的图像属性
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;
            this.displayRect = new Rect(0.0, 0.0, this.displayWidth, this.displayHeight);

            //打开骨骼帧读取器
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            //打开深度帧读取器
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            //打开彩色帧读取器
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            //打开红外帧读取器
            this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            //打开人体索引帧读取器
            this.bodyIndexFrameReader = this.kinectSensor.BodyIndexFrameSource.OpenReader();

            //初始化彩色图像
            this.colorBitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            //骨骼帧处理机制
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            //初始化复源帧
            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex);

            //初始化彩色深度映射
            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;
            this.colorMappedToDepthPoints = new DepthSpacePoint[colorWidth * colorHeight];

            //初始化驾驶员模型搭建模块
            CorrectModule = new BackgroundWorker();

            //初始化驾驶员锁定模块
            runDriverIndex = new BackgroundWorker();

            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            //坐标映射器
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            //设置传感器可捕捉的人体的最大的数
            this.bodyCount = this.kinectSensor.BodyFrameSource.BodyCount;

            //分配存储骨骼对象
            this.bodies = new Body[this.bodyCount];

            //指定所需的面框结果
            FaceFrameFeatures faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;

            //创建一个面部帧资源和读取器来跟踪每个人的脸
            this.faceFrameSources = new FaceFrameSource[this.bodyCount];
            this.faceFrameReaders = new FaceFrameReader[this.bodyCount];
            for (int i = 0; i < this.bodyCount; i++)
            {
                //创建所需的人脸特征帧和初始ID为0的面部帧源
                this.faceFrameSources[i] = new FaceFrameSource(this.kinectSensor, 0, faceFrameFeatures);

                //打开面部帧读取器
                this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
            }

            //初始化面部基本信息
            this.faceFrameResults = new FaceFrameResult[this.bodyCount];

            // populate face result colors - one for each face index
            this.faceBrush = new List<Brush>()
            {
                Brushes.White,
                Brushes.Orange,
                Brushes.Green,
                Brushes.Red,
                Brushes.LightBlue,
                Brushes.Yellow
            };

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // set the status text
            this.statusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();
            this.drawingGroup2 = new DrawingGroup();
            this.drawingGroup3 = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);
            this.FaiguteImage = new DrawingImage(this.drawingGroup2);
            this.bitmap = new DrawingImage(this.drawingGroup3);

            //初始化人体索引帧描述
            this.bodyIndexFrameDescription = this.kinectSensor.BodyIndexFrameSource.FrameDescription;

            //初始化人体索引彩色图
            this.bodyIndexPixels = new uint[this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height];

            //初始化人体坐标数据集
            for(int i = 0; i < 6; i++)
            {
                this.DriverIndex[i] = new MyQueue(this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height);
            }
            ///this.DriverIndex = new[6] MyQueue();

            //初始化人体索引帧位图
            this.bodyIndexBitmap = new WriteableBitmap(this.bodyIndexFrameDescription.Width, this.bodyIndexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            //打开传感器
            this.kinectSensor.Open();

            //实例化疲劳监测器
            myFaigute = new Faigute();

            this.infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;
            this.infraredBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// 加载函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender,RoutedEventArgs e)
        {
            if (this.depthFrameReader != null)
            {
                this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;
            }

            //updateFrame();
            if (this.multiFrameSourceReader!=null)
            {
                
                //this.multiFrameSourceReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
            }

            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.FrameArrived += this.color;
            }
            
            if(this.infraredFrameReader!=null)
            {
                this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;
            }

            for (int i = 0; i < this.bodyCount; i++) { 
            ///{
            ///if (this.myBodyIndex >= 0)
            ///{
                if (this.faceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    this.faceFrameReaders[i].FrameArrived += this.Reader_FaceFrameArrived;
                }
            }
               
            ///}

            if (this.bodyFrameReader != null)
            {
                // wire handler for body frame arrival
                this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
            }

            if (this.bodyIndexFrameReader != null)
            {
                this.bodyIndexFrameReader.FrameArrived += this.Reader_BodyIndexFrameArrived;
            }

            ///监听驾驶员模型纠正模块的运行状态
            CorrectModule.DoWork += CorrectModule_Run;
            CorrectModule.RunWorkerCompleted += CorrectModule_RunWorkerCompleted;


            ///
            runDriverIndex.DoWork += RunDriverIndex_DoWork;
            runDriverIndex.RunWorkerCompleted += RunDriverIndex_RunWorkerCompleted;
        }

        private void RunDriverIndex_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //throw new NotImplementedException();
        }


        /// <summary>
        /// 驾驶员索引锁定线程运行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private unsafe void  RunDriverIndex_DoWork(object sender, DoWorkEventArgs e)
        {
            lock (this.bitmap)
            {
                String format = "ffffff";
                DateTime date = DateTime.Now;
                String start = date.ToString(format, DateTimeFormatInfo.InvariantInfo);
                int index = -1;
                double depth = 0.0f;
                double[] depthDrivers = new double[6];
                if (this.DriverIndex != null)
                {
                    for (int a = 0; a < 6; ++a)
                    {
                        if (this.DriverIndex[a] != null)
                        {
                            int length = this.DriverIndex[a].getData().Length;
                            ///随机取一百个像素点的平均值
                            for(int i=0;i<100;i++)
                            {
                                Random ran = new Random();
                                int j = ran.Next(0, 100);
                                depthDrivers[a] += ((double)(this.frameDatas[this.DriverIndex[a].getData()[j]])) / 100;
                            }
                            ///求所有像素点的平均值
                            /*foreach (long i in this.DriverIndex[a].getData())
                            {
                                depthDrivers[a] += ((double)(this.frameDatas[i]) / (512 * 424));
                            }*/
                        }
                    }

                    double[] functionDD = { 999999, 999999, 999999, 999999, 999999, 99999 };
                    int fddI = 0;
                    for (int a = 0; a < 6; a++)
                    {
                        if (depthDrivers[a] != 0)
                        {
                            functionDD[fddI] = depthDrivers[a];
                            fddI++;
                        }
                    }

                    depth = functionDD.Min();
                    StreamWriter minW = new StreamWriter("Min.txt", true);
                    minW.WriteLine(depth);
                    minW.Close();
                    for (int a = 0; a < 6; a++)
                    {
                        if (depth == depthDrivers[a])
                            index = a;
                    }
                }
                if (index >= 0)
                {
                    ///把计算得到的驾驶员人体索引录入文件中
                    StreamWriter sw = new StreamWriter("m_index.txt", true);
                    sw.WriteLine(index);
                    sw.Close();
                }
                DateTime date2 = DateTime.Now;
                String start2 = date2.ToString(format, DateTimeFormatInfo.InvariantInfo);
                this.myBodyIndex = index;
                this.myBodyDepth = depth;
            } 
            

            //throw new NotImplementedException();
        }

        /// <summary>
        /// 驾驶员模型纠正模块的完成函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CorrectModule_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StreamWriter sw = new StreamWriter("run.txt", true);
            sw.WriteLine("CorrectModule_RunWorkerCompleted");
            sw.Close();
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            StreamWriter sw = new StreamWriter("test.txt", true);
            sw.WriteLine("test");
            sw.Close();
            if (this.kinectSensor != null)
            {
                // on failure, set the status text
                this.statusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                : Properties.Resources.SensorNotAvailableStatusText;
            }
        }

        /// <summary>
        /// 图像显示框
        /// </summary>
        public ImageSource m_imageSource
        {
            get
            {
                return this.bitmap;
            }
        }

        /// <summary>
        /// 警示框
        /// </summary>
        public ImageSource FaiguteImageSource
        {
            get
            {
                return this.FaiguteImage;
            }
        }


        //深度信息读取事件
        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                    {
                        //确认深度帧格式并写入传感器采集的数据
                        if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / infraredFrameDescription.BytesPerPixel)) &&
                            (infraredFrameDescription.Width == this.infraredBitmap.PixelWidth) && (infraredFrameDescription.Height == this.infraredBitmap.PixelHeight))
                        {
                            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                        }
                    }
                }
            }
        }


        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            int depthWidth = 0;
            int depthHeight = 0;

            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            bool isBitmapLocked = false;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            // We use a try/finally to ensure that we clean up before we exit the function.  
            // This includes calling Dispose on any Frame objects that we may have and unlocking the bitmap back buffer.
            try
            {
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyIndexFrame == null))
                {
                    return;
                }

                

                // Process Depth
                FrameDescription depthFrameDescription = depthFrame.FrameDescription;

                depthWidth = depthFrameDescription.Width;
                depthHeight = depthFrameDescription.Height;

                // Access the depth frame data directly via LockImageBuffer to avoid making a copy
                // KinectBuffer depthData = depthFrame.LockImageBuffer();
                //uint size = 515*424;
                //depthFrame.CopyFrameDataToIntPtr(depthData.UnderlyingBuffer, depthData.Size);

                /*StreamWriter sw = File.AppendText("depthData.txt");
                  sw.Write(depthData.UnderlyingBuffer);
                  sw.Close();*/

                using (KinectBuffer depthFrameData = depthFrame.LockImageBuffer())
                {
                    this.coordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
                        depthFrameData.UnderlyingBuffer,
                        depthFrameData.Size,
                        this.colorMappedToDepthPoints);
                }

                // We're done with the DepthFrame 
                depthFrame.Dispose();
                depthFrame = null;

                // Process Color

                // Lock the bitmap for writing
                //this.bitmap.Lock();
                isBitmapLocked = true;

                //colorFrame.CopyConvertedFrameDataToIntPtr(this.bitmap.BackBuffer, this.bitmapBackBufferSize, ColorImageFormat.Bgra);

                // We're done with the ColorFrame 
                colorFrame.Dispose();
                colorFrame = null;

                // We'll access the body index data directly to avoid a copy
                using (KinectBuffer bodyIndexData = bodyIndexFrame.LockImageBuffer())
                {
                    unsafe
                    {
                        byte* bodyIndexDataPointer = (byte*)bodyIndexData.UnderlyingBuffer;

                        int colorMappedToDepthPointCount = this.colorMappedToDepthPoints.Length;

                        fixed (DepthSpacePoint* colorMappedToDepthPointsPointer = this.colorMappedToDepthPoints)
                        {
                            // Treat the color data as 4-byte pixels
                            //uint* bitmapPixelsPointer = (uint*)this.bitmap.BackBuffer;

                            // Loop over each row and column of the color image
                            // Zero out any pixels that don't correspond to a body index
                            for (int colorIndex = 0; colorIndex < colorMappedToDepthPointCount; ++colorIndex)
                            {
                                float colorMappedToDepthX = colorMappedToDepthPointsPointer[colorIndex].X;
                                float colorMappedToDepthY = colorMappedToDepthPointsPointer[colorIndex].Y;

                                // The sentinel value is -inf, -inf, meaning that no depth pixel corresponds to this color pixel.
                                if (!float.IsNegativeInfinity(colorMappedToDepthX) &&
                                    !float.IsNegativeInfinity(colorMappedToDepthY))
                                {
                                    // Make sure the depth pixel maps to a valid point in color space
                                    int depthX = (int)(colorMappedToDepthX + 0.5f);
                                    int depthY = (int)(colorMappedToDepthY + 0.5f);

                                    // If the point is not valid, there is no body index there.
                                    if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                                    {
                                        int depthIndex = (depthY * depthWidth) + depthX;

                                        // If we are tracking a body for the current pixel, do not zero out the pixel
                                        if (bodyIndexDataPointer[depthIndex] != 0xff)
                                        {
                                            continue;
                                        }
                                    }
                                }

                                //bitmapPixelsPointer[colorIndex] = 0;
                            }
                        }

                        //this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));
                    }
                }
            }
            finally
            {
                if (isBitmapLocked)
                {
                    //this.bitmap.Unlock();
                }

                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                }

                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.Dispose();
                }
            }
        }

        /// <summary>
        /// 处理从传感器到达的深度帧数据
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                //FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((depthFrameDescription.Width * depthFrameDescription.Height) == (depthBuffer.Size / depthFrameDescription.BytesPerPixel)) &&
                            (depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);

                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }



        private ushort[] frameDatas=new ushort[512*424];


        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            //追踪驾驶员索引
            //this.myBodyIndex=this.getDriverIndex(frameData);
            //this.runDriverIndex.CancelAsync();
            if (!this.runDriverIndex.IsBusy)
            {
                this.runDriverIndex.RunWorkerAsync();
            }
            

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];
                frameDatas[i] = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// 将红外信息转化成图像
        /// </summary>
        /// <param name="infraredFrameData">红外图像数据</param>
        /// <param name="infraredFrameDataSize">红外图像数据尺寸</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            ushort* frameData = (ushort*)infraredFrameData;

            //锁定位图
            this.infraredBitmap.Lock();

            //获取位图深度数据空间索引
            float* backBuffer = (float*)this.infraredBitmap.BackBuffer;

            //获取深度数据
            for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
            {
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            this.infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, this.infraredBitmap.PixelWidth, this.infraredBitmap.PixelHeight));

            //解锁位图
            this.infraredBitmap.Unlock();
        }


        /// <summary>
        /// 绘制面部基本信息
        /// </summary>
        /// <param name="faceIndex"></param>
        /// <param name="faceResult"></param>
        /// <param name="drawingContext"></param>
        private void DrawFaceFrameResults(int faceIndex, FaceFrameResult faceResult, DrawingContext drawingContext)
        {
            //this.myClock.soundPlayer();
            this.NumeFaigute=this.myClock.Scheduler(faceResult);
            // choose the brush based on the face index
            Brush drawingBrush = this.faceBrush[0];
            if (faceIndex < this.bodyCount)
            {
                drawingBrush = this.faceBrush[faceIndex];
            }

            Pen drawingPen = new Pen(drawingBrush, DrawFaceShapeThickness);

            var faceBoxSource = faceResult.FaceBoundingBoxInColorSpace;
            Rect faceBox = new Rect(faceBoxSource.Left, faceBoxSource.Top, faceBoxSource.Right - faceBoxSource.Left, faceBoxSource.Bottom - faceBoxSource.Top);
            drawingContext.DrawRectangle(null, drawingPen, faceBox);

            /*if (faceResult.FacePointsInColorSpace != null)
            {
                // 绘制面部特征点
                foreach (PointF pointF in faceResult.FacePointsInColorSpace.Values)
                {
                    drawingContext.DrawEllipse(null, drawingPen, new Point(pointF.X, pointF.Y), FacePointRadius, FacePointRadius);
                }
            }*/
            

            string faceText = string.Empty;

            // 提取每个面部数据存储在facetext中 
            if (faceResult.FaceProperties != null)
            {
                foreach (var item in faceResult.FaceProperties)
                {
                    if (item.Key.ToString() == "RightEyeClosed")
                    {
                        Point faceTextLayoutt;
                        if (this.GetFaceTextPositionInColorSpace(faceIndex, out faceTextLayoutt))
                        {
                            drawingContext.DrawText(
                            new FormattedText(
                                item.Key.ToString()+": "+item.Value.ToString(),
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Georgia"),
                                DrawTextFontSize,
                                drawingBrush),
                                new Point(faceBox.Left-20,faceBox.Bottom+20));
                        }
                    }
                    if (item.Key.ToString() == "LeftEyeClosed")
                    {
                        Point faceTextLayoutt;
                        if (this.GetFaceTextPositionInColorSpace(faceIndex, out faceTextLayoutt))
                        {
                            drawingContext.DrawText(
                            new FormattedText(
                                item.Key.ToString() + ": " + item.Value.ToString(),
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Georgia"),
                                DrawTextFontSize,
                                drawingBrush),
                                new Point(faceBox.Left-20, faceBox.Bottom + 80));
                        }
                    }
                    faceText += item.Key.ToString() + " :::: ";

                    if (item.Value == DetectionResult.Maybe)
                    {
                        faceText += DetectionResult.No + "\n";
                    }
                    else
                    {
                        faceText += item.Value.ToString() + "\n";
                    }
                }
            }

            // 以欧拉角的形式提取面旋转
            if (faceResult.FaceRotationQuaternion != null)
            {
                int pitch, yaw, roll;
                ExtractFaceRotationInDegrees(faceResult.FaceRotationQuaternion, out pitch, out yaw, out roll);
                faceText += "FaceYaw : " + yaw + "\n" +
                            "FacePitch : " + pitch + "\n" +
                            "FacenRoll : " + roll + "\n";
            }



            // 渲染脸部属性和脸部旋转信息
            /*Point faceTextLayout;
            if (this.GetFaceTextPositionInColorSpace(faceIndex, out faceTextLayout))
            {
                drawingContext.DrawText(
                        new FormattedText(
                            faceText,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Georgia"),
                            DrawTextFontSize,
                            drawingBrush),
                        faceTextLayout);
            }*/

            //绘制当前跟踪的人体ID
            drawingContext.DrawText(new FormattedText(
                            "当前跟踪的人体ID为"+this.myBodyIndex.ToString()+"\n深度值为"+this.myBodyDepth.ToString(),
                            CultureInfo.GetCultureInfo("zh-cn"),
                            FlowDirection.LeftToRight,
                            new Typeface("Georgia"),
                            100,
                            drawingBrush), new Point(0, 0));
        }

        /// <summary>
        /// 更新图像
        /// </summary>
        public void updateFrame()
        {
            switch (this.frameType)
            {
                case ColorButton:
                    ImageSource temp3 = this.imageSource;
                    using (DrawingContext dc = this.drawingGroup3.Open())
                    {
                        dc.DrawImage(temp3, this.displayRect);
                    }
                    break;
                case DepthButton:
                    ImageSource temp = this.depthBitmap;
                    using(DrawingContext dc = this.drawingGroup3.Open())
                    {
                        dc.DrawImage(temp,this.displayRect);
                    }
                    
                    break;
                case InfraredButton:
                    ImageSource temp2 = this.infraredBitmap;
                    using (DrawingContext dc = this.drawingGroup3.Open())
                    {
                        dc.DrawImage(temp2, this.displayRect);
                    }
                    break;
                case BodyIndexButton:
                    ImageSource temp4 = this.bodyIndexBitmap;
                    using (DrawingContext dc = this.drawingGroup3.Open())
                    {
                        dc.DrawImage(temp4, this.displayRect);
                    }
                    break;
            }
        }

        /// <summary>
        /// 面部特征点映射到彩色图像
        /// </summary>
        /// <param name="faceIndex"></param>
        /// <param name="faceTextLayout"></param>
        /// <returns></returns>
        private bool GetFaceTextPositionInColorSpace(int faceIndex, out Point faceTextLayout)
        {
            faceTextLayout = new Point();
            bool isLayoutValid = false;

            Body body = this.bodies[faceIndex];
            if (body.IsTracked)
            {
                var headJoint = body.Joints[JointType.Head].Position;

                CameraSpacePoint textPoint = new CameraSpacePoint()
                {
                    X = headJoint.X + TextLayoutOffsetX,
                    Y = headJoint.Y + TextLayoutOffsetY,
                    Z = headJoint.Z
                };

                ColorSpacePoint textPointInColor = this.coordinateMapper.MapCameraPointToColorSpace(textPoint);

                faceTextLayout.X = textPointInColor.X;
                faceTextLayout.Y = textPointInColor.Y;
                isLayoutValid = true;
            }

            return isLayoutValid;
        }

        /// <summary>
        /// 计算面部角度
        /// </summary>
        /// <param name="rotQuaternion"></param>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        /// <param name="roll"></param>
        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            //面部角度
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
            yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
            roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
        }

        /// <summary>
        /// 绘制疲劳警示框
        /// </summary>
        /// <param name="numeFaigute"></param>
        public void drawFaigute(double numeFaigute)
        {
            using (DrawingContext dc = this.drawingGroup2.Open())
            {
                Rect rect = new Rect(new Point(0, 0), new Point(642, 100));
                if(numeFaigute<0.3)
                    dc.DrawRectangle(Brushes.Green, null, rect);
                else if(numeFaigute<0.5)
                    dc.DrawRectangle(Brushes.Orange, null, rect);
                else if(numeFaigute<0.7)
                    dc.DrawRectangle(Brushes.Black, null, rect);
                else
                    dc.DrawRectangle(Brushes.Red, null, rect);
            }
        }

        
        /// <summary>
        /// 跟踪人体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    // 更新人体数据
                    bodyFrame.GetAndRefreshBodyData(this.bodies);


                    using (DrawingContext dc = this.drawingGroup.Open())
                    {
                        ImageSource image = this.colorBitmap;

                        //将彩色图像写入待绘制的缓冲区
                        dc.DrawImage(image, this.displayRect);

                        bool drawFaceResult = false;

                        // 遍历每个面
                        for (int i = 0; i < this.bodyCount; i++)
                        {

                            // 检查这个面部资源是否有效
                            if (this.faceFrameSources[i].IsTrackingIdValid)
                            {
                                // 检查面部结果是否有效
                                if (this.faceFrameResults[i] != null)
                                {
                                    // 绘制面部结果

                                    if (this.myBodyIndex >= 0)
                                    {
                                        if (this.faceFrameResults[this.myBodyIndex] != null)
                                        {
                                            this.DrawFaceFrameResults(this.myBodyIndex, this.faceFrameResults[this.myBodyIndex], dc);

                                            ///把将要绘制的人体索引录入文件中
                                            ///StreamWriter sw = new StreamWriter("index.txt", true);
                                            ///sw.WriteLine(i);
                                            ///sw.Close();

                                            if (!drawFaceResult)
                                            {
                                                drawFaceResult = true;
                                            }
                                            //this.myBodyIndex = -1;
                                        }
                                        
                                    }
 
                                }
                            }
                            else
                            {
                                // 检查是否跟踪相应的body
                                if (this.bodies[i].IsTracked)
                                {
                                    // 更新人脸源来跟踪这个body
                                    this.faceFrameSources[i].TrackingId = this.bodies[i].TrackingId;
                                }
                            }
                        }
                        

                        if (!drawFaceResult)
                        {
                            // if no faces were drawn then this indicates one of the following:
                            // a body was not tracked 
                            // a body was tracked but the corresponding face was not tracked
                            // a body and the corresponding face was tracked though the face box or the face points were not valid
                            dc.DrawText(
                                this.textFaceNotTracked,
                                this.textLayoutFaceNotTracked);
                        }

                        dc.Close();

                       this.drawingGroup.ClipGeometry = new RectangleGeometry(this.displayRect);
                    }
                }
            }
            drawFaigute(this.NumeFaigute);
            //this.changeFrame();
        }

        /// <summary>
        /// 人体索引帧临帧事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Reader_BodyIndexFrameArrived(Object sender,BodyIndexFrameArrivedEventArgs e)
        {

            bool bodyIndexFrameProcessed = false;

            using (BodyIndexFrame bodyIndexFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyIndexFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height) == bodyIndexBuffer.Size) & (this.bodyIndexFrameDescription.Width == this.bodyIndexBitmap.PixelWidth) && (this.bodyIndexFrameDescription.Height == this.bodyIndexBitmap.PixelHeight))
                        {
                            //this.BodyIndexToDepth(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                            this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                            bodyIndexFrameProcessed = true;
                        }
                    }
                }
                
            }
            if (bodyIndexFrameProcessed)
            {
                this.RenderBodyIndexPixels();
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            this.bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this.bodyIndexBitmap.PixelWidth, this.bodyIndexBitmap.PixelHeight),
                this.bodyIndexPixels,
                this.bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }

        /// <summary>
        /// 人体索引映射到深度图像矩形中
        /// </summary>
        /// <param name="bodyIndexFrameData">人体帧数据</param>
        /// <param name="bodyIndexFrameDataSize"></param>
        private unsafe void BodyIndexToDepth(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;

            for (long i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                //将每个人体的坐标分别存储到队列中
                if (frameData[i] < 5&&frameData[i]>=0)
                {
                    //this.DriverIndex[frameData[i]].
                }
            }
        }

        /// <summary>
        /// 通过深度图像和人体索引在深度图像中的坐标，采集到最近的人体的索引
        /// </summary>
        /// <param name="depthData"></param>
        /// <returns></returns>
        public unsafe int getDriverIndex(ushort* depthData)
        {
            int index = -1;
            double depth=0.0f;
            double[] depthDrivers=new double[6];
            if (this.DriverIndex!= null)
            {
                for (int a = 0; a < 6; ++a)
                {
                    if (this.DriverIndex[a] != null)
                    {
                        foreach (long i in this.DriverIndex[a].getData())
                        {
                            depthDrivers[a] += ((double)(depthData[i]) / (this.depthFrameDescription.Width * this.depthFrameDescription.Height));
                        }
                    }
                    
                }

                double[] functionDD={999999,999999,999999,999999,999999,99999 };
                int fddI = 0;
                for (int a = 0; a < 6; a++)
                {
                    if (depthDrivers[a] != 0)
                    {
                        functionDD[fddI] = depthDrivers[a];
                        fddI++;
                    }
                }

                depth = functionDD.Min();
                StreamWriter minW = new StreamWriter("Min.txt", true);
                minW.WriteLine(depth);
                minW.Close();
                for(int a = 0; a < 6; a++)
                {
                    if (depth == depthDrivers[a])
                        index = a;
                }
            }
            if (index >= 0)
            {
                ///把计算得到的驾驶员人体索引录入文件中
                StreamWriter sw = new StreamWriter("m_index.txt", true);
                sw.WriteLine(index);
                sw.Close();
            }
            this.myBodyIndex = index;
            this.myBodyDepth = depth;
            return index;
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// </summary>
        /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
        /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;
            for (int i = 0; i < 6; i++)
            {
                this.DriverIndex[i] = new MyQueue(this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height);
            }

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < 5)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    int temp = frameData[i];
                    this.bodyIndexPixels[i] = BodyColor[frameData[i]];
                    this.DriverIndex[frameData[i]].Push(i);
                }
                else
                {
                    // this pixel is not part of a player
                    // display black
                    this.bodyIndexPixels[i] = 0x00000000;
                }
            }
        }

        /// <summary>
        /// 彩色图像的读取和绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void color(object sender, ColorFrameArrivedEventArgs e)
        {
            updateFrame();
            // 彩色帧
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // 验证数据并将新的彩色数据写入显示位图
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                    
                }
            }
        }

        /// <summary>
        /// 确认面部特征点有效
        /// </summary>
        /// <param name="faceFrameSource"></param>
        /// <returns></returns>
        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// 确认面部特征点有效
        /// </summary>
        /// <param name="faceResult"></param>
        /// <returns></returns>
        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    //检测屏幕空间内范围内是否有有效矩形
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= this.displayWidth &&
                                  faceBox.Bottom <= this.displayHeight;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (PointF pointF in facePoints.Values)
                            {
                                //检查特征点是否有效
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < this.displayWidth &&
                                                        pointF.Y < this.displayHeight;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
        }

        /// <summary>
        /// 获取人体索引
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void getDriverIndex(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                List<Body> myBody=null;
                bodyFrame.GetAndRefreshBodyData(myBody);
                //myBody.
            }
        }

        /// <summary>
        /// 面部数据的采集
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    //获取面部索引
                    int index = this.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                    //检查改面部数据是否有效
                    if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    {
                        
                        //储存数据以待绘制和疲劳检测
                        this.faceFrameResults[index] = faceFrame.FaceFrameResult;
                    }
                    else
                    {
                        //
                        this.faceFrameResults[index] = null;
                    }
                }
            }
        }

        /// <summary>
        /// 驾驶员模型纠正运行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CorrectModule_Run(object sender,DoWorkEventArgs e)
        {
            ///如果模型创建模块状态为运行状态
            if (CorrectStatus)
            {
                ///获取当前队列中左右眼闭合频率
                double thisLeftFrequency = this.myFaigute.getLeftEyesClosedFrequency();
                double thisRightFrequency = this.myFaigute.getRightEyesClosedFrequency();
            }
        }

        /// <summary>
        /// 显示人体索引图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            this.frameType = BodyIndexButton;
        }

        /// <summary>
        /// 显示彩色图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            this.frameType = ColorButton;
        }

        /// <summary>
        /// 显示红外图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            this.frameType = InfraredButton;
        }

        ///zhangjie021
        /// <summary>
        /// 显示深度图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.frameType = DepthButton;
        }

        /// <summary>
        /// 创建驾驶员模型（加权纠正）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ///启动驾驶员模型纠正模块
            CorrectModule.RunWorkerAsync();
            CorrectNumes = 0;
        }
    }
}