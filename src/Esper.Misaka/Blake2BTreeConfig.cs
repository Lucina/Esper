using System;

namespace Esper.Misaka {
    internal sealed class Blake2BTreeConfig : ICloneable {
        public int IntermediateHashSize { get; set; }
        public int MaxHeight { get; set; }
        public long LeafSize { get; set; }
        public int FanOut { get; set; }

        public Blake2BTreeConfig() {
            IntermediateHashSize = 64;
        }

        public Blake2BTreeConfig Clone() =>
            new Blake2BTreeConfig {
                IntermediateHashSize = IntermediateHashSize, MaxHeight = MaxHeight, LeafSize = LeafSize, FanOut = FanOut
            };

        public static Blake2BTreeConfig CreateInterleaved(int parallelism) =>
            new Blake2BTreeConfig {FanOut = parallelism, MaxHeight = 2, IntermediateHashSize = 64};


        object ICloneable.Clone() => Clone();
    }
}