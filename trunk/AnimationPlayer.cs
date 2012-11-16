using System;
using System.ComponentModel;

namespace QQGameRes
{
    /// <summary>
    /// Provides support to play an AnimationImage.
    /// </summary>
    public class AnimationPlayer : Component
    {
        private int minDelay;
        private int endDelay;
        private AnimationImage image;
        private object tag;
        private System.Windows.Forms.Timer timer;

        /// <summary>
        /// Creates an AnimationPlayer.
        /// </summary>
        public AnimationPlayer()
        {
        }

        /// <summary>
        /// Creates an AnimationPlayer and add it to the given container.
        /// </summary>
        public AnimationPlayer(IContainer container)
        {
            container.Add(this);
        }

        /// <summary>
        /// Gets or sets the minimum delay of each frame, in milliseconds.
        /// </summary>
        [DefaultValue(0)]
        [Description("The minimum delay of each frame in milliseconds.")]
        public int MinDelay
        {
            get { return minDelay; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("MinDelay must be non-negative.");
                minDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the extra delay of the last frame, in milliseconds.
        /// </summary>
        [DefaultValue(0)]
        [Description("The extra delay of the last frame in milliseconds.")]
        public int EndDelay
        {
            get { return endDelay; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("EndDelay must be non-negative.");
                endDelay = value;
            }
        }

        /// <summary>
        /// Gets the image being animated. If no animation is in progress,
        /// returns <code>null</code>.
        /// </summary>
        [Browsable(false)]
        public AnimationImage Image
        {
            get { return image; }
        }

        /// <summary>
        /// Gets the user-defined data associated with the current animation.
        /// If no animation is in progress, returns <code>null</code>.
        /// </summary>
        [Browsable(false)]
        public object Tag
        {
            get { return tag; }
        }

        /// <summary>
        /// Gets the current frame that should be rendered. If no animation
        /// is in progress, returns <code>null</code>.
        /// </summary>
        [Browsable(false)]
        public AnimationFrame CurrentFrame
        {
            get
            {
                if (image != null)
                    return image.CurrentFrame;
                else
                    return null;
            }
        }

        /// <summary>
        /// Occurs when a new frame should be rendered.
        /// </summary>
        [Description("Occurs when a new frame should be rendered.")]
        //public event EventHandler<AnimationPlayerEventArgs> UpdateFrame;
        public event EventHandler UpdateFrame;

        /// <summary>
        /// Occurs after an animation is ended.
        /// </summary>
        [Description("Occurs after an animation is ended.")]
        //public event EventHandler<AnimationPlayerEventArgs> AnimationEnded;
        public event EventHandler AnimationEnded;

        /// <summary>
        /// Starts animating an image, optionally associated with a piece of
        /// user-defined data. If an animation is already in progress, it is
        /// stopped first.
        /// <param name="image">The image to be animated.</param>
        /// <param name="tag">User-defined data to associate with this
        /// animation.</param>
        /// </summary>
        public void StartAnimation(AnimationImage image, object tag)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            // Stop any existing animation.
            StopAnimation();

            // Start a new animation. We create a new Timer object for each
            // animation, so that if we get a WM_TIMER message after the timer
            // is stopped, we can discard the message. This is guaranteed as
            // long as the WM_TIMER message handler is called in the same 
            // thread where StopAnimation() is called.
            this.image = image;
            this.tag = tag;
            this.timer = new System.Windows.Forms.Timer();
            this.timer.Tick += new EventHandler(this.timer_Tick);
            PlayNextFrame();
        }

        /// <summary>
        /// Stops the current animation, if any.
        /// </summary>
        public void StopAnimation()
        {
            // An animation is uniquely associated with a timer. To stop the
            // animation, we stop and dispose the timer associated with it.
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;

                // Raise the AnimationEnded event.
                if (AnimationEnded != null)
                    AnimationEnded(this, null);

                // TODO: maybe it's logically clearer if we move the image and
                // tag into EventArgs.
                image = null;
                tag = null;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            // Discard any timer event sent by a stopped timer.
            if (sender == timer)
            {
                PlayNextFrame();
            }
        }

        private void PlayNextFrame()
        {
            // Try load the next frame in the image being animated. If there
            // are no more frames, stop the animation.
            if (!image.GetNextFrame())
            {
                StopAnimation();
                return;
            }

            // Schedule the timer for the next frame. If this is the last
            // frame, delay an extra 500 milliseconds before ending the
            // animation.
            int delay = Math.Max(image.CurrentFrame.Delay,
                                 Math.Max(1, this.MinDelay));
            if (image.FrameIndex == image.FrameCount - 1)
                delay += this.EndDelay;
            this.timer.Interval = delay;
            this.timer.Start();

            // Raise the UpdateFrame event.
            if (UpdateFrame != null)
            {
                //var args = new AnimationPlayerEventArgs();
                //args.Image = this.image;
                //args.CurrentFrame = this.image.CurrentFrame;
                UpdateFrame(this, null);
                //UpdateFrame(this, args);
            }
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        /// <param name="disposing"><code>true</code> if this component 
        /// should be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null)
            {
                timer.Dispose();
            }
        }
    }

    public class AnimationPlayerEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the image being animated.
        /// </summary>
        public AnimationImage Image;

        /// <summary>
        /// Gets or sets the current frame being animated.
        /// </summary>
        public AnimationFrame CurrentFrame;
    }
}
