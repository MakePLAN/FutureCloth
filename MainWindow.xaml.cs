using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace FutureCloth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Members

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;
        private CoordinateMapper coordinateMapper = null;
        BackgroundRemoval _backgroundRemoval;
        ulong player = 0;
        byte[] colordata = null;
        int widthMask = 60;
        int lengthMask = 65; 

        #endregion

        #region Constructor

        public MainWindow()
        {

            InitializeComponent();

            
        }
        #endregion

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
            this.coordinateMapper = _sensor.CoordinateMapper;
            if (_sensor != null)
            {
                _sensor.Open();
                _backgroundRemoval = new BackgroundRemoval(_sensor.CoordinateMapper);
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);
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

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            /*
            BitmapImage b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri("shirt.jpg", UriKind.RelativeOrAbsolute);
            b.EndInit();

            this.oldPic.Source = b; 
              */
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
           var colorFrame = reference.ColorFrameReference.AcquireFrame();
           var depthFrame = reference.DepthFrameReference.AcquireFrame();
           var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame();
           var bodyFrame = reference.BodyFrameReference.AcquireFrame();

           if (colorFrame != null)
           {
               //camera1.Source = colorFrame.ToBitmap();

               if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
               {

                   //camera.Source = _backgroundRemoval.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);


                   //camera1.Source = colorFrame.ToBitmap();

                   
                   depthFrame.Dispose();
                   bodyIndexFrame.Dispose();


               }
               //colorFrame.Dispose();
           

            
              

           
               if (bodyFrame != null)
               {
                   canvas.Children.Clear();

                   _bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];

                   bodyFrame.GetAndRefreshBodyData(_bodies);

                   foreach (var body in _bodies)
                   {

                       if (body.IsTracked)
                       {
                           if (player == 0)
                           {
                               player = body.TrackingId;
                           }

                           if (body.TrackingId == player)
                           {
                               //Joints
                               Joint head = body.Joints[JointType.Head];
                               Joint neck = body.Joints[JointType.Neck];
                               
                               Joint spineMid = body.Joints[JointType.SpineMid];
                               Joint spineBase = body.Joints[JointType.SpineBase];
                               Joint spineShoulder = body.Joints[JointType.SpineShoulder];
                               Joint leftShoulder = body.Joints[JointType.ShoulderLeft];
                               Joint rightShoulder = body.Joints[JointType.ShoulderRight];

                               if (head.TrackingState == TrackingState.Tracked && neck.TrackingState == TrackingState.Tracked && spineMid.TrackingState == TrackingState.Tracked && spineBase.TrackingState == TrackingState.Tracked && spineShoulder.TrackingState == TrackingState.Tracked && leftShoulder.TrackingState == TrackingState.Tracked && rightShoulder.TrackingState == TrackingState.Tracked)
                               {
                                   int colorWidth = colorFrame.FrameDescription.Width;
                                   int colorHeight = colorFrame.FrameDescription.Height;

                                   colordata = new byte[colorWidth * colorHeight * ((PixelFormats.Bgr32.BitsPerPixel + 7)/8)  ];
                                   if ((colorWidth * colorHeight * ((PixelFormats.Bgr32.BitsPerPixel + 7)/8)) == colordata.Length) 
                                    {

                                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                        {
                                            colorFrame.CopyRawFrameDataToArray(colordata);
                                        }
                                        else
                                        {
                                            colorFrame.CopyConvertedFrameDataToArray(colordata, ColorImageFormat.Bgra);
                                        }
                                   }
                                  //cameraspacepoints
                                   CameraSpacePoint headPt = head.Position;
                                   CameraSpacePoint neckPt = neck.Position;
                                   CameraSpacePoint midSpinePt = spineMid.Position;
                                   CameraSpacePoint baseSpinePt = spineBase.Position;
                                   CameraSpacePoint shoulderSpinePt = spineShoulder.Position;
                                   CameraSpacePoint leftShoulderPt = leftShoulder.Position;
                                   CameraSpacePoint rightShoulderPt = rightShoulder.Position;

                                   //Points
                                   Point headPoint = new Point();
                                   Point neckPoint = new Point();
                                   Point midSpinePoint = new Point();
                                   Point baseSpinePoint = new Point();
                                   Point shoulderSpinePoint = new Point();
                                   Point leftShoulderPoint = new Point();
                                   Point rightShoulderPoint = new Point();


                                   //ColorSpacePoint colorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(pt);
                                   DepthSpacePoint depthHead = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(headPt);
                                   DepthSpacePoint depthNeck = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(neckPt);
                                   DepthSpacePoint depthSpineMid = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(midSpinePt);
                                   DepthSpacePoint depthSpineBase = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(baseSpinePt);
                                   DepthSpacePoint depthSpineShoulder = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(shoulderSpinePt);
                                   DepthSpacePoint depthLeftShoulder = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(leftShoulderPt);
                                   DepthSpacePoint depthRightShoulder = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(rightShoulderPt);

                                   headPoint.X = float.IsInfinity(depthHead.X) ? 0 : depthHead.X;
                                   headPoint.Y = float.IsInfinity(depthHead.Y) ? 0 : depthHead.Y;
                                   neckPoint.X = float.IsInfinity(depthNeck.X) ? 0 : depthNeck.X;
                                   neckPoint.Y = float.IsInfinity(depthNeck.Y) ? 0 : depthNeck.Y;
                                   midSpinePoint.X = float.IsInfinity(depthSpineMid.X) ? 0 : depthSpineMid.X;
                                   midSpinePoint.Y = float.IsInfinity(depthSpineMid.Y) ? 0 : depthSpineMid.Y;
                                   baseSpinePoint.X = float.IsInfinity(depthSpineBase.X) ? 0 : depthSpineBase.X;
                                   baseSpinePoint.Y = float.IsInfinity(depthSpineBase.Y) ? 0 : depthSpineBase.Y;
                                   shoulderSpinePoint.X = float.IsInfinity(depthSpineShoulder.X) ? 0 : depthSpineShoulder.X;
                                   shoulderSpinePoint.Y = float.IsInfinity(depthSpineShoulder.Y) ? 0 : depthSpineShoulder.Y;
                                   leftShoulderPoint.X = float.IsInfinity(depthLeftShoulder.X) ? 0 : depthLeftShoulder.X;
                                   leftShoulderPoint.Y = float.IsInfinity(depthLeftShoulder.Y) ? 0 : depthLeftShoulder.Y;
                                   rightShoulderPoint.X = float.IsInfinity(depthRightShoulder.X) ? 0 : depthRightShoulder.X;
                                   rightShoulderPoint.Y = float.IsInfinity(depthRightShoulder.Y) ? 0 : depthRightShoulder.Y;

                                   BitmapImage image = new BitmapImage();
                                   image.BeginInit();
                                   image.UriSource = new Uri("jason.png", UriKind.RelativeOrAbsolute);
                                   image.EndInit();
                                   ImageBrush myImageBrush = new ImageBrush(image);
                                   Canvas myCanvas = new Canvas();
                                   myCanvas.Width = widthMask;
                                   myCanvas.Height = lengthMask;
                                   myCanvas.Background = myImageBrush;
                                   Canvas.SetLeft(myCanvas, (headPoint.X) - myCanvas.Width / 2);
                                   Canvas.SetTop(myCanvas, (headPoint.Y) - 5 - (myCanvas.Height) / 2);
                                   canvas.Children.Add(myCanvas);
                                   
                                   widthMask = (int)(rightShoulderPoint.X - leftShoulderPoint.X) * 8 / 10;
                                   lengthMask = (int)(rightShoulderPoint.X - leftShoulderPoint.X) * 8 / 10;

                                   //this.number.Content = difference.ToString();
                                   double headSpine = headPoint.X - midSpinePoint.X;
                                   //head tilting
                                   canvas.RenderTransform = new RotateTransform(120 * (headSpine) / 80, headPoint.X, headPoint.Y);
                                   

                                   int[] A;
                                   A = new int[25];
                                   int[] B;
                                   B = new int[25];
                                   int[] C;
                                   C = new int[25];
                                   int[] D;
                                   D = new int[25];

                                   for (int i = 0; i < 5; i++)
                                   {
                                       for (int j = 0; j < 5; j++)
                                       {
                                           A[i + 5 * j] = colordata[4*((int)(midSpinePoint.X*4-10+5*i)+colorWidth*(int)((midSpinePoint.Y*2)-10+5*i))];
                                           B[i + 5 * j] = colordata[4 * ((int)(midSpinePoint.X*4 - 10 + 5 * i) + colorWidth * (int)((midSpinePoint.Y*2) - 10 + 5 * i))+1];
                                           C[i + 5 * j] = colordata[4 * ((int)(midSpinePoint.X*4 - 10 + 5 * i) + colorWidth * (int)((midSpinePoint.Y*2) - 10 + 5 * i))+2];
                                           D[i + 5 * j] = colordata[4 * ((int)(midSpinePoint.X*4 - 10 + 5 * i) + colorWidth * (int)((midSpinePoint.Y*2) - 10 + 5 * i))+3];
                                       }
                                   }

                                   for (int x = (int)(midSpinePoint.X*4- baseSpinePoint.Y*2 + shoulderSpinePoint.Y*2); x < (midSpinePoint.X*4 + baseSpinePoint.Y*2 - shoulderSpinePoint.Y*2); x++)
                                   {
                                       for (int y = (int)(shoulderSpinePoint.Y -10)*2; y < (baseSpinePoint.Y + 10)*3; y++)
                                       {
                                           if (4 * (x + colorWidth * y) > (colorWidth * colorHeight * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)))
                                           {
                                               continue;
                                           }
                                           int colorA = colordata[4 * (x + colorWidth * y)];
                                           int colorB = colordata[4 * (x + colorWidth * y)+1];
                                           int colorC = colordata[4 * (x + colorWidth * y)+2];
                                           int colorD = colordata[4 * (x + colorWidth * y)+3];

                                           for (int i = 0; i < 25; i++)
                                           {
                                               if ((A[i] >= colorA-10)&&(A[i] <= colorA+10)&&(B[i] >= colorB-10)&&(B[i] <= colorB+10)&&(C[i] >= colorC-10)&&(C[i] <= colorC+10)&&(D[i] >= colorD-10)&&(D[i] <= colorD+10))
                                               {
                                                   colordata[4 * (x + colorWidth * y)] = 0x99;
                                                   colordata[4 * (x + colorWidth * y)] = 0x99;
                                                   colordata[4 * (x + colorWidth * y)] = 0x99;
                                                   colordata[4 * (x + colorWidth * y)] = 0xff;
                                                   break;
                                               }
                                           }
                                       }
                                   }

                                   int stride = colorWidth * PixelFormats.Bgr32.BitsPerPixel / 8;

                                   //camera1.Source = colorFrame.ToBitmap();
                                   camera1.Source = BitmapSource.Create(colorWidth, colorHeight, 96, 96, PixelFormats.Bgr32, null, colordata, stride);
                                   colorFrame.Dispose();

                                   

                               }
                           }
                       }
                   }
                   bodyFrame.Dispose();
               }

               colorFrame.Dispose();
            }
        }


        #endregion

        

    }
}
