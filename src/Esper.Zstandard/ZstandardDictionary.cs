using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Interop = Esper.Zstandard.ZstandardInterop;

namespace Esper.Zstandard
{
    /// <summary>
    /// A Zstandard dictionary improves the compression ratio and speed on small data dramatically.
    /// </summary>
    /// <remarks>
    /// A Zstandard dictionary is calculated with a high number of small sample data.
    /// Please refer to the Zstandard documentation for more details.
    /// </remarks>
    /// <seealso cref="System.IDisposable" />
    public sealed class ZstandardDictionary : IDisposable
    {
        private readonly byte[] _dictionary;
        private readonly Dictionary<int, IntPtr> _cdicts = new();
        private readonly object _lockObject = new();
        private IntPtr _ddict;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardDictionary"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary raw data.</param>
        public ZstandardDictionary(byte[] dictionary)
        {
            _dictionary = dictionary;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardDictionary"/> class.
        /// </summary>
        /// <param name="dictionaryPath">The dictionary path.</param>
        public ZstandardDictionary(string dictionaryPath)
        {
            _dictionary = File.ReadAllBytes(dictionaryPath);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZstandardDictionary"/> class.
        /// </summary>
        /// <param name="dictionaryStream">The dictionary stream.</param>
        public ZstandardDictionary(Stream dictionaryStream)
        {
            using var memoryStream = new MemoryStream();
            dictionaryStream.CopyTo(memoryStream);
            _dictionary = memoryStream.ToArray();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ZstandardDictionary"/> class.
        /// </summary>
        ~ZstandardDictionary()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private void Dispose(bool dispose)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_ddict != IntPtr.Zero)
            {
                Interop.ZSTD_freeDDict(_ddict);
                _ddict = IntPtr.Zero;
            }

            foreach (var kv in _cdicts.ToList())
            {
                Interop.ZSTD_freeCDict(kv.Value);
                _cdicts.Remove(kv.Key);
            }
        }

        /// <summary>
        /// Gets the compression dictionary for the specified compression level.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        /// <returns>
        /// The IntPtr to the compression dictionary.
        /// </returns>
        /// <exception cref="ObjectDisposedException">ZstandardDictionary</exception>
        internal IntPtr GetCompressionDictionary(int compressionLevel)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ZstandardDictionary));
            }

            lock (_lockObject)
            {
                if (_cdicts.TryGetValue(compressionLevel, out var cdict) == false)
                {
                    _cdicts[compressionLevel] = cdict = CreateCompressionDictionary(compressionLevel);
                }

                return cdict;
            }
        }

        /// <summary>
        /// Gets the decompression dictionary.
        /// </summary>
        /// <returns>
        /// The IntPtr to the decompression dictionary.
        /// </returns>
        /// <exception cref="ObjectDisposedException">ZstandardDictionary</exception>
        internal IntPtr GetDecompressionDictionary()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ZstandardDictionary));
            }

            lock (_lockObject)
            {
                if (_ddict == IntPtr.Zero)
                {
                    _ddict = CreateDecompressionDictionary();
                }

                return _ddict;
            }
        }

        /// <summary>
        /// Creates a new compression dictionary.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        /// <returns>
        /// The IntPtr to the compression dictionary.
        /// </returns>
        private IntPtr CreateCompressionDictionary(int compressionLevel)
        {
            var alloc = GCHandle.Alloc(_dictionary, GCHandleType.Pinned);

            try
            {
                var dictBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_dictionary, 0);
                var dictSize = new UIntPtr((uint)_dictionary.Length);
                return Interop.ZSTD_createCDict(dictBuffer, dictSize, compressionLevel);
            }
            finally
            {
                alloc.Free();
            }
        }

        /// <summary>
        /// Creates a new decompression dictionary.
        /// </summary>
        /// <returns>
        /// The IntPtr to the decompression dictionary.
        /// </returns>
        private IntPtr CreateDecompressionDictionary()
        {
            var alloc = GCHandle.Alloc(_dictionary, GCHandleType.Pinned);

            try
            {
                var dictBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(_dictionary, 0);
                var dictSize = new UIntPtr((uint)_dictionary.Length);
                return Interop.ZSTD_createDDict(dictBuffer, dictSize);
            }
            finally
            {
                alloc.Free();
            }
        }
    }
}
