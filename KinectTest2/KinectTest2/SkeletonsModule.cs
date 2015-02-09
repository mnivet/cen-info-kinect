﻿namespace KinectTest2
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media;

    using KinectTest2.SkeletonModules;

    using Microsoft.Kinect;

    public class SkeletonsModule
    {
        private MainWindow window;

        private KinectSensor kinect;

        private Dictionary<int, TrackedSkeleton> trackedSkeletons;

        private int trackNumber;

        public SkeletonsModule(MainWindow window)
        {
            this.window = window;
        }

        public void Start(KinectSensor kinect)
        {
            this.kinect = kinect;
            this.trackedSkeletons = new Dictionary<int, TrackedSkeleton>();
            this.trackNumber = 0;

            this.kinect.SkeletonStream.Enable();
            this.kinect.SkeletonFrameReady += kinect_SkeletonFrameReady;
        }

        public void Stop()
        {
            this.kinect.SkeletonFrameReady -= kinect_SkeletonFrameReady;
            this.kinect.SkeletonStream.Disable();

            this.kinect = null;
        }

        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            // kinect variable can be null is the event is raised while we are stopping the device
            if (this.kinect == null) return;

            using (var sFrame = e.OpenSkeletonFrame())
            {
                if (sFrame != null)
                {
                    var skeletons = new Skeleton[sFrame.SkeletonArrayLength];
                    sFrame.CopySkeletonDataTo(skeletons);
                    for (int i = 0; i < skeletons.Length; i++)
                    {
                        var skeleton = skeletons[i];
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.Follow(skeleton, i);
                        }
                    }

                    var noMoreTrackedSkeletons = this.trackedSkeletons.Values.Where(p => p.TrackNumber != this.trackNumber).ToList();
                    foreach (var noMoreTrackedSkeleton in noMoreTrackedSkeletons)
                    {
                        this.StopFollow(noMoreTrackedSkeleton);
                    }

                    this.trackNumber++;
                }
            }
        }

        private void Follow(Skeleton skeleton, int skeletonIndex)
        {
            TrackedSkeleton trakedSkeleton;
            if (!this.trackedSkeletons.TryGetValue(skeleton.TrackingId, out trakedSkeleton))
            {
                // start follow a new skeleton
                trakedSkeleton = new TrackedSkeleton();
                trakedSkeleton.SkeletonId = skeleton.TrackingId;
                trakedSkeleton.SkeletonModules = this.CreateSkeletonModulesFor(skeleton, skeletonIndex).ToArray();
                this.trackedSkeletons.Add(skeleton.TrackingId, trakedSkeleton);
            }

            foreach (var skeletonModule in trakedSkeleton.SkeletonModules)
            {
                skeletonModule.Follow(skeleton, skeletonIndex);
            }

            // update track number to notify that the skeleton has been followed during the curent frame
            trakedSkeleton.TrackNumber = this.trackNumber;
        }

        private void StopFollow(TrackedSkeleton trackedSkeleton)
        {
            foreach (var skeletonModule in trackedSkeleton.SkeletonModules)
            {
                    skeletonModule.Dispose();
            }

            this.trackedSkeletons.Remove(trackedSkeleton.SkeletonId);
        }

        private IEnumerable<ISkeletonModule> CreateSkeletonModulesFor(Skeleton skeleton, int skeletonIndex)
        {
            yield return new HeadTrackingModule(this.kinect, this.window, skeleton.TrackingId, GetSkeletonColor(skeletonIndex));
            yield return new GesturesModule(this.kinect, this.window);
        }

        private Brush GetSkeletonColor(int skeletonIndex)
        {
            switch (skeletonIndex)
            {
                case 0: return Brushes.Black;
                case 1: return Brushes.DarkViolet;
                case 2: return Brushes.Yellow;
                case 3: return Brushes.Blue;
                case 4: return Brushes.GreenYellow;
                case 5: return Brushes.Red;
                default: return Brushes.White;
            }
        }
    }
}