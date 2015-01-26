namespace KinectTest2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;

    using LightBuzz.Vitruvius;

    using Microsoft.Kinect;

    public class GesturesModule
    {
        private MainWindow window;
        private KinectSensor kinect;

        private GestureController gestureController;

        public GesturesModule(MainWindow window)
        {
            this.window = window;
        }

        public void Start(KinectSensor kinect)
        {
            this.kinect = kinect;

            this.gestureController = new GestureController();
            this.gestureController.AddGesture(GestureType.SwipeLeft);
            this.gestureController.AddGesture(GestureType.SwipeRight);
            this.gestureController.AddGesture(GestureType.SwipeUp);
            this.gestureController.AddGesture(GestureType.SwipeDown);
            this.gestureController.GestureRecognized += this.GestureController_GestureRecognized;
        }

        public void Stop()
        {
            this.kinect = null;
            this.gestureController.GestureRecognized -= this.GestureController_GestureRecognized;
            this.gestureController = null;
        }

        public void Follow(Skeleton skeleton)
        {
            this.gestureController.Update(skeleton);
        }

        private void GestureController_GestureRecognized(object sender, GestureEventArgs e)
        {
            switch (e.Type)
            {
                case GestureType.SwipeLeft:
                    this.window.LeftRectangle.Visibility = Visibility.Visible;
                    break;
                case GestureType.SwipeRight:
                    this.window.RightRectangle.Visibility = Visibility.Visible;
                    break;
                case GestureType.SwipeUp:
                    this.window.LeftRectangle.Visibility = Visibility.Visible;
                    this.window.RightRectangle.Visibility = Visibility.Visible;
                    break;
                case GestureType.SwipeDown:
                    this.window.LeftRectangle.Visibility = Visibility.Hidden;
                    this.window.RightRectangle.Visibility = Visibility.Hidden;
                    break;
            }
        }
    }
}
