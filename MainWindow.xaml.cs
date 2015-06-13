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
               camera1.Source = colorFrame.ToBitmap();

               if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
               {

                   camera.Source = _backgroundRemoval.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);


                   //camera1.Source = colorFrame.ToBitmap();

                   
                   depthFrame.Dispose();
                   bodyIndexFrame.Dispose();


               }
               colorFrame.Dispose();
           }

            
              

           
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
                               Joint leftHand = body.Joints[JointType.HandLeft];
                               Joint rightHand = body.Joints[JointType.HandRight];

                               if (head.TrackingState == TrackingState.Tracked && neck.TrackingState == TrackingState.Tracked)
                               {
                                  //cameraspacepoints
                                   CameraSpacePoint headPt = head.Position;
                                   CameraSpacePoint neckPt = neck.Position;
                                   CameraSpacePoint handLPt = leftHand.Position;
                                   CameraSpacePoint handRPt = rightHand.Position; 

                                   //Points
                                   Point headPoint = new Point();
                                   Point neckPoint = new Point();
                                   Point handLPoint = new Point();
                                   Point handRPoint = new Point();


                                   //ColorSpacePoint colorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(pt);
                                   DepthSpacePoint depthHead = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(headPt);
                                   DepthSpacePoint depthNeck = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(neckPt);

                                   headPoint.X = float.IsInfinity(depthHead.X) ? 0 : depthHead.X;
                                   headPoint.Y = float.IsInfinity(depthHead.Y) ? 0 : depthHead.Y;
                                   neckPoint.X = float.IsInfinity(depthNeck.X) ? 0 : depthNeck.X;
                                   neckPoint.Y = float.IsInfinity(depthNeck.Y) ? 0 : depthNeck.Y;

                                   /*
                                   Ellipse headcircle = new Ellipse
                                   {
                                       Width = 20,
                                       Height = 20,
                                       Stroke = new SolidColorBrush(Colors.Red),
                                       StrokeThickness = 2
                                   };
                                   Canvas.SetLeft(headcircle, (headPoint.X) - headcircle.Width / 2);
                                   Canvas.SetTop(headcircle, (headPoint.Y) - headcircle.Height / 2);
                                   //canvas.Children.Add(headcircle);
                                    */

                                   Rectangle headbox = new Rectangle
                                   {
                                       Width = 45,
                                       Height = 50,
                                       Stroke = new SolidColorBrush(Colors.Red),
                                       StrokeThickness = 2
                                       
                                   };
                                   Canvas.SetLeft(headbox, (headPoint.X) - headbox.Width / 2);
                                   Canvas.SetTop(headbox, (headPoint.Y) - headbox.Height / 2);
                                   canvas.Children.Add(headbox);

                                   Ellipse neckcircle = new Ellipse
                                   {
                                       Width = 20,
                                       Height = 20,
                                       Stroke = new SolidColorBrush(Colors.Red),
                                       StrokeThickness = 2
                                   };
                                   Canvas.SetLeft(neckcircle, (neckPoint.X) - neckcircle.Width / 2);
                                   Canvas.SetTop(neckcircle, (neckPoint.Y) - neckcircle.Height / 2);
                                   //canvas.Children.Add(neckcircle);

                                   Line headneck = new Line
                                   {
                                       X1 = headPoint.X,
                                       Y1 = headPoint.Y,
                                       X2 = neckPoint.X,
                                       Y2 = neckPoint.Y,
                                       StrokeThickness = 5,
                                       Stroke = new SolidColorBrush(Colors.Red)
                                   };
                                   //canvas.Children.Add(headneck);

                               }
                           }
                       }
                   }
                   bodyFrame.Dispose();
               }
               
           

        }


        #endregion

        

    }
}
