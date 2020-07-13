using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ns;

namespace Esper.Misaka
{
    /// <summary>
    /// Content data index
    /// </summary>
    internal class MisakaEntryIndex : IReadOnlyDictionary<string, Location>
    {
        internal static (Action<NetSerializer, MisakaEntryIndex> encoder,
            Func<NetSerializer, MisakaEntryIndex> decoder)
            Converter => (
            (s, x) =>
            {
                s.WriteS32(x.Count);
                s.WriteSpan<byte>(x._bufData, x._bufData.Length, true);
                s.WriteSpan<int>(x._bufLoc, x._bufLoc.Length, true);
                s.WriteS32(x._bufChar.Length);
                s.WriteSpan<char>(x._bufChar, x._bufChar.Length, true);
            },
            s =>
            {
                int c = s.ReadS32();
                var bufData = new byte[c * Location.DataLength];
                s.ReadSpan<byte>(bufData, bufData.Length, true);
                var bufLoc = new int[c + 1];
                s.ReadSpan<int>(bufLoc, bufLoc.Length, true);
                var bufChar = new char[s.ReadS32()];
                s.ReadSpan<char>(bufChar, bufChar.Length, true);
                return new MisakaEntryIndex(c, bufData, bufLoc, bufChar);
            });

        private static readonly MisakaEntryIndex _empty =
            new MisakaEntryIndex(0, new byte[0], new int[1], new char[0]);

        /// <summary>
        /// Number of elements
        /// </summary>
        public int Count { get; }

        private readonly byte[] _bufData;
        private readonly int[] _bufLoc;
        private readonly char[] _bufChar;

        private MisakaEntryIndex(int c, byte[] bufData, int[] bufLoc, char[] bufChar)
        {
            Count = c;
            _bufData = bufData;
            _bufLoc = bufLoc;
            _bufChar = bufChar;
        }

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="collection">Collection</param>
        /// <returns>Instance</returns>
        internal static MisakaEntryIndex Create(IDictionary<string, Location> collection)
        {
            if (collection.Count == 0)
                return _empty;

            int c = collection.Count;
            var bufLoc = new int[c + 1];

            int len = collection.Sum(kvp => kvp.Key.Length);

            var bufChar = new char[len];
            bufLoc[0] = 0;
            var spanChar = bufChar.AsSpan();

            var bufData = new byte[c * Location.DataLength];

            len = 0;
            using var enumerator = collection.GetEnumerator();
            for (int i = 0; i < c; i++)
            {
                // ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable
                enumerator.MoveNext();
                // ReSharper restore PossiblyImpureMethodCallOnReadonlyVariable
                var kvp = enumerator.Current;
                Location.Pack(bufData.AsSpan(i * Location.DataLength, Location.DataLength), kvp.Value);
                kvp.Key.AsSpan().CopyTo(spanChar.Slice(len));
                len += kvp.Key.Length;
                bufLoc[i + 1] = len;
            }

            return new MisakaEntryIndex(c, bufData, bufLoc, bufChar);
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            int min = 0;
            int max = Count - 1;
            var baseSpan = _bufChar.AsSpan();
            var strKey = key.AsSpan();
            while (min <= max)
            {
                int mid = (min + max) / 2;
                int pos = _bufLoc[mid];
                int len = _bufLoc[mid + 1] - pos;
                int cmp = MemoryExtensions.CompareTo(baseSpan.Slice(pos, len), strKey,
                    StringComparison.InvariantCulture);
                if (cmp == 0)
                    return true;

                if (cmp > 0)
                    max = mid - 1;
                else
                    min = mid + 1;
            }

            return false;
        }

        /// <summary>
        /// Try getting value
        /// </summary>
        /// <param name="str">Index</param>
        /// <param name="item">Retrieved value</param>
        /// <returns>True if found</returns>
        public bool TryGetValue(string str, out Location item)
        {
            int min = 0;
            int max = Count - 1;
            var baseSpan = _bufChar.AsSpan();
            var strKey = str.AsSpan();
            while (min <= max)
            {
                int mid = (min + max) / 2;
                int pos = _bufLoc[mid];
                int len = _bufLoc[mid + 1] - pos;
                int cmp = MemoryExtensions.CompareTo(baseSpan.Slice(pos, len), strKey,
                    StringComparison.InvariantCulture);
                if (cmp == 0)
                {
                    item = Location.Extract(_bufData.AsSpan(mid * Location.DataLength, Location.DataLength));
                    return true;
                }

                if (cmp > 0)
                    max = mid - 1;
                else
                    min = mid + 1;
            }

            item = default;
            return false;
        }

        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="index">Index</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public Location this[string index]
        {
            get
            {
                int min = 0;
                int max = Count - 1;
                var baseSpan = _bufChar.AsSpan();
                var strKey = index.AsSpan();
                while (min <= max)
                {
                    int mid = (min + max) / 2;
                    int pos = _bufLoc[mid];
                    int len = _bufLoc[mid + 1] - pos;
                    int cmp = MemoryExtensions.CompareTo(baseSpan.Slice(pos, len), strKey,
                        StringComparison.InvariantCulture);
                    if (cmp == 0)
                    {
                        return Location.Extract(_bufData.AsSpan(mid * Location.DataLength, Location.DataLength));
                    }

                    if (cmp > 0)
                        max = mid - 1;
                    else
                        min = mid + 1;
                }

                throw new KeyNotFoundException();
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> Keys
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    int pos = _bufLoc[i];
                    int len = _bufLoc[i + 1] - pos;
                    yield return new string(_bufChar, pos, len);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<Location> Values
        {
            get
            {
                for (int i = 0; i < Count; i++)
                    yield return Location.Extract(_bufData.AsSpan(i * Location.DataLength, Location.DataLength));
            }
        }

        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="index">Index</param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public Location this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();
                return Location.Extract(_bufData.AsSpan(index * Location.DataLength, Location.DataLength));
            }
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, Location>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                int pos = _bufLoc[i];
                int len = _bufLoc[i + 1] - pos;
                yield return new KeyValuePair<string, Location>(new string(_bufChar, pos, len),
                    Location.Extract(_bufData.AsSpan(i * Location.DataLength, Location.DataLength)));
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
