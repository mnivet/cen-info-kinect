﻿namespace KinectTest2
{
    using System.Windows;

    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinect;

        private IKinectModule[] kinectModules;

        public MainWindow()
        {
            this.InitializeComponent();
            this.InitializeComComponent();

            this.TrackKinectStatus();

            this.kinectModules = new IKinectModule[]
                                     {
                                         new CameraModule(this),
                                         new ElevationModule(this),
                                         new DepthModule(this),
                                         new SkeletonsModule(this),
                                         new RecognitionModule(this)
                                     };
        }

        private void TrackKinectStatus()
        {
            var kinectSensorCollection = KinectSensor.KinectSensors;
            if (kinectSensorCollection.Count > 0)
            {
                this.KinectStatusValue.Content = kinectSensorCollection[0].Status;
            }
            else
            {
                this.KinectStatusValue.Content = KinectStatus.Disconnected;
            }

            kinectSensorCollection.StatusChanged += (o, args) =>
            {
                this.KinectStatusValue.Content = args.Status;
                if (args.Status == KinectStatus.Disconnected)
                {
                    this.StopKinect();
                }
            };
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (LaunchButton.Content.ToString() == "Start")
            {
                this.StartKinect();
            }
            else
            {
                this.StopKinect();
            }
        }

        private void StartKinect()
        {
            var kinectSensorCollection = KinectSensor.KinectSensors;
            if (kinectSensorCollection.Count > 0)
            {
                this.kinect = kinectSensorCollection[0];
                this.kinect.Start();
                KinectIdValue.Content = this.kinect.DeviceConnectionId;
                LaunchButton.Content = "Stop";

                foreach (var kinectModule in this.kinectModules)
                {
                    kinectModule.Start(this.kinect);
                }
            }
        }

        private void StopKinect()
        {
            if (this.kinect != null)
            {
                foreach (var kinectModule in this.kinectModules)
                {
                    kinectModule.Stop();
                }

                this.kinect.Stop();
                this.kinect = null;
            }

            KinectIdValue.Content = "-";
            LaunchButton.Content = "Start";
        }
    }
}
