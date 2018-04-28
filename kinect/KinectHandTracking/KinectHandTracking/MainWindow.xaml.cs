﻿using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using RehabilitationTraining;

namespace KinectHandTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Members

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private int flag = 0;           //开启屏幕和关闭屏幕的变量, 1为开启，0为关闭                         

        private String leftHandState = "---";     
        private String rightHandState = "---";
        private String lastHandState = "---";      //the hand states
        private String AllHandState = "---";
        private String danger = "false";

        private int leftHandStateCode = 0, rightHandStateCode = 0;   // record hand state code
        IList<Body> _bodies;
        Client client = new Client();
        System.Timers.Timer t = new System.Timers.Timer(10000);   //实例化Timer类，设置间隔时间为10000毫秒；

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        private void Send_State(String HandState)
        {
            String updateUrl = Config.BASE_URL + "/update";
            String latestUrl = Config.BASE_URL + "/latest";  //没啥用，，仅用于方便查看State是否上传

            String authorization = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Config.USERNAME + ":" + Config.PASSWORD));

            // 对要发送给服务器的数据进行包装
            Dictionary<String, object> state = new Dictionary<String, object>();
            state.Add("state", AllHandState);
            if (AllHandState == "close_lasso") {
                state.Add("danger", true);
            } else {
                state.Add("danger", false);
            }
            // 转换成 Json 格式。
            string json = JsonConvert.SerializeObject(state);
            // 向服务器发送新手势
            client.Post(updateUrl, json, authorization);
        }

        private void Send_Img(String filePath)
        {
            String uploadUrl = Config.BASE_URL + "/upload";
            String test = Config.BASE_URL + "/pics";    //没啥用，，仅用于方便查看图片是否上传
            String authorization = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Config.USERNAME + ":" + Config.PASSWORD));

            client.SendPicture(uploadUrl, filePath, authorization);
        }

        private void BmpToJpg(Bitmap bmp) //位图转png
        {

            if (!Directory.Exists("D:/屏幕截图"))  //判断目录是否存在,不存在就创建 
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(" D:/屏幕截图");
                directoryInfo.Create();
            }

            String time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");//获得系统时间
            time = System.Text.RegularExpressions.Regex.Replace(time, @"[^0-9]+", "");//提取数字
            String fileName = time + ".bmp"; //创建文件名
            bmp.Save("D:/屏幕截图/" + fileName); //保存为文件.
            bmp.Dispose(); //关闭对象

            String BMPFiles = "D:/屏幕截图/" + fileName;
            BitmapImage BitImage = new BitmapImage(new Uri(BMPFiles, UriKind.Absolute));
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitImage));
            String JpegImage = time + ".png";

            //JPG文件件路径
            FileStream fileStream = new FileStream("D:/屏幕截图/" + JpegImage, FileMode.Create, FileAccess.ReadWrite);
            encoder.Save(fileStream);
            fileStream.Close();

            //Send_Img("D:/屏幕截图/" + JpegImage);
        }
        #region Event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            RehabilitationTraining.MainWindow mainWindow = new RehabilitationTraining.MainWindow();
            this.Close();
            mainWindow.Show();
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();  

            if (1 == flag)   //开启影像帧
            {
                t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //到达时间的时候执行事件；   
                t.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
                t.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；

                // Color
                using (var frame = reference.ColorFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        camera.Source = frame.ToBitmap();
                    }
                }
            }
            else
            {
                    camera.Source = null;
            }


            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                if (1 == flag)
                                {
                                    // Find the joints
                                    Joint handRight = body.Joints[JointType.HandRight];
                                    Joint thumbRight = body.Joints[JointType.ThumbRight];

                                    Joint handLeft = body.Joints[JointType.HandLeft];
                                    Joint thumbLeft = body.Joints[JointType.ThumbLeft];

                                    Joint ElbowLeft = body.Joints[JointType.ElbowLeft];
                                    Joint ElbowRight = body.Joints[JointType.ElbowRight];

                                    // Draw hands and thumbs and wrist and ELbow
                                    canvas.DrawHand(handRight, _sensor.CoordinateMapper);
                                    canvas.DrawHand(handLeft, _sensor.CoordinateMapper);
                                    canvas.DrawThumb(thumbRight, _sensor.CoordinateMapper);
                                    canvas.DrawThumb(thumbLeft, _sensor.CoordinateMapper);
                                    canvas.DrawPoint(ElbowLeft, _sensor.CoordinateMapper);
                                    canvas.DrawPoint(ElbowRight, _sensor.CoordinateMapper);

                                    //tblRightHandState.Text = rightHandState;
                                    //tblLeftHandState.Text = leftHandState;
                                    tblhandState.Text = AllHandState;

                                }

                                switch (body.HandRightState)
                                {
                                    case HandState.Open:
                                        rightHandState = "Open";
                                        rightHandStateCode = 1;
                                        break;
                                    case HandState.Closed:
                                        rightHandState = "Closed";
                                        rightHandStateCode = 2;
                                        break;
                                    case HandState.Lasso:
                                        rightHandState = "Lasso";
                                        rightHandStateCode = 3;
                                        break;
                                    case HandState.Unknown:
                                        rightHandState = "Unknown...";
                                        rightHandStateCode = 0;
                                        break;
                                    case HandState.NotTracked:
                                        rightHandState = "Not tracked";
                                        rightHandStateCode = -1;
                                        break;
                                    default:
                                        break;
                                }

                                switch (body.HandLeftState)
                                {
                                    case HandState.Open:
                                        leftHandState = "Open";
                                        leftHandStateCode = 1;
                                        break;
                                    case HandState.Closed:
                                        leftHandState = "Closed";
                                        leftHandStateCode = 2;
                                        break;
                                    case HandState.Lasso:
                                        leftHandState = "Lasso";
                                        leftHandStateCode = 3;
                                        break;
                                    case HandState.Unknown:
                                        leftHandState = "Unknown...";
                                        leftHandStateCode = 0;
                                        break;
                                    case HandState.NotTracked:
                                        leftHandState = "Not tracked";
                                        leftHandStateCode = -1;
                                        break;
                                    default:
                                        break;
                                }


                                if (leftHandStateCode == 1 && rightHandStateCode == 1)
                                {
                                    tbltips.Text = "双手做剪刀动作使设备关闭";
                                    AllHandState = "open_open";
                                    flag = 1;
                                }
                                else if (leftHandStateCode == 1 && rightHandStateCode == 2)
                                {
                                    AllHandState = "open_close";
                                }
                                else if (leftHandStateCode == 1 && rightHandStateCode == 3)
                                {
                                    AllHandState = "open_lasso";
                                }
                                else if (leftHandStateCode == 2 && rightHandStateCode == 1)
                                {
                                    AllHandState = "close_open";
                                }
                                else if (leftHandStateCode == 2 && rightHandStateCode == 2)
                                {
                                    AllHandState = "close_close";
                                }
                                else if (leftHandStateCode == 2 && rightHandStateCode == 3)
                                {
                                    AllHandState = "close_lasso";
                                }
                                else if (leftHandStateCode == 3 && rightHandStateCode == 1)
                                {
                                    AllHandState = "lasso_open";

                                    //this.Hide();
                                    //RehabilitationTraining.MainWindow mainWindow = new RehabilitationTraining.MainWindow();
                                    //this.Close();
                                    //mainWindow.Show();
                                }
                                else if (leftHandStateCode == 3 && rightHandStateCode == 2)
                                {
                                    
                                    AllHandState = "lasso_close";
                                }
                                else if (leftHandStateCode == 3 && rightHandStateCode == 3)
                                {  
                                    tbltips.Text = "双手打开使设备开启";
                                    AllHandState = "lasso_lasso";
                                    flag = 0;

                                    if (MessageBox.Show("检测到退出手势，确认退出？", "退出", MessageBoxButton.YesNo) == MessageBoxResult.OK)
                                    {
                                        Application.Current.Shutdown();
                                    }
                                }


                                if (!AllHandState.Equals(lastHandState) && 1 == flag)
                                {
                                    lastHandState = AllHandState;
                                    //Send_State(AllHandState);
                                }
                            }
                        }
                    }
                }
            }

            void theout(object source, System.Timers.ElapsedEventArgs f)
            {
                //Scrennshot
                using (var frame = reference.ColorFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        System.IO.MemoryStream ms = new System.IO.MemoryStream();
                        BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create((BitmapSource)frame.ToBitmap()));
                        encoder.Save(ms);
                        Bitmap bp = new Bitmap(ms);
                        //BmpToJpg(bp);
                         
                        ms.Close();
                    }
                }
            }
        }
        #endregion
    }

}
