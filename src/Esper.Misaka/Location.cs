using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Esper.Misaka {
    /// <summary>
    /// Location of compressed data in .mw container
    /// </summary>
    public struct Location {
        internal const int DataLength = sizeof(int) + sizeof(int) + sizeof(long);
        /// <summary>
        /// Block index
        /// </summary>
        public int Block { get; }
        /// <summary>
        /// Block offset
        /// </summary>
        public int Offset { get; }
        /// <summary>
        /// Datum size
        /// </summary>
        public long Length { get; }

        internal Location(int block, int offset, long length) {
            Block = block;
            Offset = offset;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Location Extract(ReadOnlySpan<byte> span) => new Location(
            BinaryPrimitives.ReadInt32LittleEndian(span),
            BinaryPrimitives.ReadInt32LittleEndian(span.Slice(sizeof(int))),
            BinaryPrimitives.ReadInt64LittleEndian(span.Slice(sizeof(int) + sizeof(int))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Pack(Span<byte> span, Location location) {
            BinaryPrimitives.WriteInt32LittleEndian(span, location.Block);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(sizeof(int)), location.Offset);
            BinaryPrimitives.WriteInt64LittleEndian(span.Slice(sizeof(int) + sizeof(int)), location.Length);
        }
    }
}