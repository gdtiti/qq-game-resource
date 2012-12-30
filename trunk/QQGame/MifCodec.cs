using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace QQGame
{
    /// <summary>
    /// Provides static methods to read a MIF image file.
    /// </summary>
    public static class MifCodec
    {
        /// <summary>
        /// Reads MIF header from the given stream.
        /// </summary>
        /// <param name="reader">A <code>BinaryReader</code> to read the 
        /// header from.</param>
        /// <returns>The MifHeader.</returns>
        /// <exception cref="InvalidDataException">The stream does not 
        /// contain valid format.</exception>
        /// <remarks>This method does not validate that the header fields
        /// contain valid values.</remarks>
        public static MifHeader ReadHeader(BinaryReader reader)
        {
            MifHeader header = new MifHeader();
            header.Version = reader.ReadInt32();
            header.ImageWidth = reader.ReadInt32();
            header.ImageHeight = reader.ReadInt32();
            header.Type = reader.ReadInt32();
            header.FrameCount = reader.ReadInt32();
            return header;
        }

        /// <summary>
        /// Validates the fields of the given MIF header.
        /// </summary>
        /// <param name="header">The MifHeader to validate.</param>
        /// <exception cref="InvalidDataException">The header contains invalid 
        /// fields.</exception>
        public static void ValidateHeader(MifHeader header)
        {
            if (header.Version != 0 && header.Version != 1)
                throw new InvalidDataException("MIF version " + header.Version + " is not supported.");
            if (header.ImageWidth <= 0)
                throw new InvalidDataException("ImageWidth field must be positive.");
            if (header.ImageHeight <= 0)
                throw new InvalidDataException("ImageHeight field must be positive.");
            if (header.Type != 3 && header.Type != 7)
                throw new InvalidDataException("MIF type " + header.Type + " is not supported.");
            if (header.FrameCount <= 0)
                throw new InvalidDataException("FrameCount field must be positive.");
        }

        /// <summary>
        /// Decodes a frame from the given stream.
        /// </summary>
        /// <param name="reader">The underlying stream.</param>
        /// <param name="header">The MifHeader.</param>
        /// <exception cref="InvalidDataException">The stream does not 
        /// contain a valid MIF image format.</exception>
        /// <remarks>The caller is responsible for disposing the returned
        /// <code>MifFrame.Image</code>.</remarks>
        public static Util.Media.ImageFrame DecodeFrame(BinaryReader reader, MifHeader header)
        {
            // Create a buffer to read the raw data and another buffer to
            // convert the data into bmp pixel format.
            int width = header.ImageWidth;
            int height = header.ImageHeight;
            byte[] buffer = new byte[3 * width * height];
            byte[] bmpData = new byte[4 * width * height];

            // Read frame delay if Type is 7.
            int delay;
            if (header.Type == 7)
                delay = reader.ReadInt32();
            else
                delay = 0;

            // Load frame into memory.
            if (reader.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new InvalidDataException("Premature end of file.");

            // Convert each pixel of the frame. Note that we assume that
            // Stride is equal to 4 * Width, i.e. there's no padding.
            // If we choose to loose this assumption, we will need to 
            // convert the image scan line by scan line.
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

            // Copy the converted pixel data to the bitmap.
            Bitmap bmp = new Bitmap(header.ImageWidth, header.ImageHeight,
                PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bmpData, 0, data.Scan0, 4 * width * height);
            bmp.UnlockBits(data);

            // Return the frame.
            return new Util.Media.ImageFrame(bmp, delay);
        }

#if false
        public static int GetBytesPerFrame(MifHeader header)
        {
            return 3 * header.ImageWidth * header.ImageHeight +
                ((header.Type == 7) ? 4 : 0);
        }
#endif
    }

#if true
    /// <summary>
    /// Provides methods to decode a multi-frame MIF image from a stream.
    /// </summary>
    public class MifImageDecoder : Util.Media.ImageDecoder
    {
        private BinaryReader reader;
        private MifHeader header;
#if false
        private int currentIndex;
        private MifFrame currentFrame;
#endif

        /// <summary>
        /// Creates a MIF image from the specified data stream. 
        /// </summary>
        /// <param name="stream">A stream that contains the data for this
        /// image.</param>
        /// <exception cref="InvalidDataException">The stream does not 
        /// contain a valid MIF image format.</exception>
        /// <remarks>The stream must remain open throughout the lifetime of
        /// this object, and will be automatically disposed when this object
        /// is disposed. However, it will not be disposed if the underlying
        /// reader cannot be created.</remarks>
        public MifImageDecoder(Stream stream)
        {
            try
            {
                // Create a binary reader on the stream. The stream will be
                // automatically closed when the reader is disposed.
                this.reader = new BinaryReader(stream);

                // Reads and validates the MIF file header.
                this.header = MifCodec.ReadHeader(reader);
                MifCodec.ValidateHeader(header);

                // Decode the first frame.
                //this.currentFrame = MifCodec.DecodeFrame(reader, header);
                //this.currentIndex = 0;
            }
            catch (Exception)
            {
                if (reader != null)
                    reader.Dispose();
                throw;
            }
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
        /// Gets the number of frames in this image.
        /// </summary>
        public override int FrameCount
        {
            get { return header.FrameCount; }
        }

#if false
        /// <summary>
        /// Gets or sets the zero-based index of the active frame.
        /// </summary>
        public int FrameIndex
        {
            get { return this.currentIndex; }
            set
            {
                // Validate parameter.
                if (value < 0 || value >= this.FrameCount)
                    throw new IndexOutOfRangeException("FrameIndex out of range.");

                // Do nothing if the frame index is not changed.
                if (value == this.currentIndex)
                    return;

                // Seek the underlying stream if the frame index jumps.
                if (value != this.currentIndex + 1)
                {
                    reader.BaseStream.Seek((value - (this.currentIndex + 1)) *
                        MifCodec.GetBytesPerFrame(header), SeekOrigin.Current);
                }

                // Decode the requested frame.
                this.currentFrame = MifCodec.DecodeFrame(reader, header);
                this.currentIndex = value;
            }
        }

        /// <summary>
        /// Gets the active frame of the image.
        /// </summary>
        public MifFrame ActiveFrame { get { return this.currentFrame; } }
#endif

#if false
        /// <summary>
        /// Gets the image to display for the active frame.
        /// </summary>
        public Image Image { get { return currentFrame.Image; } }

        /// <summary>
        /// Gets the delay (in milliseconds) of the active frame.
        /// </summary>
        public int Delay { get { return currentFrame.Delay; } }
#endif

        /// <summary>
        /// Disposes the image and closes the underlying stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            reader.Dispose();
        }

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

        public override Util.Media.ImageFrame DecodeFrame()
        {
            return MifCodec.DecodeFrame(reader, header);
        }
    }
#endif

    /// <summary>
    /// Represents the header of an MIF image.
    /// </summary>
    public class MifHeader
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
}

#if false
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QQGame
{
    /// <summary>
    /// Represents a frame in a MIF image.
    /// </summary>
    public class MifFrame
    {
        /// <summary>
        /// Gets or sets the image to display for the frame.
        /// </summary>
        public System.Drawing.Image Image { get; set; }

        /// <summary>
        /// Gets or sets the delay, in milliseconds, before displaying the 
        /// next frame.
        /// </summary>
        public int Delay { get; set; }
    }

    /// <summary>
    /// Defines a decoder for MIF images.
    /// </summary>
    public class MifDecoder : IDisposable
    {
        private BinaryReader reader;
        private MifHeader header;

        /// <summary>
        /// Creates a MIF image decoder from the specified data stream. 
        /// </summary>
        /// <param name="stream">A stream that contains the data for this
        /// image.</param>
        /// <exception cref="OutOfMemoryException">The stream does not 
        /// contain a valid MIF image format.</exception>
        /// <remarks>The stream must remain open throughout the lifetime of
        /// this decoder, and will be automatically closed when the decoder
        /// is disposed.</remarks>
        public MifDecoder(Stream stream)
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
                throw new OutOfMemoryException("MIF version " + header.Version + " is not supported.");
            if (header.ImageWidth <= 0)
                throw new OutOfMemoryException("ImageWidth field must be positive.");
            if (header.ImageHeight <= 0)
                throw new OutOfMemoryException("ImageHeight field must be positive.");
            if (header.Type != 3 && header.Type != 7)
                throw new OutOfMemoryException("MIF type " + header.Type + " is not supported.");
            if (header.FrameCount <= 0)
                throw new OutOfMemoryException("FrameCount field must be positive.");
        }

        /// <summary>
        /// Gets the number of frames in this image.
        /// </summary>
        public int FrameCount
        {
            get { return header.FrameCount; }
        }

        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        public int ImageWidth
        {
            get { return header.ImageWidth; }
        }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        public int ImageHeight
        {
            get { return header.ImageHeight; }
        }

        /// <summary>
        /// Decodes the next frame from the underlying stream.
        /// </summary>
        /// <returns>The frame decoded, or <code>null</code> if there are
        /// no more frames left.</returns>
        /// <exception cref="ObjectDisposedException">This decoder has been
        /// disposed.</exception>
        /// <exception cref="OutOfMemoryException">The stream does not 
        /// contain a valid MIF image format.</exception>
        /// <remarks>The caller is responsible for disposing the returned
        /// <code>System.Drawing.Image</code>.</remarks>
        public MifFrame DecodeFrame()
        {
            if (reader == null)
                throw new ObjectDisposedException("MifDecoder");

#if false
            // TODO: do we need this?
            // Return null if there are no more frames left.
            if (frameIndex + 1 >= header.FrameCount)
            {
                reader.Close();
                reader = null;
                return false;
            }
#endif

            // Read the next image from the stream.
            // Create a buffer to read the raw data and another buffer to
            // convert the data.
            int width = header.ImageWidth;
            int height = header.ImageHeight;
            byte[] buffer = new byte[3 * width * height];
            byte[] bmpData = new byte[4 * width * height];

            // Read frame delay if Type is 7.
            MifFrame frame = new MifFrame();
            if (header.Type == 7)
                frame.Delay = reader.ReadInt32();
            else
                frame.Delay = 0;

            // Load frame into memory.
            if (reader.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new OutOfMemoryException("Premature end of file.");

            // Convert each pixel of the frame. Note that we assume that
            // Stride is equal to 4 * Width, i.e. there's no padding.
            // If we choose to loose this assumption, we will need to 
            // convert the image scan line by scan line.
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

            // Create a bitmap from the converted pixel data.
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bmpData, 0, data.Scan0, 4 * width * height);
            bmp.UnlockBits(data);

            // Return this frame.
            frame.Image = bmp;
            return frame;
        }

        /// <summary>
        /// Disposes the decoder and closes the underlying stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (reader == null) // already disposed
                return;
            if (disposing)
                reader.Close();
            reader = null;
        }

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
    }

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
}
#endif
