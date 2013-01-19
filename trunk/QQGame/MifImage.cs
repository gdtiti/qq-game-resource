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
        /// The bitmap pixel buffer. If all frames in this image are
        /// completely opaque (either the image contains no alpha channel
        /// or all alpha values are 255), the pixel format is Format16bpp565.
        /// Otherwise, the pixel format is Format32bppArgb.
        /// </summary>
        private Bitmap bitmap;

        /// <summary>
        /// Contains compressed data of each frame in the MIF image. If this
        /// image contains only one frame, this member is set to null and
        /// the pixel data is stored directly in the bitmap buffer.
        /// </summary>
        private MifFrame[] frames;

        /// <summary>
        /// Contains the delay of each frame in the MIF image.
        /// </summary>
        private int[] delays;

        /// <summary>
        /// Contains data of the active frame. The RGB and alpha data are 
        /// always stored in uncompressed format. This member may point to
        /// an element in the frame[] array if that frame is stored 
        /// uncompressed. If this image contains only one frame, this member
        /// is set to null and the pixel data is stored directly in the bitmap
        /// buffer.
        /// </summary>
        private MifFrame currentFrame;

        /// <summary>
        /// Index of the active frame. The pixel data of this frame is stored
        /// in currentFrame as well as the bitmap buffer.
        /// </summary>
        private int currentIndex;

        // test
        private int compressedSize;

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
                frames = new MifFrame[header.FrameCount];
                delays = new int[header.FrameCount];
                currentFrame = null;
                for (int i = 0; i < header.FrameCount; i++)
                {
                    frames[i] = reader.ReadFrame(header, currentFrame);
                    delays[i] = frames[i].delay;

                    // Test delta encoding.
                    if (i > 0)
                    {
                        byte[] deltaEncoded = MifWriter.DeltaEncode(
                            currentFrame.rgbData,
                            frames[i].rgbData);
                        compressedSize += (deltaEncoded != null) ?
                            deltaEncoded.Length : frames[i].rgbData.Length;
                        if (frames[i].alphaData != null)
                        {
                            if (currentFrame.alphaData != null)
                            {
                                deltaEncoded = MifWriter.DeltaEncode(
                                    currentFrame.alphaData,
                                    frames[i].alphaData);
                                compressedSize += (deltaEncoded != null) ?
                                    deltaEncoded.Length : frames[i].alphaData.Length;
                            }
                            else
                            {
                                compressedSize += frames[i].alphaData.Length;
                            }
                        }
                    }
                    else
                    {
                        compressedSize += frames[i].rgbData.Length;
                        if (frames[i].alphaData != null)
                            compressedSize += frames[i].alphaData.Length;
                    }

                    currentFrame = frames[i];
                }

                // Create a bitmap to store the converted frames. This bitmap
                // is not changed when we change frame to frame. The pixel
                // format of the bitmap is 16bpp if no alpha channel is
                // present, or 32bpp otherwise.
                bool alphaPresent = frames.Any(x => x.alphaData != null);
                bitmap = new Bitmap(
                    header.ImageWidth,
                    header.ImageHeight,
                    alphaPresent ? PixelFormat.Format32bppArgb : 
                                   PixelFormat.Format16bppRgb565);

                // Render the first frame in the bitmap buffer.
                this.currentIndex = 0;
                this.currentFrame = frames[0];
                ConvertPixelsToBitmap(
                    currentFrame.rgbData,
                    currentFrame.alphaData);

                // If there's only one frame, we don't need frames[] array.
                if (frames.Length == 1)
                {
                    currentFrame = null;
                    frames = null;
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
            get { return this.currentIndex; }
            set
            {
                // Validate parameter.
                if (value < 0 || value >= this.FrameCount)
                    throw new IndexOutOfRangeException("FrameIndex out of range.");

                // Do nothing if the frame index is not changed.
                if (value == this.currentIndex)
                    return;

                // Convert the format of the requested frame.
                this.currentIndex = value;
                this.currentFrame = frames[this.currentIndex];
                ConvertPixelsToBitmap(
                    currentFrame.rgbData,
                    currentFrame.alphaData);
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
            get { return new TimeSpan(10000L * delays[currentIndex]); }
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
            get { return compressedSize; }
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
    }

    /// <summary>
    /// Contains data about a frame in a MIF image.
    /// </summary>
    internal class MifFrame
    {
        public int delay = 0; // delay in milliseconds
        public byte[] rgbData = null;   // 5-6-5 RGB data of the frame, uncompressed
        public byte[] alphaData = null; // 6-bit alpha data of the frame, uncompressed
    }

    /// <summary>
    /// Provides methods to read a MIF image file.
    /// </summary>
    internal class MifReader : BinaryReader
    {
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
        /// Decodes a buffer that is delta encoded.
        /// </summary>
        /// <param name="output">On input, this contains the uncompressed 
        /// data of the previous frame. On output, this buffer is filled
        /// with the new data decoded.</param>
        /// <param name="input">The encoded difference between the previous
        /// buffer to the new buffer.</param>
        public static void DeltaDecode(byte[] output, byte[] input)
        {
            int iInput = 0, iOutput = 0;

            // Read and verify length field.
            int inputSize = BitConverter.ToInt32(input, iInput);
            iInput += 4;
            if (inputSize != input.Length - 4)
                throw new InvalidDataException("InputSize mismatch.");

            // Read and decode packets until the input is consumed.
            while (inputSize > 0)
            {
                if (inputSize < 4)
                    throw new InvalidDataException("Cannot read SkipLen field.");
                int skipLen = BitConverter.ToInt32(input, iInput);
                iInput += 4;
                inputSize -= 4;

                if (skipLen < 0)
                    throw new InvalidDataException("SkipLen must be greater than or equal to zero.");
                if (iOutput + skipLen > output.Length)
                    throw new InvalidDataException("SkipLen must not exceed the output buffer size.");
                iOutput += skipLen;

                if (inputSize == 0)
                    break;
                if (inputSize < 4)
                    throw new InvalidDataException("Cannot read CopyLen field.");
                int copyLen = BitConverter.ToInt32(input, iInput);
                iInput += 4;
                inputSize -= 4;

                if (copyLen < 0)
                    throw new InvalidDataException("CopyLen must be greater than or equal to zero.");
                if (iOutput + copyLen > output.Length)
                    throw new InvalidDataException("CopyLen must not exceed the output buffer size.");
                if (copyLen > inputSize)
                    throw new InvalidDataException("CopyLen must not exceed the input buffer size.");

                Array.Copy(input, iInput, output, iOutput, copyLen);
                iInput += copyLen;
                inputSize -= copyLen;
                iOutput += copyLen;
            }

            // When we exit the loop, the remaining buffer is left unchanged.
        }

        /// <summary>
        /// Reads delta-encoded data from the underlying stream into the 
        /// specified buffer.
        /// </summary>
        private void ReadDeltaEncodedPixelData(byte[] buffer)
        {
            // Read input size.
            int inputSize = ReadInt32();
            if (inputSize < 0)
                throw new InvalidDataException();

            // Read packets until InputSize number of bytes are consumed.
            // When we exit the loop, the remaining buffer is left unchanged.
            int index = 0;
            while (inputSize > 0)
            {
                if (inputSize < 4)
                    throw new InvalidDataException("Cannot read SkipLen field.");
                int skipLen = ReadInt32();
                inputSize -= 4;
                if (skipLen < 0)
                    throw new InvalidDataException("SkipLen must be greater than or equal to zero.");

                index += skipLen;
                if (index > buffer.Length)
                    throw new InvalidDataException("SkipLen must not exceed the output buffer size.");

                if (inputSize == 0)
                    break;
                if (inputSize < 4)
                    throw new InvalidDataException("Cannot read CopyLen field.");
                int copyLen = ReadInt32();
                inputSize -= 4;
                if (copyLen < 0)
                    throw new InvalidDataException("CopyLen must be greater than or equal to zero.");
                if (copyLen > inputSize)
                    throw new InvalidDataException("CopyLen must not exceed the input buffer size.");
                if (index + copyLen > buffer.Length)
                    throw new InvalidDataException("CopyLen must not exceed the output buffer size.");

                this.ReadFull(buffer, index, copyLen);
                index += copyLen;
                inputSize -= copyLen;
            }
        }

        public void ReadPixelData(MifHeader header, byte[] buffer, byte[] prevBuffer)
        {
            if (header.Flags.HasFlag(MifFlags.Compressed))
            {
                // Read mode.
                MifCompressionMode mode =(MifCompressionMode) ReadByte();
                switch (mode)
                {
                    case MifCompressionMode.None: // 0: not compressed
                        ReadRawPixelData(buffer);
                        break;
                    case MifCompressionMode.Delta: // 1: delta encoded
                        if (prevBuffer == null)
                            throw new InvalidDataException("The first frame must not be delta encoded.");
                        prevBuffer.CopyTo(buffer, 0);
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
            frame.rgbData = new byte[2 * header.ImageWidth * header.ImageHeight];
            ReadPixelData(header, frame.rgbData,
                prevFrame == null ? null : prevFrame.rgbData);

            // Read alpha channel if present.
            if (header.Flags.HasFlag(MifFlags.HasAlpha))
            {
                frame.alphaData = new byte[header.ImageWidth * header.ImageHeight];
                ReadPixelData(header, frame.alphaData,
                    prevFrame == null ? null : prevFrame.alphaData);

                // Check whether all pixels are fully opaque. This corresponds
                // to an encoded alpha value of 0x20. If so, we don't need to
                // store the alpha data at all.
                if (frame.alphaData.All(a => (a == 0x20)))
                    frame.alphaData = null;

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
            reader.ReadPixelData(header, rgbData, rgbData);

            // Read alpha channel if present.
            if ((header.Flags & MifFlags.HasAlpha) != 0)
                reader.ReadPixelData(header, alphaData, alphaData);

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
