using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;

using System.Runtime.InteropServices;
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
using System.Globalization;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;


namespace FutureCloth
{

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "In a full-fledged application, the SpeechRecognitionEngine object should be properly disposed. For the sake of simplicity, we're omitting that code in this sample.")]

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
        ImageBrush[] images = new ImageBrush[5];
        Random random = null;
        bool check = false; //check for rather speech is called
        int num = 4;
        int state = 0;
        ColorFrame colorFrame = null;
        WriteableBitmap colorBitmap = null;
        FrameDescription colorFrameDescription = null;


        //Speech members
        private KinectAudioStream convertStream = null; //stream for 32b-16b conversion
        private SpeechRecognitionEngine speechEngine = null; //speech recognition engine using audio data from kienct 
        private List<Span> recognitionSpans; //list of all UI span elements used to select recognized text 

        #endregion

        #region Constructor

        public MainWindow()
        {

            InitializeComponent();


        }
        #endregion

        #region Events

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            _sensor = KinectSensor.GetDefault();
            this.coordinateMapper = _sensor.CoordinateMapper;
            if (_sensor != null)
            {
                _sensor.Open();
                _backgroundRemoval = new BackgroundRemoval(_sensor.CoordinateMapper, _sensor);
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                //grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = this._sensor.AudioSource.AudioBeams;
                System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

                //create the convert stream 
                this.convertStream = new KinectAudioStream(audioStream);


                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("jason.png", UriKind.RelativeOrAbsolute);
                image.EndInit();
                ImageBrush myImageBrush = new ImageBrush(image);
                images[0] = myImageBrush;

                BitmapImage image1 = new BitmapImage();
                image1.BeginInit();
                image1.UriSource = new Uri("ironman.jpg", UriKind.RelativeOrAbsolute);
                image1.EndInit();
                ImageBrush myImageBrush1 = new ImageBrush(image1);
                images[1] = myImageBrush1;

                BitmapImage image2 = new BitmapImage();
                image2.BeginInit();
                image2.UriSource = new Uri("scream.JPG", UriKind.RelativeOrAbsolute);
                image2.EndInit();
                ImageBrush myImageBrush2 = new ImageBrush(image2);
                images[2] = myImageBrush2;

                BitmapImage image3 = new BitmapImage();
                image3.BeginInit();
                image3.UriSource = new Uri("ventena.jpg", UriKind.RelativeOrAbsolute);
                image3.EndInit();
                ImageBrush myImageBrush3 = new ImageBrush(image3);
                images[3] = myImageBrush3;

                images[4] = null;

                random = new Random();
            }

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
                //this.recognitionSpans = new List<Span> { forwardSpan, backSpan, rightSpan, leftSpan };

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);


                //Use this code to create grammar programmatically rather than froma grammar file.

                var directions = new Choices();
                directions.Add(new SemanticResultValue("mask", "MASK"));
                directions.Add(new SemanticResultValue("masks", "MASK"));



                directions.Add(new SemanticResultValue("shirt", "SHIRT"));
                directions.Add(new SemanticResultValue("shirts", "SHIRT"));
                directions.Add(new SemanticResultValue("upper", "SHIRT"));
                directions.Add(new SemanticResultValue("uppers", "SHIRT"));

                directions.Add(new SemanticResultValue("pant", "PANT"));
                directions.Add(new SemanticResultValue("pants", "PANT"));
                directions.Add(new SemanticResultValue("lower", "PANT"));
                directions.Add(new SemanticResultValue("lowers", "PANT"));

                directions.Add(new SemanticResultValue("change", "CHANGE"));
                directions.Add(new SemanticResultValue("changes", "CHANGE"));
                directions.Add(new SemanticResultValue("swap", "CHANGE"));

                directions.Add(new SemanticResultValue("off", "OFF"));
                directions.Add(new SemanticResultValue("offs", "OFF"));

                directions.Add(new SemanticResultValue("finish", "DONE"));
                directions.Add(new SemanticResultValue("done", "DONE"));



                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(directions);

                var g = new Grammar(gb);


                // Create a grammar from grammar definition XML file.
                //var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.SpeechGrammar));

                //var g = new Grammar(memoryStream);
                this.speechEngine.LoadGrammar(g);


                this.speechEngine.SpeechRecognized += this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected += this.SpeechRejected;

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {

            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.convertStream != null)
            {
                this.convertStream.SpeechActive = false;
            }

            if (this.speechEngine != null)
            {
                this.speechEngine.SpeechRecognized -= this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= this.SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }

            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        /// <summary>
        /// Remove any highlighting from recognition instructions.
        /// </summary>
        private void ClearRecognitionHighlights()
        {
            /*
            foreach (Span span in this.recognitionSpans)
            {
                span.Foreground = (Brush)this.Resources[MediumGreyBrushKey];
                span.FontWeight = FontWeights.Normal;
            }*/

        }

        private void screenShot()
        {
            if (this.colorBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(colorBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = System.IO.Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    //this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    //this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;


            this.ClearRecognitionHighlights();

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "MASK":
                        state = 1;
                        break;

                    case "SHIRT":
                        state = 2;
                        break;

                    case "PANT":
                        state = 3;
                        break;
                    case "OFF":
                        if (state == 1)
                        {
                            num = 4;
                        }

                        else if (state == 2)
                        {

                        }

                        else if (state == 3)
                        {

                        }

                        break;



                    case "CHANGE":
                        if (state == 1)
                        {
                            check = true;
                        }

                        else if (state == 2)
                        {

                        }

                        else if (state == 3)
                        {

                        }
                        break;
                    case "DONE":
                        screenShot();
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.ClearRecognitionHighlights();
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();
            
            // Color
            colorFrame = reference.ColorFrameReference.AcquireFrame();
            var depthFrame = reference.DepthFrameReference.AcquireFrame();
            var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame();
            var bodyFrame = reference.BodyFrameReference.AcquireFrame();

            if (colorFrame != null)
            {
                colorFrameDescription = colorFrame.FrameDescription;
                this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                //camera1.Source = colorFrame.ToBitmap();

                if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
                {

                    camera.Source = _backgroundRemoval.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);


                    //camera1.Source = colorFrame.ToBitmap();

                    //colorFrame.Dispose();
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
                                Joint leftHand = body.Joints[JointType.HandLeft];
                                Joint rightHand = body.Joints[JointType.HandRight];
                                Joint spineMid = body.Joints[JointType.SpineMid];
                                Joint spineBase = body.Joints[JointType.SpineBase];
                                Joint spineShoulder = body.Joints[JointType.SpineShoulder];
                                Joint leftShoulder = body.Joints[JointType.ShoulderLeft];
                                Joint rightShoulder = body.Joints[JointType.ShoulderRight];



                                if (head.TrackingState == TrackingState.Tracked && neck.TrackingState == TrackingState.Tracked && spineMid.TrackingState == TrackingState.Tracked && spineBase.TrackingState == TrackingState.Tracked && spineShoulder.TrackingState == TrackingState.Tracked && leftShoulder.TrackingState == TrackingState.Tracked && rightShoulder.TrackingState == TrackingState.Tracked)
                                {

                                    int colorWidth = colorFrame.FrameDescription.Width;
                                    int colorHeight = colorFrame.FrameDescription.Height;


                                    colordata = new byte[colorWidth * colorHeight * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];
                                    if ((colorWidth * colorHeight * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)) == colordata.Length)
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
                                    CameraSpacePoint leftHandPt = leftHand.Position;
                                    CameraSpacePoint rightHandPt = rightHand.Position;
                                    CameraSpacePoint midSpinePt = spineMid.Position;
                                    CameraSpacePoint baseSpinePt = spineBase.Position;
                                    CameraSpacePoint shoulderSpinePt = spineShoulder.Position;
                                    CameraSpacePoint leftShoulderPt = leftShoulder.Position;
                                    CameraSpacePoint rightShoulderPt = rightShoulder.Position;

                                    //Points
                                    Point headPoint = new Point();
                                    Point neckPoint = new Point();
                                    Point leftHandPoint = new Point();
                                    Point rightHandPoint = new Point();
                                    Point midSpinePoint = new Point();
                                    Point baseSpinePoint = new Point();
                                    Point shoulderSpinePoint = new Point();
                                    Point leftShoulderPoint = new Point();
                                    Point rightShoulderPoint = new Point();


                                    //ColorSpacePoint colorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(pt);
                                    DepthSpacePoint depthHead = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(headPt);
                                    DepthSpacePoint depthNeck = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(neckPt);
                                    DepthSpacePoint depthleftHand = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(leftHandPt);
                                    DepthSpacePoint depthrightHand = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(rightHandPt);
                                    DepthSpacePoint depthSpineMid = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(midSpinePt);
                                    DepthSpacePoint depthSpineBase = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(baseSpinePt);
                                    DepthSpacePoint depthSpineShoulder = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(shoulderSpinePt);
                                    DepthSpacePoint depthLeftShoulder = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(leftShoulderPt);
                                    DepthSpacePoint depthRightShoulder = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(rightShoulderPt);

                                    headPoint.X = float.IsInfinity(depthHead.X) ? 0 : depthHead.X;
                                    headPoint.Y = float.IsInfinity(depthHead.Y) ? 0 : depthHead.Y;
                                    neckPoint.X = float.IsInfinity(depthNeck.X) ? 0 : depthNeck.X;
                                    neckPoint.Y = float.IsInfinity(depthNeck.Y) ? 0 : depthNeck.Y;
                                    leftHandPoint.X = float.IsInfinity(depthleftHand.X) ? 0 : depthleftHand.X;
                                    leftHandPoint.Y = float.IsInfinity(depthleftHand.Y) ? 0 : depthleftHand.Y;
                                    rightHandPoint.X = float.IsInfinity(depthrightHand.X) ? 0 : depthrightHand.X;
                                    rightHandPoint.Y = float.IsInfinity(depthrightHand.Y) ? 0 : depthrightHand.Y;
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


                                    Rectangle rectangle = new Rectangle
                                    {
                                        Width = 100,
                                        Height = 20,
                                        Fill = new SolidColorBrush(Colors.Blue)

                                    };

                                    if (state == 1)
                                    {
                                        Canvas.SetLeft(rectangle, (headPoint.X) - rectangle.Width / 2 + 100);
                                        Canvas.SetTop(rectangle, (headPoint.Y) - (rectangle.Height) / 2 + 20);
                                        canvas.Children.Add(rectangle);
                                    }

                                    else if (state == 2)
                                    {
                                        Canvas.SetLeft(rectangle, (midSpinePoint.X) - rectangle.Width / 2 + 100);
                                        Canvas.SetTop(rectangle, (midSpinePoint.Y) - (rectangle.Height) / 2 + 20);
                                        canvas.Children.Add(rectangle);
                                    }

                                    else if (state == 3)
                                    {
                                        Canvas.SetLeft(rectangle, (baseSpinePoint.X) - rectangle.Width / 2 + 100);
                                        Canvas.SetTop(rectangle, (baseSpinePoint.Y) - (rectangle.Height) / 2 + 20);
                                        canvas.Children.Add(rectangle);
                                    }

                                    Canvas myCanvas = new Canvas();
                                    myCanvas.Width = widthMask;
                                    myCanvas.Height = lengthMask;
                                    if (check)
                                    {
                                        num = random.Next(0, 3);
                                        myCanvas.Background = images[num];
                                        check = false;
                                    }
                                    else
                                    {
                                        myCanvas.Background = images[num];
                                    }
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

                                    //shirt
                                    for (int i = 0; i < 2; i++)
                                    {
                                        for (int j = 0; j < 2; j++)
                                        {
                                            try{
                                                A[i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((midSpinePoint.Y * 2) - 10 + 5 * i))];
                                                B[i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((midSpinePoint.Y * 2) - 10 + 5 * i)) + 1];
                                                C[i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((midSpinePoint.Y * 2) - 10 + 5 * i)) + 2];
                                                D[i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((midSpinePoint.Y * 2) - 10 + 5 * i)) + 3];
                                            }
                                                catch{
                                                    continue;
                                                }
                                        }
                                    }
                                    
                                    //pants
                                    for (int i = 0; i < 2; i++)
                                    {
                                        for (int j = 0; j < 2; j++)
                                        {
                                            
                                                A[9 + i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((baseSpinePoint.Y * 3) + 10 + 5 * i))];
                                                B[9 + i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((baseSpinePoint.Y * 3) + 10 + 5 * i)) + 1];
                                                C[9 + i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((baseSpinePoint.Y * 3) + 10 + 5 * i)) + 2];
                                                D[9 + i + 2 * j] = colordata[4 * ((int)(midSpinePoint.X * 4 - 10 + 5 * i) + colorWidth * (int)((baseSpinePoint.Y * 3) + 10 + 5 * i)) + 3];
                                           
                                        }
                                    }

                                    //shirt
                                    for (int x = (int)(midSpinePoint.X * 4 - baseSpinePoint.Y * 2 + shoulderSpinePoint.Y * 2); x < (midSpinePoint.X * 4 + baseSpinePoint.Y * 2 - shoulderSpinePoint.Y * 2); x += 4)
                                    {
                                        for (int y = (int)(shoulderSpinePoint.Y - 10) * 2; y < (baseSpinePoint.Y + 10) * 3; y += 4)
                                        {
                                            int colorA = 0;
                                            int colorB = 0;
                                            int colorC = 0;
                                            int colorD = 0;

                                            if (4 * (x + colorWidth * y) > (colorWidth * colorHeight * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)))
                                            {
                                                continue;
                                            }
                                            try
                                            {
                                                colorA = colordata[4 * (x + colorWidth * y)];
                                                colorB = colordata[4 * (x + colorWidth * y) + 1];
                                                colorC = colordata[4 * (x + colorWidth * y) + 2];
                                                colorD = colordata[4 * (x + colorWidth * y) + 3];
                                            }
                                            catch
                                            {
                                                continue;
                                            }

                                            for (int i = 0; i < 4; i++)
                                            {
                                                if ((A[i] >= colorA - 10) && (A[i] <= colorA + 10) && (B[i] >= colorB - 10) && (B[i] <= colorB + 10) && (C[i] >= colorC - 10) && (C[i] <= colorC + 10) && (D[i] >= colorD - 10) && (D[i] <= colorD + 10))
                                                {
                                                    for (int ind1 = 0; ind1 < 4; ind1++)
                                                    {
                                                        for (int ind2 = 0; ind2 < 4; ind2++)
                                                        {
                                                            try
                                                            {
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2))] = 0x99;
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2)) + 1] = 0x00;
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2)) + 2] = 0x99;
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2)) + 3] = 0xff;
                                                            }
                                                            catch
                                                            {
                                                                continue;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }


                                    //pants
                                    
                                    for (int x = (int)(midSpinePoint.X * 4 - baseSpinePoint.Y * 2 + shoulderSpinePoint.Y * 2); x < (midSpinePoint.X * 4 + baseSpinePoint.Y * 2 - shoulderSpinePoint.Y * 2); x += 4)
                                    {
                                        for (int y = (int)(baseSpinePoint.Y + 10) * 3; y < (baseSpinePoint.Y + 10) * 6; y += 4)
                                        {
                                            if (4 * (x + colorWidth * y) > (colorWidth * colorHeight * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)))
                                            {
                                                continue;
                                            }
                                            int colorA = colordata[4 * (x + colorWidth * y)];
                                            int colorB = colordata[4 * (x + colorWidth * y) + 1];
                                            int colorC = colordata[4 * (x + colorWidth * y) + 2];
                                            int colorD = colordata[4 * (x + colorWidth * y) + 3];

                                            for (int i = 9; i < 13; i++)
                                            {
                                                if ((A[i] >= colorA - 10) && (A[i] <= colorA + 10) && (B[i] >= colorB - 10) && (B[i] <= colorB + 10) && (C[i] >= colorC - 10) && (C[i] <= colorC + 10) && (D[i] >= colorD - 10) && (D[i] <= colorD + 10))
                                                {
                                                    for (int ind1 = 0; ind1 < 4; ind1++)
                                                    {
                                                        for (int ind2 = 0; ind2 < 4; ind2++)
                                                        {
                                                            try
                                                            {
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2))] = 0x00;
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2)) + 1] = 0x99;
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2)) + 2] = 0x00;
                                                                colordata[4 * (x + ind1 + colorWidth * (y + ind2)) + 3] = 0xff;
                                                            }
                                                            catch
                                                            {
                                                                continue;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    int stride = colorWidth * PixelFormats.Bgr32.BitsPerPixel / 8;

                                    Canvas moneyBox = new Canvas();
                                    moneyBox.Width = 100;
                                    moneyBox.Height = 100;

                                    BitmapImage image = new BitmapImage();
                                    image.BeginInit();
                                    image.UriSource = new Uri("money.jpeg", UriKind.RelativeOrAbsolute);
                                    image.EndInit();
                                    ImageBrush myImageBrush = new ImageBrush(image);
                                    moneyBox.Background = myImageBrush;
                                    Canvas.SetLeft(moneyBox, 200);
                                    Canvas.SetTop(moneyBox, 200);
                                    canvas1.Children.Add(moneyBox);

                                    //camera1.Source = colorFrame.ToBitmap();
                                    camera1.Source = BitmapSource.Create(colorWidth, colorHeight, 96, 96, PixelFormats.Bgr32, null, colordata, stride);
                                    colorFrame.Dispose();



                                }
                            }
                            //colorFrame.Dispose();
                        }
                    }
                    bodyFrame.Dispose();
                }


            }
        }


        #endregion


    }
    }


