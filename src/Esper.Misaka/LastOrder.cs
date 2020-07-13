using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Esper.Zstandard;
using Ns;

namespace Esper.Misaka
{
    /// <summary>
    /// Utility for .mw container support format .lo
    /// </summary>
    public static class LastOrder
    {
        /// <summary>
        /// Write last order data to stream
        /// </summary>
        /// <param name="baseStream">Stream to write to</param>
        /// <param name="blockHashes">Block hashes</param>
        /// <param name="fileHashes">File hashes</param>
        /// <param name="locations">Locations</param>
        public static void WriteLastOrder(Stream baseStream, long[] blockHashes, long[] fileHashes,
            Location[] locations)
        {
            using var zs = new ZstandardStream(baseStream, ZstandardStream.MaxCompressionLevel, true);
            var ns = new NetSerializer(zs);
            ns.Serialize(blockHashes);
            ns.Serialize(fileHashes);
            ns.WriteS32(locations.Length);
            var locSpan = MemoryMarshal.Cast<Location, byte>(locations);
            ns.WriteSpan(locSpan, locSpan.Length, false);
        }

        /// <summary>
        /// Read last order data from stream
        /// </summary>
        /// <param name="baseStream">Stream to read from</param>
        /// <returns>Block hashes</returns>
        public static (long[] blockHashes, long[] fileHashes, Location[] locations) ReadLastOrder(Stream baseStream)
        {
            using var zs = new ZstandardStream(baseStream, CompressionMode.Decompress, true);
            var ns = new NetSerializer(zs);
            var blockHashes = ns.Deserialize<long[]>();
            var fileHashes = ns.Deserialize<long[]>();
            var locations = new Location[ns.ReadS32()];
            var locSpan = MemoryMarshal.Cast<Location, byte>(locations);
            ns.ReadSpan(locSpan, locSpan.Length, false);
            return (blockHashes, fileHashes, locations);
        }
    }
}
