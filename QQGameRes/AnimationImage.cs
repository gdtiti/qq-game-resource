using System;
using System.Collections.Generic;

namespace QQGameRes
{
#if false
    /// <summary>
    /// Represents a multi-frame image which can be animated.
    /// </summary>
    public interface AnimationImage
    {
        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the number of frames in this image.
        /// </summary>
        int FrameCount { get; }

        /// <summary>
        /// Gets the zero-based index of the current frame, or <code>-1</code>
        /// if <code>GetNextFrame()</code> has never been called.
        /// </summary>
        int FrameIndex { get; }

        /// <summary>
        /// Gets the current frame, or <code>null</code> if 
        /// <code>GetNextFrame()</code> has never been called.
        /// </summary>
        Util.Media.ImageFrame CurrentFrame { get; }

        /// <summary>
        /// Reads the next frame from the underlying stream.
        /// </summary>
        /// <returns><code>true</code> if the next frame is successfully read;
        /// <code>false</code> if there are no more frames left.</returns>
        bool GetNextFrame();

        // IEnumerable<AnimationFrame> GetFrames();
    }
#endif
}
