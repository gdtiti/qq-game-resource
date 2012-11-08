using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

namespace QQGameRes
{
    /// <summary>
    /// Represents the header of an MIF image.
    /// </summary>
    public struct MifHeader
    {
        /// <summary>
        /// Version of this image; must be 0 or 1.
        /// </summary>
        public int Version;

        /// <summary>
        /// Width of an individual image, in pixels.
        /// </summary>
        public int ImageWidth;

        /// <summary>
        /// Height of an individual image, in pixels.
        /// </summary>
        public int ImageHeight;

        /// <summary>
        /// Type of the MIF image; 3 for single-frame, 7 for multi-frame.
        /// </summary>
        public int Type;

        /// <summary>
        /// Number of frames in this image.
        /// </summary>
        public int FrameCount;
    }

    /// <summary>
    /// Represents a frame in a multi-frame MIF image.
    /// </summary>
    public class MifFrame
    {
        /// <summary>
        /// Gets the delay, in milliseconds, before displaying the next 
        /// frame in an animated MIF image. If the MIF image contains only
        /// one frame, this value may be zero.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// Gets the image to display for the frame.
        /// </summary>
        public Image Image { get; set; }
    }

    public class MifImage : IDisposable
    {
        private BinaryReader reader;
        private MifHeader header;
        private int frameCounter;

        /// <summary>
        /// Creates an MIF image by loading from a stream.
        /// </summary>
        /// <param name="stream">The stream from which the image is loaded.</param>
        public MifImage(Stream stream)
        {
            // Create a binary reader on the stream. The stream will be closed
            // automatically when the reader is disposed.
            reader = new BinaryReader(stream);

            // Read MIF header.
            header.Version = reader.ReadInt32();
            header.ImageWidth = reader.ReadInt32();
            header.ImageHeight = reader.ReadInt32();
            header.Type = reader.ReadInt32();
            header.FrameCount = reader.ReadInt32();

            // Validate header fields.
            if (header.Version != 0 && header.Version != 1)
                throw new IOException("MIF version " + header.Version + " is not supported.");
            if (header.ImageWidth <= 0)
                throw new IOException("ImageWidth field must be positive.");
            if (header.ImageHeight <= 0)
                throw new IOException("ImageHeight field must be positive.");
            if (header.Type != 3 && header.Type != 7)
                throw new IOException("MIF type " + header.Type + " is not supported.");
            if (header.FrameCount <= 0)
                throw new IOException("FrameCount field must be positive.");

            // Set frame counter to zero.
            frameCounter = 0;
        }

        /// <summary>
        /// Gets the number of frames in this image. This value is taken from
        /// the image header, which may or may not be actual frame count.
        /// </summary>
        public int FrameCount
        {
            get { return header.FrameCount; }
        }

        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        public int Width
        {
            get { return header.ImageWidth; }
        }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        public int Height
        {
            get { return header.ImageHeight; }
        }

        /// <summary>
        /// Returns the next frame in the image.
        /// </summary>
        /// <returns>The next frame in the image, or <code>null</code> if
        /// there are no more frames left.</returns>
        public MifFrame GetNextFrame()
        {
            if (reader == null)
                throw new ObjectDisposedException("BinaryReader");

            // Return null if there are no more frames left.
            if (frameCounter >= header.FrameCount)
            {
                reader.Close();
                reader = null;
                return null;
            }
            frameCounter++;

            // Read the next image from the stream.
            int width = header.ImageWidth;
            int height = header.ImageHeight;
            int bytesPerImage = width * height * 3;
            byte[] buffer = new byte[bytesPerImage];

            // Read frame interval if Type is 7.
            MifFrame frame = new MifFrame();
            if (header.Type == 7)
                frame.Delay = reader.ReadInt32();
            else
                frame.Delay = 0;

#if DEBUG && false
            if (frame.Delay != 0 && frame.Delay != 100)
                System.Diagnostics.Debug.WriteLine("Unexpected frame delay: " + frame.Delay);
#endif

            // Load image into memory buffer.
            if (reader.Read(buffer, 0, bytesPerImage) != bytesPerImage)
                throw new IOException("Premature end of file.");

#if DEBUG
            bool[] colorPresent = new bool[65536];
            int greenBit = 0;
#endif
            // Create a bitmap and set its pixels accordingly.
            Bitmap bmp = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte a = buffer[2 * width * height + y * width + x];
                    byte b1 = buffer[2 * (y * width + x)];
                    byte b2 = buffer[2 * (y * width + x) + 1];
                    Color c = Color.FromArgb(
                        (a >= 32) ? 255 : (a << 3),     // alpha
                        b2 & 0xF8,                      // red
                        ((b2 << 5) | (b1 >> 3)) & 0xFC, // green
                        (b1 & 0x1F) << 3);              // blue
                    bmp.SetPixel(x, y, c);
#if DEBUG
                    colorPresent[(b2 << 8) | b1] = true;
                    if ((c.G & 4) != 0)
                        greenBit++;
#endif
                }
            }
#if DEBUG
            int colorCount = 0;
            for (int i = 0; i < 65536; i++)
            {
                if (colorPresent[i])
                    ++colorCount;
            }
#if false
            System.Diagnostics.Debug.WriteLine("Number of colors: " + colorCount);
            System.Diagnostics.Debug.WriteLine("Green bit set in " + greenBit + " pixels.");
#endif
#endif
            frame.Image = bmp;
            return frame;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (reader == null)
                return;
            if (disposing)
                reader.Dispose();
            reader = null;
        }
    }
}
