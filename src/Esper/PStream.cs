using System;
using System.IO;

namespace Esper
{
    /// <summary>
    /// Stream from pointer
    /// </summary>
    public class PStream : Stream
    {
        private readonly IntPtr _mPtr;
        private readonly int _mLength;
        private int _mPosition;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => _mLength;

        /// <inheritdoc />
        public override long Position
        {
            get => _mPosition;
            set => _mPosition = (int)value;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, Math.Min(_mLength - _mPosition, buffer.Length - offset));
            new Span<byte>((_mPtr + _mPosition).ToPointer(), count).CopyTo(buffer.AsSpan(offset, count));
            _mPosition += count;
            return count;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _mPosition = (int)offset;
                    break;
                case SeekOrigin.Current:
                    _mPosition += (int)offset;
                    break;
                case SeekOrigin.End:
                    _mPosition = _mLength + (int)offset;
                    break;
            }

            return _mPosition;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            if (Math.Min(count, Math.Min(_mLength - _mPosition, buffer.Length - offset)) < count)
                throw new IndexOutOfRangeException(
                    $"Array of length {buffer.Length}, offset {offset} and count {count} could not be used to " +
                    $"populate Memory<byte> at position {_mPosition} with length {_mLength}");
            buffer.AsSpan(offset, count).CopyTo(new Span<byte>((_mPtr + _mPosition).ToPointer(), count));
            _mPosition += count;
        }

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="ptr">Pointer</param>
        /// <param name="length">Length</param>
        public PStream(IntPtr ptr, int length)
        {
            _mPtr = ptr;
            _mLength = length;
        }
    }
}
