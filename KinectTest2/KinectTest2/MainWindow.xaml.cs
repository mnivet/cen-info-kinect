﻿namespace KinectTest2
{
    using System;
    using System.IO.Ports;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinect;

        private RecognitionModule recognitionModule;

        public UartManager UartManager { get; private set; }

        private SkeletonsModule skeletonsModule;

        private int min = 300;

        private int max = 400;

        public MainWindow()
        {
            this.recognitionModule = new RecognitionModule(this);
            this.skeletonsModule = new SkeletonsModule(this);
            InitializeComponent();
            NearDepthMinDistanceTextBox.Text = min.ToString();
            NearDepthMaxDistanceTextBox.Text = max.ToString();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            KinectSensorCollection kinectSensorCollection = KinectSensor.KinectSensors;
            int sensorCount = kinectSensorCollection.Count;
            if (sensorCount > 0)
            {
                kinect = kinectSensorCollection[0];
                kinectSensorCollection.StatusChanged += (o, args) =>
                {
                    KinectStatusValue.Content = args.Status.ToString();
                };

                if (LaunchButton.Content.ToString() == "Start")
                {
                    kinect.Start();
                    KinectIdValue.Content = kinect.DeviceConnectionId;
                    LaunchButton.Content = "Stop";
                    ElevationSlider.Value = kinect.ElevationAngle;

                    this.InitKinectDisplay();
                    this.recognitionModule.Start(kinect);
                    this.skeletonsModule.Start(this.kinect);
                }
                else
                {
                    this.skeletonsModule.Stop();
                    this.CleanKinectDisplay();
                    kinect.Stop();
                    kinect = null;
                    KinectIdValue.Content = "-";
                    KinectStatusValue.Content = "-";
                    LaunchButton.Content = "Start";
                }
            }
        }


        private void InitKinectDisplay()
        {
            this.CleanKinectDisplay();

            if (true == VideoRadioButton.IsChecked)
            {
                kinect.ColorStream.Enable();
                kinect.ColorFrameReady += KinectOnColorFrameReady;

            }
            else if (true == DepthRadioButton.IsChecked)
            {
                kinect.DepthStream.Enable();
                kinect.DepthFrameReady += KinectOnDepthFrameReady;
            }
            else if (true == PhotoRadioButton.IsChecked)
            {
                kinect.ColorStream.Enable();
            }
        }


        private void CleanKinectDisplay()
        {
            kinect.ColorFrameReady -= KinectOnColorFrameReady;
            kinect.DepthFrameReady -= KinectOnDepthFrameReady;
            if (kinect.ColorStream.IsEnabled)
            {
                kinect.ColorStream.Disable();
            }
            if (kinect.DepthStream.IsEnabled)
            {
                kinect.DepthStream.Disable();
            }
        }

        private void KinectOnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs depthImageFrameReadyEventArgs)
        {
            using (DepthImageFrame frame = depthImageFrameReadyEventArgs.OpenDepthImageFrame())
            {
                ShowDepthImageFrame(frame);
            }
        }

        private void KinectOnColorFrameReady(object sender, ColorImageFrameReadyEventArgs colorImageFrameReadyEventArgs)
        {
            using (ColorImageFrame frame = colorImageFrameReadyEventArgs.OpenColorImageFrame())
            {
                ShowColorImageFrame(frame);
            }
        }

        private async void TakePicture_Click(object sender, RoutedEventArgs e)
        {
            if (this.kinect == null) return;

            kinect.ColorFrameReady -= KinectOnColorFrameReady;
            kinect.DepthFrameReady -= KinectOnDepthFrameReady;

            if (!kinect.ColorStream.IsEnabled) kinect.ColorStream.Enable();

            using (ColorImageFrame frame = kinect.ColorStream.OpenNextFrame(100))
            {
                ShowColorImageFrame(frame);
            }

            var restoreDisplayTask = new Task(
                () =>
                {
                    Thread.Sleep(1000);
                });
            restoreDisplayTask.Start();

            await restoreDisplayTask;

            this.InitKinectDisplay();
        }

        private void ShowDepthImageFrame(DepthImageFrame frame)
        {
            if (frame == null)
            {
                return;
            }
            DepthImagePixel[] depthPixels = new DepthImagePixel[frame.PixelDataLength];
            byte[] colorPixels = new byte[frame.PixelDataLength*sizeof (int)];
            frame.CopyDepthImagePixelDataTo(depthPixels);

            WriteableBitmap bitmap = new WriteableBitmap(frame.Width, frame.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // Get the min and max reliable depth for the current frame
            int minDepth = frame.MinDepth;
            int maxDepth = frame.MaxDepth;

            int depthInRangeCount = 0;

            // Convert the depth to RGB
            int colorPixelIndex = 0;
            for (int i = 0; i < depthPixels.Length; ++i)
            {
                // Get the depth for this pixel
                short depth = depthPixels[i].Depth;

                // To convert to a byte, we're discarding the most-significant
                // rather than least-significant bits.
                // We're preserving detail, although the intensity will "wrap."
                // Values outside the reliable depth range are mapped to 0 (black).

                // Note: Using conditionals in this loop could degrade performance.
                // Consider using a lookup table instead when writing production code.
                // See the KinectDepthViewer class used by the KinectExplorer sample
                // for a lookup table example.
                byte intensity = (byte) (depth >= minDepth && depth <= maxDepth ? depth : 0);


                if (min < depth && depth < max)
                {
                    depthInRangeCount++;

                    // Write out blue byte
                    colorPixels[colorPixelIndex++] = 0;

                    // Write out green byte
                    colorPixels[colorPixelIndex++] = 0;

                    // Write out red byte
                    colorPixels[colorPixelIndex++] = intensity;
                }
                else
                {
                    // Write out blue byte
                    colorPixels[colorPixelIndex++] = intensity;

                    // Write out green byte
                    colorPixels[colorPixelIndex++] = intensity;

                    // Write out red byte
                    colorPixels[colorPixelIndex++] = intensity;
                }

                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                // If we were outputting BGRA, we would write alpha here.
                ++colorPixelIndex;
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), colorPixels,
                bitmap.PixelWidth*sizeof (int), 0);
            ImageCanvas.Background = new ImageBrush(bitmap);

            NearDepthCountValue.Content = depthInRangeCount.ToString();
        }

        private void ShowColorImageFrame(ColorImageFrame frame)
        {
            if (frame == null)
            {
                return;
            }
            var pixelData = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(pixelData);
            int stride = frame.Width*frame.BytesPerPixel;
            BitmapSource bitmapSource = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null,
                pixelData,
                stride);

            ImageCanvas.Background = new ImageBrush(bitmapSource);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.kinect == null) return;

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    kinect.ElevationAngle = (int) ElevationSlider.Value;
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void NearDepthMinDistanceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            min = Convert.ToInt32(NearDepthMinDistanceTextBox.Text);
        }

        private void NearDepthMaxDistanceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            max = Convert.ToInt16(NearDepthMaxDistanceTextBox.Text);
        }

        private void VideoRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (this.kinect == null) return;

            this.InitKinectDisplay();
        }

        private void PhotoRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (this.kinect == null) return;

            this.InitKinectDisplay();
        }

        private void DepthRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (this.kinect == null) return;

            this.InitKinectDisplay();
        }

        // COM Part

        private void LoadListButton_Click(object sender, RoutedEventArgs e)
        {
            string[] portNames = SerialPort.GetPortNames();
            COMComboBox.Items.Clear();
            foreach (string portName in portNames)
            {
                COMComboBox.Items.Add(portName);
            }
        }

        private void COMComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selectedItem = COMComboBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }
            if (UartManager != null)
            {
                UartManager.Close();
            }
            String portName = selectedItem.ToString();
            UartManager = new UartManager(portName);
            UartManager.Open();
        }


        private void TestMotorButton_Click(object sender, RoutedEventArgs e)
        {
            if (UartManager == null)
            {
                return;
            }
            UartManager.RunMotors();
        }

        private void StopMotorButton_OnClickMotorButton_Click(object sender, RoutedEventArgs e)
        {
            if (UartManager == null)
            {
                return;
            }
            UartManager.StopMotors();
        }

        private void DrawSkeletonCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            this.skeletonsModule.ReStart();
        }

        private void DrawSkeletonCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            this.skeletonsModule.ReStart();
        }

        private void GestureCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            this.skeletonsModule.ReStart();
        }

        private void GestureCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            this.skeletonsModule.ReStart();
        }
    }
}
