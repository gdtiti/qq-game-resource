using System;

namespace QQGameRes
{
    /// <summary>
    /// Represents a frame in an animated image.
    /// </summary>
    public class AnimationFrame
    {
        /// <summary>
        /// Gets the delay, in milliseconds, before displaying the next 
        /// frame in an animated image.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// Gets the image to display for the frame.
        /// </summary>
        public System.Drawing.Image Image { get; set; }
    }
}
