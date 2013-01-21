// Copyright (c) 2012-2013 fancidev
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Util.IO;
using Util.Media;

namespace QQGame
{
    /// <summary>
    /// Provides methods to decode a multi-frame MIF image from a stream.
    /// </summary>
    public class MifImage : Util.Media.MultiFrameImage
    {
        /// <summary>
        /// The MIF header.
        /// </summary>
        private MifHeader header;

        /// <summary>
        /// Contains 32-bpp ARGB pixel data of the active frame. This array
        /// is pinned in memory and is selected into the underlying bitmap.
        /// </summary>
        private int[] bmpBuffer;

        /// <summary>
        /// Pin-handle of bmpData. This handle must be released at disposal.
        /// </summary>
        private GCHandle bmpBufferHandle;

        /// <summary>
        /// Bitmap that contains the pixel data of the active frame. If all
        /// frames in the MIF image are opaque (i.e. either the image does not
        /// contain an alpha channel or all alpha values are 255), the pixel
        /// format is Format16bpp565. Otherwise, the pixel format is 
        /// Format32bppArgb.
        /// </summary>
        private Bitmap bitmap;

        /// <summary>
        /// Contains compressed data of the change from one frame to the next.
        /// If this image contains only one frame, this member is set to null.
        /// </summary>
        private MifFrameDiff[] frameDiff;

        /// <summary>
        /// Contains the frames in this MIF image. The color data and alpha
        /// data of each frame are stored either in uncompressed format or in
        /// delta-encoded format.
        /// If this image contains only one frame, this member is set to null.
        /// </summary>
        private MifFrame2[] frames;

        /// <summary>
        /// Contains the delay in milliseconds of each frame in the MIF image.
        /// </summary>
        private int[] delays;

        /// <summary>
        /// Contains uncompressed RGB and alpha channel data of the active
        /// frame. If the image contains only one frame, this member is set to
        /// null and the pixel data is stored directly in the bitmap buffer.
        /// </summary>
        // TODO: get rid of this
        private MifFrame activeFrame;

        /// <summary>
        /// Index of the active frame. The pixel data of this frame is stored
        /// in activeFrame as well as the bitmap buffer.
        /// </summary>
        private int activeIndex;

        /// <summary>
        /// Creates a MIF image from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read the image from. This 
        /// stream is automatically disposed by the constructor, whether the
        /// constructor succeeds or throws an exception. If this is not what
        /// you want, create an extra layer on top of the stream to ignore
        /// Dispose().
        /// </param>
        /// <exception cref="InvalidDataException">The stream does not
        /// contain a valid MIF image format.</exception>
        public MifImage(Stream stream)
        {
            // Create a MIF reader on the stream.
            using (stream)
            using (MifReader reader = new MifReader(stream))
            {
                // Reads and validates the MIF file header.
                this.header = reader.ReadHeader();
                if (!header.Flags.HasFlag(MifFlags.HasImage))
                    throw new InvalidDataException("The stream does not contain an image.");
                if (header.ImageWidth <= 0)
                    throw new InvalidDataException("ImageWidth field must be positive.");
                if (header.ImageHeight <= 0)
                    throw new InvalidDataException("ImageHeight field must be positive.");
                if (header.FrameCount <= 0)
                    throw new InvalidDataException("FrameCount field must be positive.");

                // TODO: avoid DoS attach if FrameCount, Width, or Height
                // are very large.

                // Read all frames at once so that we can close the stream
                // and navigate through the frames easily later.
                frames = new MifFrame2[header.FrameCount];
                frameDiff = new MifFrameDiff[header.FrameCount];
                delays = new int[header.FrameCount];
                MifFrame firstFrame = null, lastFrame = null;
                for (int i = 0; i < header.FrameCount; i++)
                {
                    // Read the next frame in uncompressed format.
                    MifFrame thisFrame = reader.ReadFrame(header, lastFrame);

                    // ----
                    frames[i] = new MifFrame2();
                    frames[i].colorCompression = MifCompressionMode.None;
                    frames[i].alphaCompression = MifCompressionMode.None;
                    frames[i].colorData = new byte[thisFrame.rgbData.Length * 2];
                    Buffer.BlockCopy(thisFrame.rgbData, 0, frames[i].colorData, 0, frames[i].colorData.Length);
                    if (thisFrame.alphaData != null)
                        frames[i].alphaData = (byte[])thisFrame.alphaData.Clone();
                    // ----

                    delays[i] = thisFrame.delay;
                    if (i == 0)
                        firstFrame = thisFrame;
                    frameDiff[i] = new MifFrameDiff();

                    // Only store the difference of this frame compared
                    // to last frame.
                    if (lastFrame != null)
                    {
                        frameDiff[i].colorDiff = new ArrayPatch<short>(
                            lastFrame.rgbData,
                            thisFrame.rgbData,
                            4, // two DWORDs
                            ArrayPatchType.Overwrite);

                        if (thisFrame.alphaData != null)
                        {
                            frameDiff[i].alphaDiff = new ArrayPatch<byte>(
                                lastFrame.alphaData,
                                thisFrame.alphaData,
                                8, // two DWORDs
                                ArrayPatchType.Overwrite);
                        }
                    }
                    lastFrame = thisFrame;
                }

                // Delta-encode the first frame from the last frame.
                if (header.FrameCount > 1)
                {
                    frameDiff[0].colorDiff = new ArrayPatch<short>(
                        lastFrame.rgbData,
                        firstFrame.rgbData,
                        8,
                        ArrayPatchType.Overwrite);
                    if (lastFrame.alphaData != null)
                    {
                        frameDiff[0].alphaDiff = new ArrayPatch<byte>(
                            lastFrame.alphaData,
                            firstFrame.alphaData,
                            8,
                            ArrayPatchType.Overwrite);
                    }
                }

                // Create a bitmap to store the converted frames. This bitmap
                // is not changed when we change frame to frame. The pixel
                // format of the bitmap is 16bpp if no alpha channel is
                // present, or 32bpp otherwise.
                bool alphaPresent = true; // frames.Any(x => x.alphaData != null);
#if false
                bitmap = new Bitmap(
                    header.ImageWidth,
                    header.ImageHeight,
                    alphaPresent ? PixelFormat.Format32bppArgb :
                                   PixelFormat.Format16bppRgb565);
#else
                bmpBuffer = new int[header.ImageWidth * header.ImageHeight];
                bmpBufferHandle = GCHandle.Alloc(bmpBuffer, GCHandleType.Pinned);
                bitmap = new Bitmap(
                    header.ImageWidth,
                    header.ImageHeight,
                    header.ImageWidth * 4,
                    PixelFormat.Format32bppArgb,
                    bmpBufferHandle.AddrOfPinnedObject());
#endif

                // Render the first frame in the bitmap buffer.
                this.activeIndex = 0;
                this.activeFrame = firstFrame;
                ConvertFrameToBitmap(
                    activeFrame.rgbData,
                    activeFrame.alphaData,
                    bmpBuffer);

                // activeColorBuffer = new MifBitmap32ColorProxy(


                // If there's only one frame, we don't need frames[] array.
                if (frameDiff.Length == 1)
                {
                    activeFrame = null;
                    frameDiff = null;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                    bitmap = null;
                    bmpBufferHandle.Free();
                    bmpBuffer = null;
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        public override int Width { get { return header.ImageWidth; } }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        public override int Height { get { return header.ImageHeight; } }

        /// <summary>
        /// Gets the number of frames in this image.
        /// </summary>
        public override int FrameCount { get { return header.FrameCount; } }

        /// <summary>
        /// Gets or sets the zero-based index of the active frame.
        /// </summary>
        public override int FrameIndex
        {
            get { return this.activeIndex; }
            set
            {
                int oldIndex = this.activeIndex;
                int newIndex = value;

                // Validate parameter.
                if (newIndex < 0 || newIndex >= this.FrameCount)
                    throw new IndexOutOfRangeException("FrameIndex out of range.");

                // Do nothing if the frame index is not changed.
                if (newIndex == oldIndex)
                    return;

                // Delta-decode the frames.
                do
                {
                    oldIndex = (oldIndex + 1) % this.FrameCount;
                    frameDiff[oldIndex].colorDiff.Apply(activeFrame.rgbData);
                    if (activeFrame.alphaData != null)
                        frameDiff[oldIndex].alphaDiff.Apply(activeFrame.alphaData);
                }
                while (oldIndex != newIndex);

                // Render the requested frame.
                this.activeIndex = newIndex;
                ConvertFrameToBitmap(
                    activeFrame.rgbData,
                    activeFrame.alphaData,
                    bmpBuffer);
            }
        }

        /// <summary>
        /// Gets the current frame of the image.
        /// </summary>
        public override Image Frame { get { return this.bitmap; } }

        /// <summary>
        /// Gets the delay of the current frame in milliseconds, or zero if 
        /// the underlying image doesn't support delay.
        /// </summary>
        public override TimeSpan FrameDelay
        {
            get { return new TimeSpan(10000L * delays[activeIndex]); }
        }

        /// <summary>
        /// Gets the sum of delays of all frames.
        /// </summary>
        public TimeSpan Duration
        {
            get { return new TimeSpan(10000L * delays.Sum()); }
        }

        public int CompressedSize
        {
            get
            {
                int size = header.ImageWidth * header.ImageHeight * 7;
                if (frameDiff != null)
                {
                    foreach (MifFrameDiff diff in frameDiff)
                    {
                        if (diff.colorDiff != null)
                        {
                            foreach (var change in diff.colorDiff.Changes)
                                size += change.Data.Length;
                        }
                        if (diff.alphaDiff != null)
                        {
                            foreach (var change in diff.alphaDiff.Changes)
                                size += change.Data.Length;
                        }
                    }
                }
                return size;
            }
        }

#if false
        /// <summary>
        /// Decodes a frame from the given stream.
        /// </summary>
        /// <param name="reader">The underlying stream.</param>
        /// <param name="header">The MifHeader.</param>
        /// <exception cref="InvalidDataException">The stream does not 
        /// contain a valid MIF image format.</exception>
        /// <remarks>The caller is responsible for disposing the returned
        /// <code>MifFrame.Image</code>.</remarks>
        private void ConvertPixelsToBitmap(byte[] rgbData, byte[] alphaData)
        {
            int width = header.ImageWidth;
            int height = header.ImageHeight;

            // If our underlying bitmap is 16bpp, we can just copy the
            // rgbData directly.
            if (bitmap.PixelFormat == PixelFormat.Format16bppRgb565)
            {
                BitmapData bmpData16 = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format16bppRgb565);
                IntPtr ptr = bmpData16.Scan0;
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(rgbData, y * width * 2, ptr, width * 2);
                    ptr += bmpData16.Stride;
                }
                bitmap.UnlockBits(bmpData16);
                return;
            }

            // Get a pointer to the underlying pixel data of the bitmap buffer.
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            IntPtr bmpPtr = bmpData.Scan0;

            // Convert the pixels scanline by scanline.
            int[] scanline = new int[width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte a = (alphaData == null) ? (byte)0x20 : alphaData[y * width + x];
                    byte b1 = rgbData[2 * (y * width + x)];
                    byte b2 = rgbData[2 * (y * width + x) + 1];

                    byte alpha = (byte)((a << 3) - (a >> 5));
                    byte red = (byte)(b2 & 0xF8);
                    byte green = (byte)(((b2 << 5) | (b1 >> 3)) & 0xFC);
                    byte blue = (byte)((b1 & 0x1F) << 3);

                    scanline[x] = (alpha << 24)
                                | (red << 16)
                                | (green << 8)
                                | (blue << 0);
                }
                Marshal.Copy(scanline, 0, bmpPtr, width);
                bmpPtr += bmpData.Stride;
            }

            // Return the bitmap.
            bitmap.UnlockBits(bmpData);
        }
#endif

        /// <summary>
        /// Converts MIF pixel format to RGB-16bpp or RGB-32bpp format.
        /// </summary>
        private static void ConvertFrameToBitmap(
            short[] colorData, byte[] alphaData, int[] bmpBuffer)
        {
            if (bmpBuffer == null)
                throw new ArgumentNullException("bmpBuffer");
            if (colorData.Length != bmpBuffer.Length)
                throw new ArgumentException("colorData and bmpBuffer must have the same length.");
            if (alphaData.Length != bmpBuffer.Length)
                throw new ArgumentException("alphaData and bmpBuffer must have the same length.");
#if false
            // If our underlying bitmap is 16bpp, we can just copy the
            // rgbData directly.
            if (bitmap.PixelFormat == PixelFormat.Format16bppRgb565)
            {
                BitmapData bmpData16 = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format16bppRgb565);
                IntPtr ptr = bmpData16.Scan0;
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(rgbData, y * width, ptr, width);
                    ptr += bmpData16.Stride;
                }
                bitmap.UnlockBits(bmpData16);
                return;
            }
#endif

            // Convert the pixels one by one.
            for (int i = 0; i < bmpBuffer.Length; i++)
            {
                byte a = (alphaData == null) ? (byte)0x20 : alphaData[i];
                short b = colorData[i];

                byte alpha = (byte)((a << 3) - (a >> 5));
                byte red = (byte)((b >> 8) & 0xF8);
                byte green = (byte)((b >> 3) & 0xFC);
                byte blue = (byte)((b << 3) & 0xF8);

                bmpBuffer[i] = (alpha << 24)
                            | (red << 16)
                            | (green << 8)
                            | (blue << 0);
            }
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
        private void ConvertPixelsToBitmap(short[] rgbData, byte[] alphaData)
        {
            int width = header.ImageWidth;
            int height = header.ImageHeight;

            // If our underlying bitmap is 16bpp, we can just copy the
            // rgbData directly.
            if (bitmap.PixelFormat == PixelFormat.Format16bppRgb565)
            {
                BitmapData bmpData16 = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format16bppRgb565);
                IntPtr ptr = bmpData16.Scan0;
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(rgbData, y * width, ptr, width);
                    ptr += bmpData16.Stride;
                }
                bitmap.UnlockBits(bmpData16);
                return;
            }

            // Convert the pixels one by one.
            for (int i = 0; i < bmpBuffer.Length; i++)
            {
                byte a = (alphaData == null) ? (byte)0x20 : alphaData[i];
                short b = rgbData[i];

                byte alpha = (byte)((a << 3) - (a >> 5));
                byte red = (byte)((b >> 8) & 0xF8);
                byte green = (byte)((b >> 3) & 0xFC);
                byte blue = (byte)((b << 3) & 0xF8);

                bmpBuffer[i] = (alpha << 24)
                            | (red << 16)
                            | (green << 8)
                            | (blue << 0);
            }
        }
    }

    /// <summary>
    /// Contains data about a frame in a MIF image.
    /// </summary>
    class MifFrame
    {
        public int delay = 0; // delay in milliseconds
        public short[] rgbData = null;   // 5-6-5 RGB data of the frame, uncompressed
        public byte[] alphaData = null; // 6-bit alpha data of the frame, uncompressed
    }

    struct IndexRange
    {
        public int BeginIndex;
        public int EndIndex;
    }

    class MifDeltaEncoding
    {
        public static IEnumerable<IndexRange> FindUnchangedRanges(
            byte[] previous, byte[] current, int threshold, int alignment)
        {
            yield break;
        }

        public static void SaveDeltaEncodedStream(
            byte[] previous, byte[] current, int threshold, int alignment,
            BinaryWriter writer)
        {
            int lastIndex = 0;
            foreach (IndexRange r in FindUnchangedRanges(previous, current, threshold, alignment))
            {
                if (lastIndex == 0)
                {
                    writer.Write(0);
                    writer.Write(r.BeginIndex - lastIndex);
                    writer.Write(current, r.BeginIndex, r.EndIndex - r.BeginIndex);
                }
            }
        }

        public static IEnumerable<IndexRange> Decode<T>(T[] data)
        {
            yield break;
        }
    }

    class MifConversion
    {
        public static void UpdateColor16(byte[] colorData, short[] bmpBuffer)
        {
            Buffer.BlockCopy(colorData, 0, bmpBuffer, 0, colorData.Length);
        }

        public static void UpdateColor16DeltaEncoded(byte[] colorData, short[] bmpBuffer)
        {
            foreach (IndexRange r in MifDeltaEncoding.Decode(colorData))
            {
            }
        }

        public static void UpdateColor32(short[] colorData, int[] bmpBuffer)
        {
            // RRRR RGGG-GGGB BBBB
            for (int i = 0; i < colorData.Length; i++)
            {
                ushort c = (ushort)colorData[i];
                bmpBuffer[i] =
                    (bmpBuffer[i] & ~0x00FFFFFF) // A
                    | ((c & 0xF800) << 8)        // R
                    | ((c & 0x07E0) << 5)        // G
                    | ((c & 0x001F) << 3);       // B
            }
        }

        /// <summary>
        /// Updates the alpha channel data of a 16-bpp bitmap buffer.
        /// </summary>
        public static void UpdateAlpha16(byte[] alphaData, short[] bmpBuffer)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Updates the alpha channel data of a 32-bpp bitmap buffer.
        /// </summary>
        public static void UpdateAlpha32(byte[] alphaData, int[] bmpBuffer)
        {
            for (int i = 0; i < alphaData.Length; i++)
            {
                bmpBuffer[i] = (bmpBuffer[i] & 0x00FFFFFF) | (alphaData[i] << 24);
            }
        }
    }

    //class 
    class MifFrame2
    {
        public MifCompressionMode colorCompression;
        public MifCompressionMode alphaCompression;
        public byte[] colorData;
        public byte[] alphaData;


        /// <summary>
        /// Draws the frame onto a pixel buffer.
        /// </summary>
        /// <param name="bmpBuffer"></param>
        public void Draw(IPixelBuffer colorBuffer, IPixelBuffer alphaBuffer)
        {
            if (colorCompression == MifCompressionMode.None)
            {
                colorBuffer.Write(0, colorData, 0, colorData.Length);
            }
            if (colorCompression == MifCompressionMode.Delta)
            {
                byte[] data = colorData;
                bool expectCopyPacket = false;
                int iInput = 0, iOutput = 0;
                while (iInput < data.Length)
                {
                    int len = BitConverter.ToInt32(data, iInput);
                    iInput += 4;
                    if (expectCopyPacket)
                    {
                        colorBuffer.Write(iOutput, data, iInput, len);
                        iInput += len;
                    }
                    iOutput += len;
                    expectCopyPacket = !expectCopyPacket;
                }
            }

            if (alphaData == null)
            {
                return;
            }
            if (alphaCompression == MifCompressionMode.None)
            {
                alphaBuffer.Write(0, alphaData, 0, alphaData.Length);
            }
            if (alphaCompression == MifCompressionMode.Delta)
            {
            }
        }
    }

    class MifUncompressedFrame
    {
    }

    class MifDeltaFrame
    {
    }

    /// <summary>
    /// Contains information about the change from one frame to the next.
    /// </summary>
    class MifFrameDiff
    {
        public ArrayPatch<short> colorDiff; // change in RGB data
        public ArrayPatch<byte> alphaDiff; // change in alpha data

        public void Apply(IPixelBuffer pixels)
        {
            // for each change in color
            // pixels.Write(pos, change, 0, change.length);
        }
    }

    /// <summary>
    /// Provides methods to read a MIF image file.
    /// </summary>
    internal class MifReader : BinaryReader
    {
        /// <summary>
        /// Decodes a buffer that is delta encoded.
        /// </summary>
        /// <param name="buffer">On input, this contains the data of the
        /// previous frame. On output, this buffer is updated with the data
        /// of the new frame.</param>
        /// <param name="delta">A stream containing the encoded difference
        /// from the previous buffer to the new buffer.</param>
        public static void DeltaDecode(byte[] buffer, BinaryReader delta)
        {
            // Read input size.
            int inputSize = delta.ReadInt32();
            if (inputSize < 0)
                throw new InvalidDataException("InputSize must be greater than or equal to zero.");

            // Read packets until InputSize number of bytes are consumed.
            int index = 0; // buffer[index] is the next byte to output
            bool expectCopyPacket = false;
            while (inputSize > 0)
            {
                if (inputSize < 4)
                    throw new InvalidDataException("Not enough bytes for packet length.");
                int len = delta.ReadInt32();
                inputSize -= 4;

                if (len < 0)
                    throw new InvalidDataException("Packet length must be greater than or equal to zero.");
                if (index + len > buffer.Length)
                    throw new InvalidDataException("Packet length must not exceed output buffer size.");

                if (expectCopyPacket) // copy packet
                {
                    if (len > inputSize)
                        throw new InvalidDataException("Packet length must not exceed input size.");
                    delta.ReadFull(buffer, index, len);
                    inputSize -= len;
                    index += len;
                }
                else // skip packet
                {
                    index += len;
                }
                expectCopyPacket = !expectCopyPacket;
            }

            // When we exit the loop, the remaining buffer is left unchanged.
        }

        /// <summary>
        /// Creates a MIF reader from an underlying stream.
        /// </summary>
        /// <param name="stream">The underlying stream.</param>
        public MifReader(Stream stream) : base(stream) { }

        /// <summary>
        /// Reads MIF file header. This method does not validate the fields
        /// of the header.
        /// </summary>
        public MifHeader ReadHeader()
        {
            MifHeader header = new MifHeader();
            header.Version = ReadInt32();
            header.ImageWidth = ReadInt32();
            header.ImageHeight = ReadInt32();
            header.Flags = (MifFlags)ReadInt32();
            header.FrameCount = ReadInt32();
            return header;
        }

        /// <summary>
        /// Reads data from the underlying stream to fill the supplied buffer.
        /// </summary>
        private void ReadRawPixelData(byte[] buffer)
        {
            this.ReadFull(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads delta-encoded data from the underlying stream into the 
        /// specified buffer.
        /// </summary>
        private void ReadDeltaEncodedPixelData(byte[] buffer)
        {
            DeltaDecode(buffer, this);
        }

        public void ReadPixelData(MifHeader header, byte[] buffer, bool isFirstFrame)
        {
            if (header.Flags.HasFlag(MifFlags.Compressed))
            {
                // Read mode.
                MifCompressionMode mode = (MifCompressionMode)ReadByte();
                switch (mode)
                {
                    case MifCompressionMode.None: // 0: not compressed
                        ReadRawPixelData(buffer);
                        break;
                    case MifCompressionMode.Delta: // 1: delta encoded
                        if (isFirstFrame)
                            throw new InvalidDataException("The first frame must not be delta encoded.");
                        ReadDeltaEncodedPixelData(buffer);
                        break;
                    default:
                        throw new NotSupportedException(string.Format(
                            "Compression mode {0} is not supported.", mode));
                }
            }
            else
            {
                ReadRawPixelData(buffer);
            }
        }

        /// <summary>
        /// Read a frame.
        /// </summary>
        public MifFrame ReadFrame(MifHeader header, MifFrame prevFrame)
        {
            MifFrame frame = new MifFrame();

            // Read Delay field if present.
            if (header.Flags.HasFlag(MifFlags.HasDelay))
            {
                frame.delay = ReadInt32();
            }

            // Read primary channels.
            byte[] rgbData = new byte[2 * header.ImageWidth * header.ImageHeight];
            if (prevFrame != null && prevFrame.rgbData != null)
            {
                Buffer.BlockCopy(prevFrame.rgbData, 0, rgbData, 0, rgbData.Length);
                ReadPixelData(header, rgbData, false);
            }
            else
            {
                ReadPixelData(header, rgbData, true);
            }
            frame.rgbData = new short[header.ImageWidth * header.ImageHeight];
            Buffer.BlockCopy(rgbData, 0, frame.rgbData, 0, rgbData.Length);

            // Read alpha channel if present.
            if (header.Flags.HasFlag(MifFlags.HasAlpha))
            {
                frame.alphaData = new byte[header.ImageWidth * header.ImageHeight];
                if (prevFrame != null && prevFrame.alphaData != null)
                {
                    prevFrame.alphaData.CopyTo(frame.alphaData, 0);
                    ReadPixelData(header, frame.alphaData, false);
                }
                else
                {
                    ReadPixelData(header, frame.alphaData, true);
                }
#if false
                // Check whether all pixels are fully opaque. This corresponds
                // to an encoded alpha value of 0x20. If so, we don't need to
                // store the alpha data at all.
                if (frame.alphaData.All(a => (a == 0x20)))
                    frame.alphaData = null;
#endif

#if false
                // Disable the following for the moment because there are not
                // so many images that are completely opaque or transparent.
                // ----------------------------------------------------------
                // Check whether all pixels are either fully opaque or fully
                // transparent. If so, we only need 1 bpp for alpha channel.
                if (frame.alphaData != null)
                // && !frame.alphaData.All(a => (a & ~0x20) == 0))
                {
                    for (int k = 0; k < frame.alphaData.Length; k++)
                    {
                        if (frame.alphaData[k] != 0 && frame.alphaData[k] != 0x20)
                        {
                            int kk = 1;
                            System.Diagnostics.Debug.WriteLine("Image is semi-transparent.");
                        }
                    }
                }
#endif
            }

            return frame;
        }
    }

    internal class MifWriter
    {
        private struct CommonSegment
        {
            public int StartIndex;
            public int Length;
        }

        /// <summary>
        /// Encodes a buffer using delta encoding.
        /// </summary>
        /// <param name="previous">On input, contains the uncompressed data
        /// of the previous buffer. On output, this buffer is filled with
        /// the encoded difference (including the leading 4-byte length
        /// indicator). The total number of bytes stored is in the return
        /// value.</param>
        /// <param name="current">Contains uncompressed data of the current
        /// buffer. This function does not alter this buffer.</param>
        /// <returns>The number of bytes stored in 'previous' after encoding,
        /// or zero if the new buffer cannot be encoded using delta encoding.
        /// This can happen if the encoding will produce a larger buffer than
        /// the uncompressed format, for example say when the buffer only 
        /// contains bytes. If the return value is zero, the new frame should
        /// be written to the file in uncompressed format.
        /// </returns>
        public static byte[] DeltaEncode(byte[] previous, byte[] current)
        {
            if (previous == null)
                throw new ArgumentNullException("previous");
            if (current == null)
                throw new ArgumentNullException("previous");
            if (previous.Length != current.Length)
                throw new ArgumentException("The buffers must have the same size.");
            if (previous.Length == 0)
                return null;

            // Let (i,j) be a locally maximal range of bytes such that 
            // previou[i..j] and current[i..j] are equal but extending i or j
            // by one will cause them to be unequal. It takes 8 bytes to
            // encode such a segment, so it makes sense if and only if
            // the common length (j - i + 1) >= 8. The number of bytes saved
            // is (j - i - 7).
            //
            // There are three special points to note (in that order):
            // 1. Since the encoded format always starts with a 4-byte length
            //    indicator, this causes a 4-byte overhead to start.
            // 2. If the last byte is part of a common segment, we only need
            //    4 bytes of overhead for that. So it makes sense if the 
            //    length of the common segment is > 4.
            // 3. Since the format spec requires that we start by encoding the
            //    length of a common segment (even if this length is zero), we
            //    should encode the first common segment whatsoever. If the
            //    length of this segment is L < 8, then there's an overhead of
            //    (8 - L) bytes.
            //
            // We proceed as follows. First, we find all the common segments
            // that have
            //   o  L >= 0 if in the beginning
            //   o  L > 8 if in the middle
            //   o  L > 4 if in the end
            // Then we compute the total savings by using delta encoding, 
            // taking into account the 4-byte overhead of the length field.
            // If the savings is positive, we encoded; if breakeven or
            // negative, we don't encode it and use uncompressed format.

            List<CommonSegment> common = new List<CommonSegment>();
            int savings = -4; // length field
            for (int index = 0; index < previous.Length; )
            {
                int L = GetCommonLength(previous, current, index);
                int threshold = (index + L == previous.Length) ? 4 : 8;
                bool include = false;
                if (index == 0) // beginning of buffer
                {
                    savings += (L - threshold);
                    include = true;
                }
                else
                {
                    if (L > threshold)
                    {
                        savings += (L - threshold);
                        include = true;
                    }
                }

                if (include)
                {
                    common.Add(new CommonSegment { StartIndex = index, Length = L });
                }
                index += L;
                index += GetDistinctLength(previous, current, index);
            }

            // Don't bother if we're only saving a few bytes.
            if (savings <= 16) // 16 is arbitrary
                return null;

            // Now write the actual encoded array.
            int totalLen = previous.Length - savings;
            byte[] result = new byte[totalLen];
            using (MemoryStream output = new MemoryStream(result))
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                writer.Write(totalLen - 4);
                int index = 0;
                foreach (CommonSegment segment in common)
                {
                    if (segment.StartIndex > 0)
                    {
                        int n = segment.StartIndex - index;
                        writer.Write(n);
                        writer.Write(current, index, n);
                    }
                    writer.Write(segment.Length);
                    index = segment.StartIndex + segment.Length;
                }
                if (index < current.Length)
                {
                    int n = current.Length - index;
                    writer.Write(n);
                    writer.Write(current, index, n);
                }
                if (output.Position != totalLen)
                    throw new Exception("Internal exception");
            }

            return result;
        }

        private static int GetCommonLength(byte[] x, byte[] y, int startIndex)
        {
            int i = startIndex;
            while (i < x.Length && x[i] == y[i])
                i++;
            return (i - startIndex);
        }

        private static int GetDistinctLength(byte[] x, byte[] y, int startIndex)
        {
            int i = startIndex;
            while (i < x.Length && x[i] != y[i])
                i++;
            return (i - startIndex);
        }
    }

    public enum MifCompressionMode
    {
        None = 0,
        Delta = 1,
    }

#if false
    /// <summary>
    /// Supports accessing the pixel data in an uncompressed MIF frame as a
    /// Stream.
    /// 
    /// The MIF format stores the RGB channels and alpha channels separately.
    /// The RGB section comes first, with 2 bytes per pixel. The optional 
    /// Alpha section follows, with 1 byte per pixel. In each section, the 
    /// pixels are stored scanline by scanline from top to bottom, and in 
    /// each scanline from left to right.
    /// </summary>
    class MifPixelStream
    {
    }
#endif

    /// <summary>
    /// Supports reading and writing the (uncompressed) RGB section of a 
    /// MIF frame. Each pixel is represented by two bytes in RGB-565 format.
    /// </summary>
    class MifColorBuffer : ArrayPixelBuffer<short>
    {
        public MifColorBuffer(short[] colorData)
            : base(colorData, PixelFormat.Format16bppRgb565) { }
    }

    /// <summary>
    /// Supports reading and writing the (uncompressed) Alpha section of a 
    /// MIF frame. Each pixel is represented by one byte with value between
    /// 0 and 32, inclusive.
    /// </summary>
    class MifAlphaBuffer : ArrayPixelBuffer<byte>
    {
        public MifAlphaBuffer(byte[] alphaData)
            : base(alphaData, PixelFormat.Format8bppIndexed) { }
    }

    /// <summary>
    /// Provides methods to read and write the RGB channels in a RGB 5-6-5
    /// bitmap.
    /// </summary>
    class MifBitmap16ColorProxy : BitmapPixelBuffer
    {
        public MifBitmap16ColorProxy(Bitmap bitmap)
            : base(bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format16bppRgb565)
                throw new NotSupportedException("Unsupported pixel format.");
        }
    }

    /// <summary>
    /// Provides methods to read and write the RGB channels in a 32-bpp ARGB
    /// bitmap as if its pixel format were RGB 5-6-5.
    /// </summary>
    class MifBitmap32ColorProxy : IPixelBuffer
    {
        Bitmap bmp;
        BitmapPixelBuffer bmpBuffer;

        public MifBitmap32ColorProxy(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Unsupported bitmap pixel format.");
            this.bmp = bitmap;
            this.bmpBuffer = new BitmapPixelBuffer(bitmap);
        }

        public void Dispose()
        {
            if (bmpBuffer != null)
            {
                bmpBuffer.Dispose();
                bmpBuffer = null;
                bmp = null;
            }
        }

        public PixelFormat PixelFormat { get { return PixelFormat.Format16bppRgb565; } }

        public int Length { get { return bmp.Width * bmp.Height * 2; } }

        public void Read(int position, byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public void Write(int position, byte[] buffer, int offset, int count)
        {
            // TODO: validate parameters.

            int iPixel = position / 2;
            int numWholePixels = (count - (position % 2)) / 2;
            byte[] argb = new byte[Math.Max(1, numWholePixels) * 4];

            // Process odd byte at the beginning: GGGBBBBB
            if (count > 0 && position % 2 != 0)
            {
                byte b = buffer[offset];
                bmpBuffer.Read(iPixel * 4, argb, 0, 4);
                argb[0] = (byte)(b << 3);
                argb[1] = (byte)((argb[1] & 0xE0) | ((b & 0xE0) >> 3));
                bmpBuffer.Write(iPixel * 4, argb, 0, 4);
                offset++;
                count--;
                iPixel++;
            }

            // Process WORD-aligned bytes.
            if (numWholePixels > 0)
            {
                bmpBuffer.Read(iPixel * 4, argb, 0, 4 * numWholePixels);
                for (int i = 0; i < numWholePixels; i++)
                {
                    short c = (short)(buffer[offset] | (buffer[offset + 1] << 8));
                    argb[4 * i + 0] = (byte)((c << 3) & 0xF8); // B
                    argb[4 * i + 1] = (byte)((c >> 3) & 0xFC); // G
                    argb[4 * i + 2] = (byte)((c >> 8) & 0xF8); // R
                    offset += 2;
                }
                bmpBuffer.Write(iPixel * 4, argb, 0, 4 * numWholePixels);
                count -= 2 * numWholePixels;
                iPixel += numWholePixels;
            }

            // Process odd byte at the end: RRRRRGGG
            if (count > 0)
            {
                byte b = buffer[offset];
                bmpBuffer.Read(iPixel * 4, argb, 0, 4);
                argb[1] = (byte)((argb[1] & 0x1F) | (b << 5)); // G
                argb[2] = (byte)(b & 0xF8); // R
                bmpBuffer.Write(iPixel * 4, argb, 0, 4);
            }
        }
    }

    /// <summary>
    /// Provides methods to read and write the Alpha channel in a 32-bpp ARGB
    /// bitmap as a continuous buffer with alpha value ranging from 0 to 32.
    /// </summary>
    class MifBitmap32AlphaProxy : IPixelBuffer
    {
        Bitmap bmp;
        BitmapPixelBuffer bmpBuffer;

        public MifBitmap32AlphaProxy(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                throw new NotSupportedException("Unsupported bitmap pixel format.");
            this.bmp = bitmap;
            this.bmpBuffer = new BitmapPixelBuffer(bitmap);
        }

        public void Dispose()
        {
            if (bmpBuffer != null)
            {
                bmpBuffer.Dispose();
                bmpBuffer = null;
                bmp = null;
            }
        }

        public PixelFormat PixelFormat { get { return PixelFormat.Format8bppIndexed; } }

        public int Length { get { return bmp.Width * bmp.Height; } }

        public void Read(int position, byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public void Write(int position, byte[] buffer, int offset, int count)
        {
            byte[] argb = new byte[count * 4];
            bmpBuffer.Read(position, argb, 0, 4 * count);
            for (int i = 0; i < count; i++)
            {
                byte a = buffer[i];
                argb[4 * i + 3] = (byte)((a >= 32) ? 255 : (a << 3)); // A
            }
            bmpBuffer.Write(position, argb, 0, 4 * count);
        }
    }

    class TestMe
    {
        public void Test()
        {
            // read uncompressed bitmap from file
            // colorData
            // alphaData
            // MifColorBuffer colorBuffer;
            // colorBuffer.Write(0, colorData, 0, colorData.Length);

            // read compressed bitmap from file
            // MifColorBuffer colorBuffer;
            // int pos = 0;
            // ...
            // pos += SkipLen;
            // colorBuffer.Write(pos, colorData, 0, CopyLen);
            // pos += CopyLen;
            // ...
            // MifAlphaBuffer alphaStream;
            // similar to above

            // render colorData and alphaData to MifPixelWriter.
        }
    }

#if false

    public class PixelBuffer
    {
    }

    public class MifPixelChunk
    {
        public int StartIndex;  // index of the pixels
        public int Count;       // number of pixels
        public int ChannelMask; // what channels are contained
        public byte[] Data;
    }

    public class MifFrameNewTest
    {
        public LinkedList<MifPixelChunk> Chunks;
    }

    /// Each pixel chunk has three members: data, repeat, mask.
    public class PixelChunk
    {
        private readonly byte[] data;
        private readonly int repeat;
        private readonly int mask;

        public PixelChunk(byte[] data, int repeat, int mask)
        {
            this.data = data;
            this.repeat = repeat;
            this.mask = mask;
        }

        /// <summary>
        /// Gets the (unrepeated) data.
        /// </summary>
        public byte[] Data { get { return data; } }

        /// <summary>
        /// Gets the number of times Data repeats.
        /// </summary>
        public int RepeatCount { get { return repeat; } }

        /// <summary>
        /// Gets a bit-mask that indicates the channels present in this chunk.
        /// The bits in Data corresponding to unset bits in the mask must be
        /// set to zero, so that an external user can simply OR the result.
        /// Examples:
        /// 0xFF000000 - contains the alpha channel only
        /// 0x00FFFFFF - contains the RGB channel only
        /// 0x00000000 - contains no data; used to skip bytes
        /// </summary>
        public int Mask { get { return mask; } }
    }

    public abstract class PixelReader
    {
        // public PixelFormat PixelFormat { get; }

        // public abstract PixelChunk ReadPixels(int count);
        public abstract int ReadPixels(int[] pixels, int startIndex, int count, out int mask);
    }

    public abstract class PixelWriter
    {
        public abstract void WritePixels(int[] pixels, int startIndex, int count, int mask);
    }

    public class BitmapPixelWriter:PixelWriter
    {
        public override void WritePixels(int[] pixels, int startIndex, int count, int mask)
        {
            // throw new NotImplementedException();
            // just copy the data is fine.
        }
    }

    public class OpaqueAlphaReader : PixelReader
    {
        // TODO: make it int.
        private static readonly byte[] OnePixel = new byte[] { 255 };

        public override PixelChunk ReadPixels(int count)
        {
            return new PixelChunk(
                data: OnePixel,
                mask: (0xFF << 24),
                repeat: count);
        }
    }

    public class MifRgbChannelReader: PixelReader
    {
        public override PixelChunk ReadPixels(int count)
        {
            // if (pixel format is 16bpp) then copy as is

            // if (pixel format is 32bpp) then decode
            for (int i = 0; i < count; i++)
            {
                buffer[startIndex + i] |= (0xFF << 24);
            }

            // if (chunk type is rle), then do a loop
            // if (chunk type is rle and mask == 0) // skip
        }
    }
#endif

#if true
    /// <summary>
    /// Provides methods to decode a multi-frame MIF image from a stream.
    /// </summary>
    public class MifImageDecoder : Util.Media.ImageDecoder
    {
        private MifReader reader;
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
                // Create a MIF reader on the stream. The stream will be
                // automatically closed when the reader is disposed.
                this.reader = new MifReader(stream);

                // Reads and validates the MIF file header.
                this.header = reader.ReadHeader();
                if ((header.Flags & MifFlags.HasImage) == 0)
                    throw new InvalidDataException("The stream does not contain an image.");
                if (header.ImageWidth <= 0)
                    throw new InvalidDataException("ImageWidth field must be positive.");
                if (header.ImageHeight <= 0)
                    throw new InvalidDataException("ImageHeight field must be positive.");
                if (header.FrameCount <= 0)
                    throw new InvalidDataException("FrameCount field must be positive.");

                // Create the internal buffer for RGB data and Alpha data.
                // We need to maintain this buffer across frames because
                // the frames may be delta-encoded, in which case the next
                // frame depend on the previous frame.
                rgbData = new byte[2 * header.ImageWidth * header.ImageHeight];
                alphaData = new byte[header.ImageWidth * header.ImageHeight];

                // TODO: avoid DoS attach if very large fields are specified
                // in Width and Height.

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
        protected override void Dispose(bool disposing)
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
            // Read Delay field if present.
            int delay;
            if ((header.Flags & MifFlags.HasDelay) != 0)
                delay = reader.ReadInt32();
            else
                delay = 0;

            // Read primary channels.
            reader.ReadPixelData(header, rgbData, false);

            // Read alpha channel if present.
            if ((header.Flags & MifFlags.HasAlpha) != 0)
                reader.ReadPixelData(header, alphaData, false);

            // Convert RGB and Alpha data to an image.
            return new Util.Media.ImageFrame(ConvertPixelsToBitmap(), delay);
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
        private Bitmap ConvertPixelsToBitmap()
        {
            int width = header.ImageWidth;
            int height = header.ImageHeight;

            // Create a bitmap and get a pointer to its pixel data.
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            IntPtr bmpPtr = bmpData.Scan0;

            // Convert the pixels scanline by scanline.
            int[] scanline = new int[width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte a = alphaData[y * width + x];
                    byte b1 = rgbData[2 * (y * width + x)];
                    byte b2 = rgbData[2 * (y * width + x) + 1];

                    byte alpha = (byte)((a << 3) - (a >> 5));
                    byte red = (byte)(b2 & 0xF8);
                    byte green = (byte)(((b2 << 5) | (b1 >> 3)) & 0xFC);
                    byte blue = (byte)((b1 & 0x1F) << 3);

                    scanline[x] = (alpha << 24)
                                | (red << 16)
                                | (green << 8)
                                | (blue << 0);
                }
                Marshal.Copy(scanline, 0, bmpPtr, width);
                bmpPtr += bmpData.Stride;
            }

            // Return the bitmap.
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private byte[] rgbData;   // 5-6-5 RGB data of current frame
        private byte[] alphaData; // 6-bit alpha data of current frame
    }
#endif

    /// <summary>
    /// Represents the file header of a MIF image.
    /// </summary>
    public class MifHeader
    {
        /// <summary>
        /// Version of this file.
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
        /// Flags of the MIF image.
        /// </summary>
        public MifFlags Flags;

        /// <summary>
        /// Number of frames in this image.
        /// </summary>
        public int FrameCount;

        /// <summary>
        /// Gets the number of bytes per frame.
        /// </summary>
        /// <exception cref="NotSupportedException">The image frames are 
        /// compressed.</exception>
        public int BytesPerFrame
        {
            get
            {
                if (Flags.HasFlag(MifFlags.Compressed))
                    throw new NotSupportedException();

                int n = 2 * ImageWidth * ImageHeight;
                if (Flags.HasFlag(MifFlags.HasAlpha))
                    n += ImageWidth * ImageHeight;
                if (Flags.HasFlag(MifFlags.HasDelay))
                    n += 4;
                return n;
            }
        }
    }

    [Flags]
    public enum MifFlags
    {
        None = 0,
        HasImage = 1,
        HasAlpha = 2,
        HasDelay = 4,
        Compressed = 8,
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
    /// Defines a decoder for MIF images.
    /// </summary>
    public class MifDecoder : IDisposable
    {
        /// <summary>
        /// Gets the number of frames in this image.
        /// </summary>
        public int FrameCount
        {
            get { return header.FrameCount; }
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
    }
}
#endif
