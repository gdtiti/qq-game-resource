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
    /// Represents a multi-frame MIF image.
    /// </summary>
    public class MifImage : AnimationImage, IDisposable
    {
        private BinaryReader reader;
        private MifHeader header;
        private int frameIndex;
        private AnimationFrame currentFrame;

        /// <summary>
        /// Loads a MIF image from a stream.
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

            // Set frame counter to -1 (before loading the first frame).
            frameIndex = -1;
        }

        /// <summary>
        /// Gets the number of frames in this image.
        /// </summary>
        public int FrameCount
        {
            get { return header.FrameCount; }
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
        public AnimationFrame CurrentFrame
        {
            get { return currentFrame; }
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
        /// Reads the next frame from the underlying stream.
        /// </summary>
        /// <returns><code>true</code> if the next frame is successfully read;
        /// <code>false</code> if there are no more frames left.</returns>
        public bool GetNextFrame()
        {
            if (reader == null)
                return false;

            // Return null if there are no more frames left.
            if (frameIndex + 1 >= header.FrameCount)
            {
                reader.Dispose();
                reader = null;
                return false;
            }

            // Read the next image from the stream.
            int width = header.ImageWidth;
            int height = header.ImageHeight;
            int bytesPerImage = width * height * 3;
            byte[] buffer = new byte[bytesPerImage];

            // Read frame interval if Type is 7.
            AnimationFrame frame = new AnimationFrame();
            if (header.Type == 7)
                frame.Delay = reader.ReadInt32();
            else
                frame.Delay = 0;

#if false
            if (frame.Delay != 0 && frame.Delay != 100)
                System.Diagnostics.Debug.WriteLine("Special frame delay: " + frame.Delay);
#endif

            // Load image into memory buffer.
            if (reader.Read(buffer, 0, bytesPerImage) != bytesPerImage)
                throw new IOException("Premature end of file.");

#if false
            // Count the number of colors and green bits in the bitmap.
            bool[] colorPresent = new bool[65536];
            int greenBit = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte b1 = buffer[2 * (y * width + x)];
                    byte b2 = buffer[2 * (y * width + x) + 1];
                    byte green = (byte)(((b2 << 5) | (b1 >> 3)) & 0xFC);
                    colorPresent[(b2 << 8) | b1] = true;
                    if ((green & 4) != 0)
                        greenBit++;
                }
            }

            int colorCount = 0;
            for (int i = 0; i < 65536; i++)
            {
                if (colorPresent[i])
                    ++colorCount;
            }
            System.Diagnostics.Debug.WriteLine("Number of colors: " + colorCount);
            System.Diagnostics.Debug.WriteLine("Green bit set in " + greenBit + " pixels.");
#endif

            // Create a bitmap by setting its data directly.
            byte[] bmpData = new byte[4 * width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte a = buffer[2 * width * height + y * width + x];
                    byte b1 = buffer[2 * (y * width + x)];
                    byte b2 = buffer[2 * (y * width + x) + 1];
                    byte alpha = (byte)((a >= 32) ? 255 : (a << 3));
                    byte red = (byte)(b2 & 0xF8);
                    byte green = (byte)(((b2 << 5) | (b1 >> 3)) & 0xFC);
                    byte blue = (byte)((b1 & 0x1F) << 3);

                    bmpData[4 * (y * width + x) + 0] = blue;
                    bmpData[4 * (y * width + x) + 1] = green;
                    bmpData[4 * (y * width + x) + 2] = red;
                    bmpData[4 * (y * width + x) + 3] = alpha;
                }
            }

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), 
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bmpData, 0, data.Scan0, 4 * width * height);
            bmp.UnlockBits(data);

            frame.Image = bmp;

            // Update the current frame.
            frameIndex++;
            currentFrame = frame;
            return true;
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
