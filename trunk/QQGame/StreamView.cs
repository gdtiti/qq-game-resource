using System;
using System.IO;

namespace Util.IO
{
    /// <summary>
    /// Represents a viewport of an underlying stream. The underlying stream
    /// must support telling its seek pointer via the Position property.
    /// </summary>
    public class StreamView : Stream
    {
        private Stream stream;
        private long viewOffset;
        private long viewLength;

        public StreamView(Stream baseStream, long viewOffset, long viewLength)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");
            if (viewOffset < 0)
                throw new ArgumentException("viewOffset must be greater than or equal to zero.");
            if (viewLength < 0)
                throw new ArgumentException("viewLength must be greater than or equal to zero.");

            if (viewOffset != baseStream.Position)
                baseStream.Seek(viewOffset, SeekOrigin.Begin);

            this.stream = baseStream;
            this.viewOffset = viewOffset;
            this.viewLength = viewLength;
        }

        public override bool CanRead { get { return stream.CanRead; } }

        public override bool CanSeek { get { return stream.CanSeek; } }

        public override bool CanWrite { get { return stream.CanWrite; } }

        public override void Flush() { stream.Flush(); }

        public override long Length { get { return viewLength; } }

        public override long Position
        {
            get { return stream.Position - viewOffset; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Position must be greater than or equal to zero.");
                stream.Position = viewOffset + value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long pos;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    pos = this.viewOffset;
                    break;
                case SeekOrigin.Current:
                    pos = this.viewOffset + this.Position;
                    break;
                case SeekOrigin.End:
                    pos = this.viewOffset + this.viewLength;
                    break;
                default:
                    throw new ArgumentException("Invalid value in origin.");
            }

            pos += offset;
            if (pos < this.viewOffset)
                throw new ArgumentException("Cannot seek past the beginning of the stream.");

            return stream.Seek(pos, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.Position >= this.viewLength) // seek pointer outside viewport
                return 0;
            count = Math.Min(count, (int)(this.viewLength - this.Position));
            return stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < 0)
                throw new ArgumentException("count must be greater than or equal to zero.");
            if (this.Position + count >= this.viewLength)
                throw new IOException("Cannot write beyond the end of the stream.");

            stream.Write(buffer, offset, count);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            // do nothing
        }
    }
}
