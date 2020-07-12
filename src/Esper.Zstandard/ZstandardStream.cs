using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Interop = Esper.Zstandard.ZstandardInterop;

namespace Esper.Zstandard
{
    /// <summary>
    /// Provides methods and properties for compressing and decompressing streams by using the Zstandard algorithm.
    /// </summary>
    public class ZstandardStream : Stream
    {
        /// <summary>
        /// Stream this instance wraps
        /// </summary>
        public Stream Stream { get; set; }

        private readonly CompressionMode _mode;
        private readonly bool _leaveOpen;
        private bool _isClosed;
        private bool _isDisposed;
        private bool _isInitialized;

        private IntPtr _zstream;
        private readonly uint _zstreamInputSize;
        private readonly uint _zstreamOutputSize;

        private byte[] _data;
        private bool _dataDepleted;
        private bool _dataSkipRead;
        private int _dataPosition;
        private int _dataSize;

        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardStream"/> class by using the specified stream and compression mode, and optionally leaves the stream open.
        /// </summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
        /// <param name="leaveOpen">true to leave the stream open after disposing the <see cref="ZstandardStream"/> object; otherwise, false.</param>
        public ZstandardStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _mode = mode;
            _leaveOpen = leaveOpen;

            switch (mode)
            {
                case CompressionMode.Compress:
                    _zstreamInputSize = Interop.ZSTD_CStreamInSize().ToUInt32();
                    _zstreamOutputSize = Interop.ZSTD_CStreamOutSize().ToUInt32();
                    _zstream = Interop.ZSTD_createCStream();
                    _data = _arrayPool.Rent((int)_zstreamOutputSize);
                    break;
                case CompressionMode.Decompress:
                    _zstreamInputSize = Interop.ZSTD_DStreamInSize().ToUInt32();
                    _zstreamOutputSize = Interop.ZSTD_DStreamOutSize().ToUInt32();
                    _zstream = Interop.ZSTD_createDStream();
                    _data = _arrayPool.Rent((int)_zstreamInputSize);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardStream"/> class  by using the specified stream and compression level, and optionally leaves the stream open.
        /// </summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="compressionLevel">The compression level.</param>
        /// <param name="leaveOpen">true to leave the stream open after disposing the <see cref="ZstandardStream"/> object; otherwise, false.</param>
        public ZstandardStream(Stream stream, int compressionLevel, bool leaveOpen = false) : this(stream,
            CompressionMode.Compress, leaveOpen)
        {
            CompressionLevel = compressionLevel;
        }

        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------

        /// <summary>
        /// The version of the native Zstd library.
        /// </summary>
        public static Version Version
        {
            get
            {
                int version = (int)Interop.ZSTD_versionNumber();
                return new Version(version / 10000 % 100, version / 100 % 100, version % 100);
            }
        }

        /// <summary>
        /// The maximum compression level supported by the native Zstd library.
        /// </summary>
        public static int MaxCompressionLevel => Interop.ZSTD_maxCLevel();

        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the compression level to use, the default is 6.
        /// </summary>
        /// <remarks>
        /// To get the maximum compression level see <see cref="MaxCompressionLevel"/>.
        /// </remarks>
        public int CompressionLevel { get; set; } = 6;

        /// <summary>
        /// Gets or sets the compression dictionary tp use, the default is null.
        /// </summary>
        /// <value>
        /// The compression dictionary.
        /// </value>
        public ZstandardDictionary CompressionDictionary { get; set; }

        /// <summary>
        /// Gets whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => Stream.CanRead && _mode == CompressionMode.Decompress;

        /// <summary>
        ///  Gets whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => Stream.CanWrite && _mode == CompressionMode.Compress;

        /// <summary>
        ///  Gets whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------


        /// <summary>
        /// Reset internal state
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
            _dataPosition = 0;
            _dataSize = 0;
            _dataDepleted = false;
            _dataSkipRead = false;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_isDisposed) return;
            if (!_isClosed) ReleaseResources(false);
            _arrayPool.Return(_data);
            _isDisposed = true;
            _data = null;
        }

        /// <inheritdoc />
        public override void Close()
        {
            if (_isClosed) return;

            try
            {
                ReleaseResources(true);
            }
            finally
            {
                _isClosed = true;
                base.Close();
            }
        }

        private unsafe void ReleaseResources(bool flushStream)
        {
            switch (_mode)
            {
                case CompressionMode.Compress:
                    try
                    {
                        if (!flushStream) return;
                        ProcessStream((zcs, buffer) =>
                        {
                            Interop.ThrowIfError(Interop.ZSTD_flushStream(zcs, &buffer));
                            return buffer;
                        });
                        ProcessStream((zcs, buffer) =>
                        {
                            Interop.ThrowIfError(Interop.ZSTD_endStream(zcs, &buffer));
                            return buffer;
                        });
                        Stream.Flush();
                    }
                    finally
                    {
                        Interop.ZSTD_freeCStream(_zstream);
                        if (!_leaveOpen) Stream.Close();
                    }

                    break;
                case CompressionMode.Decompress:
                {
                    Interop.ZSTD_freeDStream(_zstream);
                    if (!_leaveOpen) Stream.Close();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CompressionDictionary?.Dispose();
        }

        /// <inheritdoc />
        public override unsafe void Flush()
        {
            if (_mode != CompressionMode.Compress) return;
            ProcessStream((zcs, buffer) =>
            {
                Interop.ThrowIfError(Interop.ZSTD_flushStream(zcs, &buffer));
                return buffer;
            });
            Stream.Flush();
        }

        /// <inheritdoc />
        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            if (CanRead == false) throw new NotSupportedException();

            // prevent the buffers from being moved around by the garbage collector
            var alloc1 = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var alloc2 = GCHandle.Alloc(_data, GCHandleType.Pinned);

            try
            {
                int length = 0;

                if (_isInitialized == false)
                {
                    _isInitialized = true;

                    if (CompressionDictionary == null)
                        Interop.ZSTD_initDStream(_zstream);
                    else
                        Interop.ZSTD_initDStream_usingDDict(_zstream,
                            CompressionDictionary.GetDecompressionDictionary());
                }

                while (count > 0)
                {
                    int inputSize = _dataSize - _dataPosition;

                    // read data from input stream 
                    if (inputSize <= 0 && !_dataDepleted && !_dataSkipRead)
                    {
                        _dataSize = Stream.Read(_data, 0, (int)_zstreamInputSize);
                        _dataDepleted = _dataSize <= 0;
                        _dataPosition = 0;
                        inputSize = _dataDepleted ? 0 : _dataSize;

                        // skip stream.Read until the internal buffer is depleted
                        // avoids a Read timeout for applications that know the exact number of bytes in the stream
                        _dataSkipRead = true;
                    }

                    // configure the inputBuffer
                    Interop.Buffer inputBuffer;
                    inputBuffer.Data = inputSize <= 0
                        ? IntPtr.Zero
                        : Marshal.UnsafeAddrOfPinnedArrayElement(_data, _dataPosition);
                    inputBuffer.Size = inputSize <= 0 ? UIntPtr.Zero : new UIntPtr((uint)inputSize);
                    inputBuffer.Position = UIntPtr.Zero;

                    // configure the outputBuffer
                    Interop.Buffer outputBuffer;
                    outputBuffer.Data = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
                    outputBuffer.Size = new UIntPtr((uint)count);
                    outputBuffer.Position = UIntPtr.Zero;

                    // decompress inputBuffer to outputBuffer
                    Interop.ThrowIfError(Interop.ZSTD_decompressStream(_zstream, &outputBuffer, &inputBuffer));

                    // calculate progress in outputBuffer
                    int outputBufferPosition = (int)outputBuffer.Position.ToUInt32();
                    if (outputBufferPosition == 0)
                    {
                        // the internal buffer is depleted, we're either done
                        if (_dataDepleted) break;

                        // or we need more bytes
                        _dataSkipRead = false;
                    }

                    length += outputBufferPosition;
                    offset += outputBufferPosition;
                    count -= outputBufferPosition;

                    // calculate progress in inputBuffer
                    int inputBufferPosition = (int)inputBuffer.Position.ToUInt32();
                    _dataPosition += inputBufferPosition;
                }

                return length;
            }
            finally
            {
                alloc1.Free();
                alloc2.Free();
            }
        }

        /// <inheritdoc />
        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            if (CanWrite == false) throw new NotSupportedException();

            // prevent the buffers from being moved around by the garbage collector
            var alloc1 = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var alloc2 = GCHandle.Alloc(_data, GCHandleType.Pinned);

            try
            {
                if (_isInitialized == false)
                {
                    _isInitialized = true;

                    var result = CompressionDictionary == null
                        ? Interop.ZSTD_initCStream(_zstream, CompressionLevel)
                        : Interop.ZSTD_initCStream_usingCDict(_zstream,
                            CompressionDictionary.GetCompressionDictionary(CompressionLevel));

                    Interop.ThrowIfError(result);
                }

                while (count > 0)
                {
                    uint inputSize = Math.Min((uint)count, _zstreamInputSize);

                    // configure the outputBuffer
                    Interop.Buffer outputBuffer;
                    outputBuffer.Data = Marshal.UnsafeAddrOfPinnedArrayElement(_data, 0);
                    outputBuffer.Size = new UIntPtr(_zstreamOutputSize);
                    outputBuffer.Position = UIntPtr.Zero;

                    // configure the inputBuffer
                    Interop.Buffer inputBuffer;
                    inputBuffer.Data = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
                    inputBuffer.Size = new UIntPtr(inputSize);
                    inputBuffer.Position = UIntPtr.Zero;

                    // compress inputBuffer to outputBuffer
                    Interop.ThrowIfError(Interop.ZSTD_compressStream(_zstream, &outputBuffer, &inputBuffer));

                    // write data to output stream
                    int outputBufferPosition = (int)outputBuffer.Position.ToUInt32();
                    Stream.Write(_data, 0, outputBufferPosition);

                    // calculate progress in inputBuffer
                    int inputBufferPosition = (int)inputBuffer.Position.ToUInt32();
                    offset += inputBufferPosition;
                    count -= inputBufferPosition;
                }
            }
            finally
            {
                alloc1.Free();
                alloc2.Free();
            }
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------

        private void ProcessStream(Func<IntPtr, Interop.Buffer, Interop.Buffer> outputAction)
        {
            var alloc = GCHandle.Alloc(_data, GCHandleType.Pinned);

            try
            {
                Interop.Buffer outputBuffer;
                outputBuffer.Data = Marshal.UnsafeAddrOfPinnedArrayElement(_data, 0);
                outputBuffer.Size = new UIntPtr(_zstreamOutputSize);
                outputBuffer.Position = UIntPtr.Zero;

                outputBuffer = outputAction(_zstream, outputBuffer);

                int outputBufferPosition = (int)outputBuffer.Position.ToUInt32();
                Stream.Write(_data, 0, outputBufferPosition);
            }
            finally
            {
                alloc.Free();
            }
        }
    }
}
