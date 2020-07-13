using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Esper.Zstandard;
using static Esper.DataSizes;
using static System.Buffers.ArrayPool<byte>;

namespace Esper.Misaka
{
    internal static class BlockUtil
    {
        private const int BlockSize = 256 * KiB;

        //private const int BufferSize = 4096;
        private const int BufferSize = BlockSize;
        private static readonly byte[] _blankBuffer = new byte[BufferSize];

        internal static List<Location> WriteData(
            IEnumerable<Func<(Stream stream, bool keepOpen, int? enforcedOffset)>> inputs, Stream targetStream,
            bool enforceBlockFill, out List<long> blockList, bool hashBlocks, out List<long>? blockHashList)
        {
            blockList = new List<long>();
            blockHashList = hashBlocks ? new List<long>() : null;
            using var hasher = hashBlocks ? new Blake2BHashAlgorithm() : null;
            var sStream = hashBlocks ? new SStream() : null;
            using var zsa = hashBlocks
                ? new ZstandardStream(sStream, CompressionMode.Decompress, true)
                : null;
            long tmpTar;
            var locations = new List<Location>();
            using var zs = new ZstandardStream(targetStream, ZstandardStream.MaxCompressionLevel, true);
            int currentBlock = 0;
            long currentBlockPosition = targetStream.Position;
            int currentBlockOffset = 0;
            blockList.Add(currentBlockPosition);
            foreach (var entryFunc in inputs)
            {
                (Stream stream, bool keepOpen, int? enforcedOffsetNullable) = entryFunc.Invoke();
                try
                {
                    int targetBlockOffset = currentBlockOffset;
                    // enforce offset
                    if (enforcedOffsetNullable is { } enforcedOffset)
                    {
                        // can't get offset in current block, add another
                        if (currentBlockOffset > enforcedOffset)
                        {
                            // fill block
                            if (enforceBlockFill)
                            {
                                int left = BlockSize - currentBlockOffset;
                                while (left > 0)
                                {
                                    int read = Math.Min(left, BufferSize);
                                    zs.Write(_blankBuffer, 0, read);
                                    left -= read;
                                }
                            }

                            zs.Flush();
                            zs.Reset();
                            tmpTar = targetStream.Position;
                            blockHashList?.Add(Hash(hasher!, zsa!, sStream!, targetStream, currentBlockPosition,
                                tmpTar - currentBlockPosition));
                            currentBlockPosition = tmpTar;
                            currentBlockOffset = 0;
                            currentBlock++;
                            blockList.Add(currentBlockPosition);
                        }

                        targetBlockOffset = enforcedOffset;
                    }

                    locations.Add(new Location(currentBlock, targetBlockOffset, (int)stream.Length));

                    while (true)
                    {
                        // keep writing blocks for stream
                        int len = WriteBlock(zs, currentBlockOffset, targetBlockOffset, out int resultBlockOffset,
                            stream);
                        if (resultBlockOffset == BlockSize)
                        {
                            // full block
                            zs.Flush();
                            zs.Reset();
                            tmpTar = targetStream.Position;
                            blockHashList?.Add(Hash(hasher!, zsa!, sStream!, targetStream, currentBlockPosition,
                                tmpTar - currentBlockPosition));
                            currentBlockPosition = tmpTar;
                            currentBlockOffset = 0;
                            currentBlock++;
                            blockList.Add(currentBlockPosition);
                        }
                        else
                            currentBlockOffset = resultBlockOffset;

                        targetBlockOffset = currentBlockOffset;

                        if (len == 0) break;
                    }
                }
                finally
                {
                    if (!keepOpen)
                        stream.Close();
                }
            }

            if (currentBlockOffset == 0) return locations;
            int endLeft = BlockSize - currentBlockOffset;
            while (endLeft > 0)
            {
                int read = Math.Min(endLeft, BufferSize);
                zs.Write(_blankBuffer, 0, read);
                endLeft -= read;
            }

            zs.Flush();

            tmpTar = targetStream.Position;
            blockHashList?.Add(Hash(hasher!, zsa!, sStream!, targetStream, currentBlockPosition,
                tmpTar - currentBlockPosition));
            currentBlockPosition = tmpTar;
            blockList.Add(currentBlockPosition);

            return locations;
        }

        private static long Hash(Blake2BHashAlgorithm hasher, ZstandardStream zstandardStream, SStream sStream,
            Stream stream, long offset, long length)
        {
            zstandardStream.Reset();
            sStream.Set(stream, offset, length);
            long hash = MemoryMarshal.Read<long>(hasher.ComputeHash(zstandardStream));
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(hash) : hash;
        }

        private static int WriteBlock(ZstandardStream zstd, int currentBlockOffset, int targetBlockOffset,
            out int resultBlockOffset, Stream sourceStream)
        {
            var buf = Shared.Rent(BufferSize);
            try
            {
                if (currentBlockOffset > targetBlockOffset)
                {
                    // fill block
                    resultBlockOffset = BlockSize;
                    int left = BlockSize - currentBlockOffset;
                    while (left > 0)
                    {
                        int read = Math.Min(left, BufferSize);
                        zstd.Write(_blankBuffer, 0, read);
                        left -= read;
                    }

                    return -1;
                }

                if (currentBlockOffset < targetBlockOffset)
                {
                    // pad block
                    int left = targetBlockOffset - currentBlockOffset;
                    while (left > 0)
                    {
                        int read = Math.Min(left, BufferSize);
                        zstd.Write(_blankBuffer, 0, read);
                        left -= read;
                    }
                }

                // write block
                int total = 0;
                resultBlockOffset = targetBlockOffset;
                while (true)
                {
                    int read = sourceStream.Read(buf, 0, Math.Min(BufferSize, BlockSize - resultBlockOffset));
                    if (read == 0) break;
                    resultBlockOffset += read;
                    total += read;
                    zstd.Write(buf, 0, read);
                }

                return total;
            }
            finally
            {
                Shared.Return(buf);
            }
        }

        internal class Reader : IDisposable
        {
            private readonly ZstandardStream _decompressor;
            private readonly Stream _baseStream;
            private readonly SStream _sStream;
            private readonly IReadOnlyList<long> _blockList;

            public Reader(Stream baseStream, IReadOnlyList<long> blockList)
            {
                _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
                _blockList = blockList ?? throw new ArgumentNullException(nameof(blockList));
                _sStream = new SStream();
                _decompressor = new ZstandardStream(_sStream, CompressionMode.Decompress, true);
            }

            public void FillStream(Location location, Stream targetStream)
            {
                int block = location.Block;
                int blockOffset = location.Offset;
                long left = location.Length;
                //_decompressor.Reset();
                while (left > 0)
                {
                    _sStream.Set(_baseStream, _blockList[block], _blockList[block + 1] - _blockList[block]);
                    _decompressor.Reset();
                    left -= ReadBlock(_decompressor, blockOffset, targetStream, left);
                    blockOffset = 0;
                    block++;
                }
            }

            public byte[] GetArray(Location location)
            {
                var res = new byte[location.Length];
                int block = location.Block;
                int blockOffset = location.Offset;
                long left = location.Length;
                int ofs = 0;
                //_decompressor.Reset();
                while (left > 0)
                {
                    _sStream.Set(_baseStream, _blockList[block], _blockList[block + 1] - _blockList[block]);
                    _decompressor.Reset();
                    int read = ReadBlock(_decompressor, true, blockOffset, res, ofs, left);
                    left -= read;
                    ofs += read;
                    blockOffset = 0;
                    block++;
                }

                return res;
            }

            public Stream GetStream(Location location) => new AutoStream(location, _baseStream, _blockList);

            private static int ReadBlock(ZstandardStream zstd, int blockOffset, Stream targetStream,
                long bytesToDecompress)
            {
                //zstd.Reset();
                var buf = Shared.Rent(BufferSize);
                try
                {
                    // Skip blockOffset
                    int skipLeft = blockOffset;
                    while (skipLeft > 0)
                    {
                        int read = zstd.Read(buf, 0, Math.Min(skipLeft, BufferSize));
                        if (read == 0) throw new EndOfStreamException();
                        skipLeft -= read;
                    }

                    // Read lower of bytes in block and bytes to decompress
                    long virtualNeed = Math.Min(BlockSize - blockOffset, bytesToDecompress);
                    long virtualLeft = virtualNeed;
                    while (virtualLeft > 0)
                    {
                        int read = zstd.Read(buf, 0, (int)Math.Min(virtualLeft, BufferSize));
                        if (read == 0) throw new EndOfStreamException();
                        targetStream.Write(buf, 0, read);
                        virtualLeft -= read;
                    }

                    return (int)(virtualNeed - virtualLeft);
                }
                finally
                {
                    Shared.Return(buf);
                }
            }

            private static int ReadBlock(ZstandardStream zstd, bool enableSkip, int blockOffset, byte[] targetBuffer,
                int offset, long bytesToDecompress)
            {
                var buf = Shared.Rent(BufferSize);
                try
                {
                    int avail = targetBuffer.Length - offset;
                    if (avail <= 0) throw new ApplicationException();
                    //zstd.Reset();
                    // Skip blockOffset
                    if (enableSkip)
                    {
                        int skipLeft = blockOffset;
                        while (skipLeft > 0)
                        {
                            int read = zstd.Read(buf, 0, Math.Min(skipLeft, BufferSize));
                            if (read == 0) throw new EndOfStreamException();
                            skipLeft -= read;
                        }
                    }

                    // Read lower of bytes in block and bytes to decompress
                    long virtualNeed = Math.Min(BlockSize - blockOffset, bytesToDecompress);
                    long virtualLeft = virtualNeed;
                    while (virtualLeft > 0 && avail > 0)
                    {
                        int read = zstd.Read(targetBuffer, offset, (int)Math.Min(virtualLeft, avail));
                        if (read == 0) throw new EndOfStreamException();
                        virtualLeft -= read;
                        offset += read;
                        avail -= read;
                    }

                    return (int)(virtualNeed - virtualLeft);
                }
                finally
                {
                    Shared.Return(buf);
                }
            }

            public void Dispose()
            {
                _decompressor.Dispose();
                _baseStream.Dispose();
                _sStream.Dispose();
            }

            private class AutoStream : Stream
            {
                private int _block;
                private int _blockOffset;
                private long _left;
                private bool _first;
                private readonly Stream _stream;
                private readonly SStream _sStream;
                private readonly ZstandardStream _decompressor;
                private readonly IReadOnlyList<long> _blockList;

                internal AutoStream(Location location, Stream stream, IReadOnlyList<long> blockList)
                {
                    _block = location.Block;
                    _blockOffset = location.Offset;
                    _left = location.Length;
                    _first = true;
                    Length = location.Length;
                    _stream = stream;
                    _sStream = new SStream(isolate: true);
                    _blockList = blockList;
                    _sStream.Set(_stream, _blockList[_block], _blockList[_block + 1] - _blockList[_block]);
                    _decompressor = new ZstandardStream(_sStream, CompressionMode.Decompress, true);
                }

                public override void Flush()
                {
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    if (_left == 0 || count == 0) return 0;
                    int read = ReadBlock(_decompressor, _first, _blockOffset, buffer, offset, Math.Min(_left, count));
                    _first = false;
                    _left -= read;
                    _blockOffset += read;
                    if (_blockOffset == BlockSize)
                    {
                        _decompressor.Reset();
                        _blockOffset = 0;
                        _block++;
                        _sStream.Set(_stream, _blockList[_block], _blockList[_block + 1] - _blockList[_block]);
                    }

                    return read;
                }

                public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

                public override void SetLength(long value) => throw new NotSupportedException();

                public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

                public override bool CanRead => true;
                public override bool CanSeek => false;
                public override bool CanWrite => false;
                public override long Length { get; }

                public override long Position
                {
                    get => throw new NotSupportedException();
                    set => throw new NotSupportedException();
                }
            }
        }
    }
}
