using System;
using System.IO;

namespace Esper
{
    /// <summary>
    /// Stream from <see cref="Memory{T}"/> of bytes
    /// </summary>
    public class MStream : Stream
    {
        private readonly ReadOnlyMemory<byte> _memory;
        private readonly Memory<byte> _writeMemory;
        private readonly bool _canWrite;
        private readonly int _length;
        private int _position;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => _canWrite;

        /// <inheritdoc />
        public override long Length => _length;

        /// <inheritdoc />
        public override long Position
        {
            get => _position;
            set => _position = (int)value;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int ReadByte() => _position >= _length ? -1 : _memory.Span[_position++];

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            int count1 = _length - _position, count2 = buffer.Length - offset;
            if (count1 < 0) throw new EndOfStreamException();
            if (count2 < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            count = Math.Min(count, Math.Min(count1, count2));
            _memory.Span.Slice(_position, count).CopyTo(buffer.AsSpan(offset, count));
            _position += count;
            return count;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = (int)offset;
                    break;
                case SeekOrigin.Current:
                    _position += (int)offset;
                    break;
                case SeekOrigin.End:
                    _position = _length + (int)offset;
                    break;
            }

            return _position;
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
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_canWrite) throw new NotSupportedException("Cannot write on read-only stream");
            if (Math.Min(count, Math.Min(_length - _position, buffer.Length - offset)) < count)
                throw new IndexOutOfRangeException(
                    $"Array of length {buffer.Length}, offset {offset} and count {count} could not be used to " +
                    $"populate Memory<byte> at position {_position} with length {_length}");
            buffer.AsSpan(offset, count).CopyTo(_writeMemory.Span.Slice(_position, count));
            _position += count;
        }

        /// <summary>
        /// Create new instance of <see cref="MStream"/>
        /// </summary>
        /// <param name="memory">Writeable memory instance</param>
        public MStream(Memory<byte> memory)
        {
            _writeMemory = memory;
            _memory = memory;
            _canWrite = true;
            _length = memory.Length;
        }

        /// <summary>
        /// Create new instance of <see cref="MStream"/>
        /// </summary>
        /// <param name="memory">Memory instance</param>
        public MStream(ReadOnlyMemory<byte> memory)
        {
            _writeMemory = Memory<byte>.Empty;
            _memory = memory;
            _canWrite = false;
            _length = memory.Length;
        }

        internal ReadOnlyMemory<byte> GetMemory() => _memory;
        internal Memory<byte> GetWriteableMemory() => _writeMemory;
    }
}
