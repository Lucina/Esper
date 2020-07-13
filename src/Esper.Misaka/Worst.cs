using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Esper.Zstandard;
using Ns;

namespace Esper.Misaka
{
    /// <summary>
    /// Provides r/w access for .mw container
    /// </summary>
    public class Worst
    {
        private readonly MisakaEntryIndex _index;
        private readonly BlockUtil.Reader _blockReader;

        private Worst(MisakaEntryIndex index, BlockUtil.Reader blockReader)
        {
            _index = index;
            _blockReader = blockReader;
        }

        /// <summary>
        /// Get filenames in container
        /// </summary>
        /// <returns>Stored entries</returns>
        public IEnumerable<string> GetEntries() => _index.Keys;

        /// <summary>
        /// Load data from container
        /// </summary>
        /// <param name="value">Path</param>
        /// <param name="stream">Target</param>
        /// <returns>True if path found</returns>
        public bool TryReadToStream(string value, Stream stream)
        {
            if (!_index.TryGetValue(value, out var res))
                return false;

            _blockReader.FillStream(res, stream);
            return true;
        }

        /// <summary>
        /// Load stream from container
        /// </summary>
        /// <param name="value">Path</param>
        /// <param name="stream">Target</param>
        /// <returns>True if path found</returns>
        public bool TryGetStream(string value, out Stream? stream)
        {
            if (!_index.TryGetValue(value, out var res))
            {
                stream = null;
                return false;
            }

            stream = _blockReader.GetStream(res);
            return true;
        }

        /// <summary>
        /// Load data from container
        /// </summary>
        /// <param name="value">Path</param>
        /// <param name="array">Result</param>
        /// <returns>True if path found</returns>
        public bool TryGetArray(string value, out byte[]? array)
        {
            if (!_index.TryGetValue(value, out var res))
            {
                array = null;
                return false;
            }

            array = _blockReader.GetArray(res);
            return true;
        }

        /// <summary>
        /// Load container
        /// </summary>
        /// <param name="baseStream">Base stream</param>
        /// <returns>Container</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="baseStream"/> is null</exception>
        /// <exception cref="ArgumentException">If <paramref name="baseStream"/> is not seekable or readable</exception>
        public static Worst Read(Stream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanSeek)
                throw new ArgumentException("Stream must be seekable");
            if (!baseStream.CanRead)
                throw new ArgumentException("Stream must be readable");
            var ns = new NetSerializer(baseStream, NsMisaka.Converters);
            baseStream.Position = 0;
            long pos = ns.ReadS64();
            baseStream.Position = pos;
            MisakaEntryIndex index;
            long[] blocks;
            using (var zs = new ZstandardStream(baseStream, CompressionMode.Decompress, true))
            {
                ns.BaseStream = zs;
                index = ns.Deserialize<MisakaEntryIndex>();
                blocks = ns.Deserialize<long[]>();
            }

            return new Worst(index, new BlockUtil.Reader(baseStream, blocks));
        }

        /// <summary>
        /// Write container wrapper
        /// </summary>
        /// <param name="targetStream">Target stream</param>
        /// <param name="blocks">Blocks stored</param>
        /// <param name="collection">Sorted name/location collection</param>
        /// <returns>Container</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="targetStream"/>, <paramref name="blocks"/>, or <paramref name="collection"/> are null</exception>
        /// <exception cref="ArgumentException">If <paramref name="targetStream"/> is not seekable or writable</exception>
        /// <remarks>
        /// Should be called after calling <see cref="WriteData"/>.
        /// </remarks>
        public static Worst WriteWrapper(Stream targetStream, long[] blocks,
            IDictionary<string, Location> collection)
        {
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (!targetStream.CanSeek)
                throw new ArgumentException("Stream must be seekable");
            if (!targetStream.CanWrite)
                throw new ArgumentException("Stream must be writable");
            var ns = new NetSerializer(targetStream, NsMisaka.Converters);
            targetStream.Position = 0;
            ns.WriteS64(targetStream.Length);
            targetStream.Position = targetStream.Length;
            var index = MisakaEntryIndex.Create(collection);
            using (var zs = new ZstandardStream(targetStream, ZstandardStream.MaxCompressionLevel, true))
            {
                ns.BaseStream = zs;
                ns.Serialize(index);
                ns.Serialize(blocks);
            }

            var blockReader = new BlockUtil.Reader(targetStream, blocks);
            return new Worst(index, blockReader);
        }

        /// <summary>
        /// Write container data
        /// </summary>
        /// <param name="inputs">Inputs</param>
        /// <param name="targetStream">Target stream</param>
        /// <param name="enforceBlockFill">Enforce filling blocks</param>
        /// <param name="blockList">List of block positions (+1 final for end position)</param>
        /// <param name="hashBlocks">If true, calculate per-block hash</param>
        /// <param name="blockHashList">Block hash</param>
        /// <returns>Metadata for each compressed input</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">If <paramref name="targetStream"/> is not seekable or writable</exception>
        public static List<Location> WriteData(
            IEnumerable<Func<(Stream stream, bool keepOpen, int? enforcedOffset)>> inputs, Stream targetStream,
            bool enforceBlockFill, out List<long> blockList, bool hashBlocks, out List<long>? blockHashList)
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));
            if (targetStream == null)
                throw new ArgumentNullException(nameof(targetStream));
            if (!targetStream.CanSeek)
                throw new ArgumentException("Stream must be seekable");
            if (!targetStream.CanWrite)
                throw new ArgumentException("Stream must be writable");
            targetStream.SetLength(0);
            targetStream.Position = sizeof(long);
            return BlockUtil.WriteData(inputs, targetStream, enforceBlockFill, out blockList, hashBlocks,
                out blockHashList);
        }
    }
}
