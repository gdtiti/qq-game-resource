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
using System.IO.Compression;

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
        /// Pin-handle of bmpBuffer. This handle must be freed when the 
        /// MifImage object is disposed.
        /// </summary>
        private GCHandle bmpBufferHandle;

        /// <summary>
        /// Bitmap of the active frame. If all frames in the MIF image are
        /// opaque (i.e. either the image does not contain an alpha channel
        /// or all alpha values are 255), the pixel format is Format16bpp565.
        /// Otherwise, the pixel format is Format32bppArgb.
        /// </summary>
        private Bitmap bitmap;

        /// <summary>
        /// Pixel stream that encapsulates the RGB channel of the underlying
        /// bitmap as RGB-565 format.
        /// </summary>
        private PixelStream colorStream;

        /// <summary>
        /// Pixel stream that encapsulates the alpha channel of the underying
        /// bitmap as 6-bit.
        /// </summary>
        private PixelStream alphaStream;

        /// <summary>
        /// Contains compressed data of the change from one frame to the next.
        /// If this image contains only one frame, this member is set to null.
        /// </summary>
        private MifFrameDiff[] frameDiff;

        /// <summary>
        /// Contains the delay in milliseconds of each frame in the MIF image.
        /// </summary>
        private int[] delays;

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
#if false
            byte[] test = new byte[] { 1, 3, 5, 5, 4, 6, 7, 9, 7, 7, 7, 5, 7, 8 };
            byte[] aa = MifRunLengthEncoding.Encode(test, 1);
            byte[] bb = MifRunLengthEncoding.Decode(aa, 1);
#endif
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
                frameDiff = new MifFrameDiff[header.FrameCount];
                delays = new int[header.FrameCount];
                MifFrame firstFrame = null, prevFrame = null;
                bool alphaPresent = false;
                for (int i = 0; i < header.FrameCount; i++)
                {
                    // Read the next frame in uncompressed format.
                    MifFrame thisFrame = reader.ReadFrame(header, prevFrame);
                    delays[i] = thisFrame.delay;
                    if (i == 0)
                        firstFrame = thisFrame;

                    // Check whether this frame contains non-opaque alpha.
                    if (!alphaPresent && thisFrame.alphaData != null)
                    {
                        alphaPresent = thisFrame.alphaData.Any(a => (a < 0x20));
                    }

                    // Store the difference of thisFrame from prevFrame using
                    // delta encoding.
                    if (i > 0)
                    {
                        frameDiff[i] = new MifFrameDiff(prevFrame, thisFrame);
                    }
                    prevFrame = thisFrame;
                }

                // Delta-encode the first frame from the last frame.
                if (header.FrameCount > 1)
                {
                    frameDiff[0] = new MifFrameDiff(prevFrame, firstFrame);
                }

                // If no frame contains a non-opaque alpha, then we don't need
                // to store the alpha channel at all. In this case, the bitmap
                // is 16 bpp. Otherwise, we create a 32-bpp bitmap.
#if true
                if (!alphaPresent)
                {
                    int stride = (header.ImageWidth * 2 + 3) / 4 * 4;
                    bmpBuffer = new int[stride / 4 * header.ImageHeight];
                    bmpBufferHandle = GCHandle.Alloc(bmpBuffer, GCHandleType.Pinned);
                    bitmap = new Bitmap(
                        header.ImageWidth,
                        header.ImageHeight,
                        stride,
                        PixelFormat.Format16bppRgb565,
                        bmpBufferHandle.AddrOfPinnedObject());
                    colorStream = new ArrayPixelStream<int>(
                        bmpBuffer,
                        PixelFormat.Format16bppRgb565,
                        stride,
                        header.ImageWidth * 2);
                    alphaStream = null;
                }
                else // 32-bpp with alpha channel
#endif
                {
                    bmpBuffer = new int[header.ImageWidth * header.ImageHeight];
                    bmpBufferHandle = GCHandle.Alloc(bmpBuffer, GCHandleType.Pinned);
                    bitmap = new Bitmap(
                        header.ImageWidth,
                        header.ImageHeight,
                        header.ImageWidth * 4,
                        PixelFormat.Format32bppArgb,
                        bmpBufferHandle.AddrOfPinnedObject());
                    colorStream = new MifColorStream32(bmpBuffer);
                    alphaStream = new MifAlphaStream32(bmpBuffer);
                }

                // Render the first frame in the bitmap buffer.
                this.activeIndex = 0;
                firstFrame.UpdateBitmap(colorStream, alphaStream);

                // If there's only one frame, we don't need frames[] array.
                if (frameDiff.Length == 1)
                {
                    frameDiff = null;
                }

                // If there's no alpha, we don't need to store alpha diff,
                // although this won't really save much memory as we're
                // run-length encoded.
                if (!alphaPresent && frameDiff != null)
                {
                    foreach (MifFrameDiff f in frameDiff)
                        f.RemoveAlpha();
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

                // Delta-decode the frames. This directly updates the
                // underlying bitmap buffer.
                do
                {
                    oldIndex = (oldIndex + 1) % this.FrameCount;
                    frameDiff[oldIndex].UpdateBitmap(colorStream, alphaStream);
                }
                while (oldIndex != newIndex);

                this.activeIndex = newIndex;
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
                int size = bmpBuffer.Length * 4;
                if (frameDiff != null)
                {
                    size += frameDiff.Select(f => f.CompressedSize).Sum();
                }
                return size;
            }
        }

#if false
        /// <summary>
        /// Converts 32-bpp ARGB format to MIF pixel format.
        /// </summary>
        private static void ConvertBitmapToFrame(
            int[] bmpBuffer, byte[] colorData, byte[] alphaData)
        {
            if (bmpBuffer == null)
                throw new ArgumentNullException("bmpBuffer");
            if (colorData != null && colorData.Length != bmpBuffer.Length * 2)
                throw new ArgumentException("colorData and bmpBuffer must have the same length.");
            if (alphaData != null && alphaData.Length != bmpBuffer.Length)
                throw new ArgumentException("alphaData and bmpBuffer must have the same length.");

            // Convert the pixels one by one.
            for (int i = 0; i < bmpBuffer.Length; i++)
            {
                if (colorData != null)
                {
                    // RRRR-RGGG|GGGB-BBBB
                    byte r = (byte)(bmpBuffer[i] >> 16);
                    byte g = (byte)(bmpBuffer[i] >> 8);
                    byte b = (byte)(bmpBuffer[i] >> 0);
                    int c = ((r & 0xF8) << 8)
                          | ((g & 0xFC) << 3)
                          | (b >> 3);
                    colorData[2 * i] = (byte)c;
                    colorData[2 * i + 1] = (byte)(c >> 8);
                }
                if (alphaData != null)
                {
                    byte a = (byte)(bmpBuffer[i] >> 24);
                    alphaData[i] = (byte)(((a << 7) + a) >> 10);
                }
            }
        }
#endif
    }

    /// <summary>
    /// Contains data about a frame in a MIF image.
    /// </summary>
    class MifFrame
    {
        public int delay = 0; // delay in milliseconds
        public byte[] colorData; // uncompressed 5-6-5 RGB data of the frame
        public byte[] alphaData; // uncompressed 6-bit alpha data of the frame

        public void UpdateBitmap(PixelStream colorStream, PixelStream alphaStream)
        {
            if (colorStream != null && colorData != null)
            {
                colorStream.Position = 0;
                colorStream.Write(colorData, 0, colorData.Length);
            }
            if (alphaStream != null && alphaData != null)
            {
                alphaStream.Position = 0;
                alphaStream.Write(alphaData, 0, alphaData.Length);
            }
        }
    }
    
    /// <summary>
    /// Contains information about the change from one frame to the next.
    /// </summary>
    public class MifFrameDiff
    {
        public static int memoryUsed1 = 0;
        public static int memoryUsed2 = 0;

        private byte[] colorDiff; // delta-encoded change in color, then RL-encoded
        private byte[] alphaDiff; // delta-encoded change in alpha, then RL-encoded

        /// <summary>
        /// Creates a FrameDiff object as the difference between two frames.
        /// </summary>
        /// <param name="oldFrame"></param>
        /// <param name="newFrame"></param>
        internal MifFrameDiff(MifFrame oldFrame, MifFrame newFrame)
        {
            if (oldFrame == null)
                throw new ArgumentNullException("oldFrame");
            if (newFrame == null)
                throw new ArgumentNullException("newFrame");

            if (oldFrame.colorData != null && newFrame.colorData != null)
            {
                this.colorDiff = MifDeltaEncoding.Encode(
                    oldFrame.colorData, newFrame.colorData, true);
                memoryUsed1 += colorDiff.Length;
                this.colorDiff = MifRunLengthEncoding.Encode(colorDiff, 2);
                memoryUsed2 += colorDiff.Length;
            }
            if (oldFrame.alphaData != null && newFrame.alphaData != null)
            {
                this.alphaDiff = MifDeltaEncoding.Encode(
                    oldFrame.alphaData, newFrame.alphaData, true);
                memoryUsed1 += alphaDiff.Length;
                this.alphaDiff = MifRunLengthEncoding.Encode(alphaDiff, 1);
                memoryUsed2 += alphaDiff.Length;
            }
        }

        public void RemoveAlpha()
        {
            this.alphaDiff = null;
        }

        internal void UpdateFrame(MifFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");

            if (frame.colorData != null && this.colorDiff != null)
            {
                byte[] actualColorDiff = MifRunLengthEncoding.Decode(this.colorDiff, 2);
                MifDeltaEncoding.Decode(frame.colorData, actualColorDiff, true);
            }
            if (frame.alphaData != null && this.alphaDiff != null)
            {
                byte[] actualAlphaDiff = MifRunLengthEncoding.Decode(this.alphaDiff, 1);
                MifDeltaEncoding.Decode(frame.alphaData, actualAlphaDiff, true);
            }
        }

        internal void UpdateBitmap(PixelStream colorStream, PixelStream alphaStream)
        {
            if (colorStream != null && this.colorDiff != null)
            {
                byte[] actualColorDiff = MifRunLengthEncoding.Decode(this.colorDiff, 2);
                foreach (var change in MifDeltaEncoding.GetChanges(actualColorDiff, true))
                {
                    colorStream.Seek(change.StartIndex, SeekOrigin.Begin);
                    colorStream.Write(change.NewData, change.NewDataOffset, change.Length);
                }
            }
            if (alphaStream != null && this.alphaDiff != null)
            {
                byte[] actualAlphaDiff = MifRunLengthEncoding.Decode(this.alphaDiff, 1);
                foreach (var change in MifDeltaEncoding.GetChanges(actualAlphaDiff, true))
                {
                    alphaStream.Seek(change.StartIndex, SeekOrigin.Begin);
                    alphaStream.Write(change.NewData, change.NewDataOffset, change.Length);
                }
            }
        }

        public int CompressedSize
        {
            get
            {
                return (colorDiff == null ? 0 : colorDiff.Length)
                     + (alphaDiff == null ? 0 : alphaDiff.Length);
            }
        }
    }

    class MifDeltaEncoding
    {
        public class DeltaChange
        {
            public int StartIndex;
            public int Length;
            public byte[] NewData;
            public int NewDataOffset;
        }

        public static IEnumerable<DeltaChange> GetChanges(byte[] delta, bool use7Bit)
        {
            if (!use7Bit)
                throw new NotSupportedException();

            int index = 0;
            bool expectCopyPacket = false;
            for (int i = 0; i < delta.Length; )
            {
                int len = SevenBitIntegerEncoding.Read(delta, ref i);
                if (len < 0)
                    throw new InvalidDataException("Packet length must be greater than or equal to zero.");

                if (expectCopyPacket) // copy from delta
                {
                    yield return new DeltaChange {
                        StartIndex = index,
                        Length = len,
                        NewData = delta,
                        NewDataOffset = i
                    };
                    i += len;
                }
                index += len;
                expectCopyPacket = !expectCopyPacket;
            }
        }

        /// <summary>
        /// Decodes a stream encoded by MifDeltaEncoding.Encode().
        /// </summary>
        /// <param name="oldData">Old data.</param>
        /// <param name="oldIndex">Start index of old data.</param>
        /// <param name="newData">New data. This may be the same as OldData.
        /// </param>
        /// <param name="newIndex">Start index of the new data. If OldData
        /// and NewData are the same, then newIndex should be >= oldIndex
        /// for the method to work properly.</param>
        /// <param name="delta">A reader containing the encoded difference
        /// from the old buffer to the new buffer.</param>
        public static void Decode(
            byte[] oldData, int oldIndex,
            byte[] newData, int newIndex,
            BinaryReader delta)
        {
            bool expectCopyPacket = false;
            while (delta.BaseStream.Position < delta.BaseStream.Length)
            {
                int len = delta.ReadInt32();
                if (len < 0)
                    throw new InvalidDataException("Packet length must be greater than or equal to zero.");
                if (oldIndex + len > oldData.Length)
                    throw new InvalidDataException("Packet length must not exceed old buffer.");
                if (newIndex + len > newData.Length)
                    throw new InvalidDataException("Packet length must not exceed new buffer.");

                if (expectCopyPacket) // copy from delta
                {
                    delta.ReadFull(newData, newIndex, len);
                }
                else // copy from old data
                {
                    if (!(oldData == newData && oldIndex == newIndex))
                    {
                        // We MUST copy byte by byte from the beginning; this
                        // allows RunLengthEncoding to work correctly. Do not
                        // use Array.Copy() or Buffer.BlockCopy().
                        for (int i = 0; i < len; i++)
                            newData[newIndex + i] = oldData[oldIndex + i];
                    }
                }
                oldIndex += len;
                newIndex += len;
                expectCopyPacket = !expectCopyPacket;
            }

            // When we exit the loop, the remaining buffer is left unchanged.
        }

        /// <summary>
        /// Decodes a buffer that is delta encoded.
        /// </summary>
        /// <param name="buffer">On input, this contains the data of the
        /// previous frame. On output, this buffer is updated with the data
        /// of the new frame.</param>
        /// <param name="delta">A stream containing the encoded difference
        /// from the previous buffer to the new buffer.</param>
        public static void Decode(byte[] buffer, BinaryReader delta)
        {
            Decode(buffer, 0, buffer, 0, delta);
        }

        public static void Decode(byte[] oldData, byte[] delta, bool use7Bit)
        {
            using (var input = new MemoryStream(delta))
            using (var reader = use7Bit ?
                   new BinaryReaderWith7BitEncoding(input) :
                   new BinaryReader(input))
            {
                Decode(oldData, reader);
            }
        }

        public static byte[] Encode(byte[] oldData, byte[] newData, bool use7Bit)
        {
            using (var output = new MemoryStream())
            using (var writer = use7Bit ?
                   new BinaryWriterWith7BitEncoding(output) :
                   new BinaryWriter(output))
            {
                Encode(oldData, 0, newData, 0, newData.Length, writer, use7Bit ? 2 : 8);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Encodes the difference between the previous contents and the 
        /// current contents of a byte buffer. The encoded format can be
        /// written directly to a MIF file.
        /// </summary>
        /// <param name="oldData">Contains the data in the previous buffer.
        /// </param>
        /// <param name="newData">Contains the data of the current buffer.
        /// </param>
        /// <param name="output">A stream to write the encoded difference.
        /// </param>
        /// <param name="encodeInteger">Whether to encode integer as 7-bit.
        /// </param>
        /// <remarks>
        /// The encoding algorithm is described below.
        /// 
        /// Let (i,j) be a locally maximal range of bytes such that 
        /// previous[i..j] = current[i..j] but extending either i or j by one
        /// will cause them to be unequal. It takes 8 bytes to encode such a
        /// segment, so it makes sense if and only if the common length 
        /// (j - i + 1) > 8. We use this as the criteria to determine whether
        /// a new chunk should be encoded.
        /// 
        /// Note that the encoded difference ALWAYS starts with a common
        /// chunk, even if its length is zero.
        ///</remarks>
        public static void Encode(
            byte[] oldData, int oldOffset,
            byte[] newData, int newOffset,
            int length, BinaryWriter writer, int threshold)
        {
            if (oldData == null || newData == null || writer == null)
                throw new ArgumentNullException();
            if (oldOffset < 0 || newOffset < 0 || length < 0)
                throw new ArgumentOutOfRangeException();
            if (oldOffset + length > oldData.Length ||
                newOffset + length > newData.Length)
                throw new ArgumentOutOfRangeException();

            int lastCommonEnd = 0; // end-index of the previous common chunk
            int index = 0;         // begin-index of the current common chunk
            while (index < length)
            {
                int L = GetCommonLength(
                    oldData, oldOffset + index,
                    newData, newOffset + index,
                    length - index);

                // Encode this common block if the block length is greater
                // than the threshold, or if we are at the very beginning of
                // the buffer, in which case the format requires a common
                // block.
                if (index == 0 || L > threshold)
                {
                    // Encode last distinct chunk.
                    if (index > lastCommonEnd)
                    {
                        writer.Write(index - lastCommonEnd);
                        writer.Write(newData, newOffset + lastCommonEnd, index - lastCommonEnd);
                    }

                    // Encode current common chunk.
                    writer.Write(L);
                    lastCommonEnd = index + L;
                }
                index += L;
                index += GetDistinctLength(
                    oldData, oldOffset + index,
                    newData, newOffset + index,
                    length - index);
            }

            // Encode last distinct chunk.
            if (index > lastCommonEnd)
            {
                writer.Write(index - lastCommonEnd);
                writer.Write(newData, newOffset + lastCommonEnd, index - lastCommonEnd);
            }
        }

        private static int GetCommonLength(
            byte[] x, int index1,
            byte[] y, int index2,
            int count)
        {
            int i;
            for (i = 0; i < count && x[index1 + i] == y[index2 + i]; i++) ;
            return i;
        }

        private static int GetDistinctLength(
            byte[] x, int index1,
            byte[] y, int index2,
            int count)
        {
            int i;
            for (i = 0; i < count && x[index1 + i] != y[index2 + i]; i++) ;
            return i;
        }
    }

    static class SevenBitIntegerEncoding
    {
        public static int Read(byte[] buffer, ref int index)
        {
            int result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = buffer[index++];
                result |= ((b & 0x7F) << shift);
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return result;
        }
    }

    class BinaryWriterWith7BitEncoding : BinaryWriter
    {
        public BinaryWriterWith7BitEncoding(Stream stream)
            : base(stream) { }

        public override void Write(int value)
        {
            base.Write7BitEncodedInt(value);
        }
    }

    class BinaryReaderWith7BitEncoding : BinaryReader
    {
        public BinaryReaderWith7BitEncoding(Stream stream)
            : base(stream) { }

        public override int ReadInt32()
        {
            return base.Read7BitEncodedInt();
        }
    }

    class MifRunLengthEncoding
    {
        /// <summary>
        /// We build run-length encoder on top of delta encoder.
        /// 
        /// Let a = [a0, a2, ..., a9] be a byte array. To run-length encode
        /// it with a lookahead value of 2, (suitable for Int16), we shift
        /// the array by 2 bytes:
        /// 
        ///   a0  a1  a2  a3  a4  a5  a6  a7  a8  a9  
        ///           a0  a1  a2  a3  a4  a5  a6  a7  a8  a9
        /// 
        /// We first write the length of the byte array to the output.
        /// Next we write a0, a1 to the output. Then we delta-encode
        ///    [a2 .. a9] := newData
        ///    [a0 .. a7] := oldData
        ///    
        /// This works as long as the encoder moves from begin to end.
        /// </summary>
        /// <returns></returns>
        public static void Encode(
            byte[] data, int offset, int count,
            int lookahead, BinaryWriter writer, int threshold)
        {
            if (data == null || writer == null)
                throw new ArgumentNullException();
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException();
            if (offset + count > data.Length)
                throw new ArgumentOutOfRangeException();
            if (lookahead <= 0)
                throw new ArgumentException("lookback must be greater than or equal to 1.");

            // Output array length.
            writer.Write((int)data.Length);

            // Output 'lookahead' number of bytes as is.
            writer.Write(data, offset, lookahead);

            // TODO: what if count <= lookahead?
            // Encode the remaining data using delta encoding.
            MifDeltaEncoding.Encode(
                data, offset, data, offset + lookahead,
                count - lookahead - offset, writer, threshold);
        }

        public static byte[] Encode(byte[] data, int offset, int count, int lookahead)
        {
            using (MemoryStream output = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriterWith7BitEncoding(output))
            {
                Encode(data, offset, count, lookahead, writer, 2);
                return output.ToArray();
            }
        }

        public static byte[] Encode(byte[] data, int lookahead)
        {
            return Encode(data, 0, data.Length, lookahead);
        }

        public static byte[] Decode(byte[] data, int lookahead)
        {
            using (MemoryStream input = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReaderWith7BitEncoding(input))
            {
                // Read length field.
                int len = reader.ReadInt32();
                byte[] result = new byte[len];

                // Read first 'lookahead' elements.
                reader.ReadFull(result, 0, lookahead);

                // Delta-decode the remaining.
                MifDeltaEncoding.Decode(result, 0, result, lookahead, reader);
                return result;
            }
        }
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
        /// Reads delta-encoded data from the underlying stream into the 
        /// specified buffer.
        /// </summary>
        private void ReadDeltaEncodedPixelData(byte[] buffer)
        {
            // Read input size.
            int inputSize = ReadInt32();
            if (inputSize < 0)
                throw new InvalidDataException("InputSize must be greater than or equal to zero.");

            // Create a stream view to cover the range.
            using (StreamView view = new StreamView(
                this.BaseStream, this.BaseStream.Position, inputSize))
            using (BinaryReader reader = new BinaryReader(view))
            {
                MifDeltaEncoding.Decode(buffer, reader);
            }
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
            frame.colorData = new byte[2 * header.ImageWidth * header.ImageHeight];
            if (prevFrame != null && prevFrame.colorData != null)
            {
                prevFrame.colorData.CopyTo(frame.colorData, 0);
                ReadPixelData(header, frame.colorData, false);
            }
            else
            {
                ReadPixelData(header, frame.colorData, true);
            }

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
            }

            return frame;
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
    /// Provides methods to read and write the RGB channels in a 32-bpp ARGB
    /// pixel buffer as if it were in RGB 565 pixel format.
    /// </summary>
    class MifColorStream32 : PixelStream
    {
        int[] pixels;
        int position; // byte position as if this were 2 bytes per pixel

        public MifColorStream32(int[] pixels)
        {
            if (pixels == null)
                throw new ArgumentNullException("pixels");

            this.pixels = pixels;
            this.position = 0;
        }

        public override PixelFormat PixelFormat
        {
            get { return PixelFormat.Format16bppRgb565; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            if (this.Position + count > this.Length)
                throw new EndOfStreamException();

            // Since we may start writing in the middle of a pixel, we need
            // to split the data to write into three parts:
            // 1. an odd byte at the beginning
            // 2. WORD-aligned bytes in the middle
            // 3. an odd byte at the end

            int newPosition = position + count;
            int iPixel = position / 2;
            int numWholePixels = (count - (position % 2)) / 2;

            // Process odd byte at the beginning. This is the second byte in
            // an RGB-565 pixel, which, in little endian, is the high byte.
            //          RRRRR    GGG
            // AAAAAAAA RRRRRRRR GGGGGGGG BBBBBBBB
            if (count > 0 && position % 2 != 0)
            {
                byte b = buffer[offset];
                int c = pixels[iPixel];
                pixels[iPixel] = (c & ~0x00F8E000)
                               | ((b & 0xF8) << 16)  // RRRRR
                               | ((b & 0x07) << 13); // GGG
                offset++;
                count--;
                iPixel++;
            }

            // Process WORD-aligned bytes.
            //          RRRRR    GGG GGG   BBBBB
            // AAAAAAAA RRRRRRRR GGG-GGGGG BBBBBBBB
            for (int i = 0; i < numWholePixels; i++)
            {
                ushort b = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
                int c = pixels[ iPixel ];
                pixels[iPixel] = (c & ~0x00FFFFFF)
                               | ((b & 0xF800) << 8)  // RRRRR
                               | ((b & 0x07E0) << 5)  // GGGGGG
                               | ((b & 0x001F) << 3); // BBBBB
                offset += 2;
                count -= 2;
                iPixel++;
            }

            // Process odd byte at the end. This is the first byte in an
            // RGB-565 pixel, which, in little endian, is the low byte.
            //                      GGG   BBBBB
            // AAAAAAAA RRRRRRRR GGGGGGGG BBBBBBBB
            if (count > 0)
            {
                byte b = buffer[offset];
                int c = pixels[iPixel];
                pixels[iPixel] = (c & ~0x1CFF)
                               | ((b & 0xE0) << 5)  // GGG
                               | ((b & 0x1F) << 3); // BBBBB
                offset++;
                count--;
                iPixel++;
            }

            // Update position.
            position = newPosition;
        }

        public override long Position
        {
            get { return position; }
            set
            {
                if (value < 0 || value > this.Length)
                    throw new ArgumentOutOfRangeException("value");
                this.position = (int)value;
            }
        }

        public override long Length
        {
            get { return 2L * pixels.Length; }
        }
    }

    /// <summary>
    /// Provides methods to read and write the Alpha channel in a 32-bpp ARGB
    /// pixel buffer as if it had only 6 bits of alpha.
    /// </summary>
    class MifAlphaStream32 : PixelStream
    {
        int[] pixels;
        int position; // byte position, which is equal to pixel index

        public MifAlphaStream32(int[] pixels)
        {
            if (pixels == null)
                throw new ArgumentNullException("pixels");

            this.pixels = pixels;
            this.position = 0;
        }

        public override PixelFormat PixelFormat
        {
            get { return PixelFormat.Format8bppIndexed; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");
            if (this.Position + count > this.Length)
                throw new EndOfStreamException();

            for (int i = 0; i < count; i++)
            {
                byte a = buffer[offset + i];
                int c = pixels[position + i];
                pixels[position + i] = (c & 0x00FFFFFF)
                                     | (((a >= 32) ? 255 : (a << 3)) << 24);
            }
            position += count;
        }

        public override long Position
        {
            get { return position; }
            set
            {
                if (value < 0 || value > this.Length)
                    throw new ArgumentOutOfRangeException("value");
                this.position = (int)value;
            }
        }

        public override long Length
        {
            get { return pixels.Length; }
        }
    }

#if false
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
#endif
    
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

        /// <summary>
        /// Disposes the image and closes the underlying stream.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            reader.Dispose();
        }

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
