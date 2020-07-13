using System;

namespace Esper.Misaka
{
    /// <summary>
    /// Blake2b config
    /// </summary>
    public sealed class Blake2BConfig : ICloneable
    {
        /// <summary>
        /// Personalization
        /// </summary>
        public byte[]? Personalization { get; set; }

        /// <summary>
        /// Salt
        /// </summary>
        public byte[]? Salt { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        public byte[]? Key { get; set; }

        /// <summary>
        /// Output size
        /// </summary>
        public int OutputSizeInBytes { get; set; }

        /// <summary>
        /// Output size
        /// </summary>
        public int OutputSizeInBits
        {
            get => OutputSizeInBytes * 8;
            set =>
                OutputSizeInBytes = value % 8 == 0
                    ? value / 8
                    : throw new ArgumentException("Output size must be a multiple of 8 bits");
        }

        /// <summary>
        /// Create new default config
        /// </summary>
        public Blake2BConfig()
        {
            OutputSizeInBytes = 64;
        }

        /// <summary>
        /// Clone instance
        /// </summary>
        /// <returns>Cloned instance</returns>
        public Blake2BConfig Clone()
        {
            var result = new Blake2BConfig {OutputSizeInBytes = OutputSizeInBytes};
            if (Key != null)
                result.Key = (byte[])Key.Clone();
            if (Personalization != null)
                result.Personalization = (byte[])Personalization.Clone();
            if (Salt != null)
                result.Salt = (byte[])Salt.Clone();
            return result;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
