using System;
using System.IO;

namespace Esper
{
    /// <summary>
    /// Provides cut view into stream
    /// </summary>
    public class CutStream : Stream
    {
        /// <summary>
        /// Reposition before every read operation
        /// </summary>
        public bool Reposition { get; set; }

        private Stream _stream;
        private long _ofs;
        private long _len;
        private long _cOfs;

        /// <summary>
        /// Create default instance
        /// </summary>
        public CutStream()
        {
            Set(Null, 0, 0);
        }

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="stream">Stream to cut</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        public CutStream(Stream stream, long offset, long length)
        {
            Set(stream, offset, length);
        }

        /// <summary>
        /// Set source for this stream
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="offset">Source stream offset</param>
        /// <param name="length">Source stream length</param>
        public void Set(Stream stream, long offset, long length)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!Reposition && _stream.CanSeek)
                _stream.Position = offset;
            _ofs = offset;
            _len = length;
            _cOfs = _ofs;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Reposition)
                _stream.Position = _stream.CanSeek ? _cOfs : throw new ApplicationException();
            count = (int)Math.Min(count, _len + _ofs - _cOfs);
            int c = _stream.Read(buffer, offset, count);
            _cOfs += c;
            return c;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) =>
            origin switch
            {
                SeekOrigin.Begin => ((_cOfs = _stream.Seek(_ofs + offset, origin)) - _ofs),
                SeekOrigin.Current => ((_cOfs = _stream.Seek(offset, origin)) - _ofs),
                SeekOrigin.End => ((_cOfs = _stream.Seek(_ofs + _len - _stream.Length + offset, origin)) - _ofs),
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
            };

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _stream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => _len;

        /// <inheritdoc />
        public override long Position
        {
            get => _cOfs - _ofs;
            set => _cOfs = _ofs + value;
        }
    }
}
