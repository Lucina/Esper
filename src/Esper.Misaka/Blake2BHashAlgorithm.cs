using System;
using System.Security.Cryptography;

namespace Esper.Misaka {
    /// <inheritdoc />
    public sealed class Blake2BHashAlgorithm : HashAlgorithm {
        private readonly Blake2BCore _core;
        private ulong[] _rawConfig;
        private readonly byte[] _key = new byte[128];
        private int _outputSizeInBytes;
        private static readonly Blake2BConfig DefaultConfig = new Blake2BConfig();

        /// <inheritdoc />
        public Blake2BHashAlgorithm(Blake2BConfig config = null) {
            _core = new Blake2BCore();
            SetConfig(config);
        }

        /// <summary>
        /// Set config
        /// </summary>
        /// <param name="config">Config</param>
        public void SetConfig(Blake2BConfig config = null) {
            if (config == null)
                config = DefaultConfig;
            _rawConfig = Blake2IvBuilder.ConfigB(config, null);
            if (config.Key != null && config.Key.Length != 0) Array.Copy(config.Key, _key, config.Key.Length);
            _outputSizeInBytes = config.OutputSizeInBytes;
            Initialize();
        }

        /// <inheritdoc />
        protected override void HashCore(byte[] array, int ibStart, int cbSize) =>
            _core.HashCore(array, ibStart, cbSize);

        /// <inheritdoc />
        protected override byte[] HashFinal() {
            var fullResult = _core.HashFinal();
            if (_outputSizeInBytes == fullResult.Length) return fullResult;
            var result = new byte[_outputSizeInBytes];
            Array.Copy(fullResult, result, result.Length);
            return result;
        }

        /// <inheritdoc />
        public override void Initialize() {
            _core.Initialize(_rawConfig);
            if (_key != null) _core.HashCore(_key, 0, _key.Length);
        }
    }
}