using System;
using System.Diagnostics.CodeAnalysis;

namespace Esper.Misaka
{
    internal static class Blake2IvBuilder
    {
        private static readonly Blake2BTreeConfig SequentialTreeConfig = new Blake2BTreeConfig()
        {
            IntermediateHashSize = 0, LeafSize = 0, FanOut = 1, MaxHeight = 1
        };

        [SuppressMessage("ReSharper", "NotResolvedInText")]
        public static ulong[] ConfigB(Blake2BConfig config, Blake2BTreeConfig treeConfig)
        {
            bool isSequential = treeConfig == null;
            if (isSequential)
                treeConfig = SequentialTreeConfig;
            var rawConfig = new ulong[8];

            //digest length
            if (config.OutputSizeInBytes <= 0 | config.OutputSizeInBytes > 64)
                throw new ArgumentOutOfRangeException("config.OutputSize");
            rawConfig[0] |= (uint)config.OutputSizeInBytes;

            //Key length
            if (config.Key != null)
            {
                if (config.Key.Length > 64)
                    throw new ArgumentException("Key too long", "config.Key");
                rawConfig[0] |= (uint)config.Key.Length << 8;
            }

            // FanOut
            rawConfig[0] |= (uint)treeConfig.FanOut << 16;
            // Depth
            rawConfig[0] |= (uint)treeConfig.MaxHeight << 24;
            // Leaf length
            rawConfig[0] |= ((ulong)(uint)treeConfig.LeafSize) << 32;
            // Inner length
            if (!isSequential && (treeConfig.IntermediateHashSize <= 0 || treeConfig.IntermediateHashSize > 64))
                throw new ArgumentOutOfRangeException("treeConfig.TreeIntermediateHashSize");
            rawConfig[2] |= (uint)treeConfig.IntermediateHashSize << 8;
            // Salt
            if (config.Salt != null)
            {
                if (config.Salt.Length != 16)
                    throw new ArgumentException("config.Salt has invalid length");
                rawConfig[4] = Blake2BCore.BytesToUInt64(config.Salt, 0);
                rawConfig[5] = Blake2BCore.BytesToUInt64(config.Salt, 8);
            }

            // Personalization
            if (config.Personalization == null) return rawConfig;
            if (config.Personalization.Length != 16)
                throw new ArgumentException("config.Personalization has invalid length");
            rawConfig[6] = Blake2BCore.BytesToUInt64(config.Personalization, 0);
            rawConfig[7] = Blake2BCore.BytesToUInt64(config.Personalization, 8);

            return rawConfig;
        }

        public static void ConfigBSetNode(ulong[] rawConfig, byte depth, ulong nodeOffset)
        {
            rawConfig[1] = nodeOffset;
            rawConfig[2] = (rawConfig[2] & ~0xFFul) | depth;
        }
    }
}
