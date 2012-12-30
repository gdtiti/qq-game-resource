using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QQGameRes
{
    /// <summary>
    /// Represents a multi-frame MIF image.
    /// </summary>
    public class MifImage : Util.Media.ImageDecoder, IDisposable
    {
        private QQGame.MifImageDecoder decoder;
        private int frameIndex;
        private Util.Media.ImageFrame currentFrame;

        /// <summary>
        /// Loads a MIF image from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the image is loaded.</param>
        public MifImage(Stream stream)
        {
            this.decoder = new QQGame.MifImageDecoder(stream);
            this.frameIndex = -1;
            this.currentFrame = null;
        }

        /// <summary>
        /// Gets the number of frames in this image.
        /// </summary>
        public override int FrameCount
        {
            get { return decoder.FrameCount; }
        }

        /// <summary>
        /// Gets the zero-based index of the current frame, or <code>-1</code>
        /// if <code>GetNextFrame()</code> has never been called.
        /// </summary>
        public int FrameIndex
        {
            get { return frameIndex; }
        }

        /// <summary>
        /// Gets the current frame, or <code>null</code> if 
        /// <code>GetNextFrame()</code> has never been called.
        /// </summary>
        public Util.Media.ImageFrame CurrentFrame
        {
            get { return currentFrame; }
        }

        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        public int Width
        {
            get { return decoder.Width; }
        }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        public int Height
        {
            get { return decoder.Height; }
        }

        /// <summary>
        /// Reads the next frame from the underlying stream.
        /// </summary>
        /// <returns><code>true</code> if the next frame is successfully read;
        /// <code>false</code> if there are no more frames left.</returns>
        public bool GetNextFrame()
        {

            //if (reader == null)
            //    return false;

            // Return null if there are no more frames left.
            if (frameIndex + 1 >= FrameCount)
            {
                Dispose();
                return false;
            }

            currentFrame = decoder.DecodeFrame();
            frameIndex++;
            return true;
        }

        public override Util.Media.ImageFrame DecodeFrame()
        {
            return decoder.DecodeFrame();
        }

        protected override void Dispose(bool disposing)
        {
            decoder.Dispose();
        }
    }
}
