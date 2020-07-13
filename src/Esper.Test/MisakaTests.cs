using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Esper.Misaka;
using NUnit.Framework;
using static Esper.DataSizes;

namespace Esper.Test
{
    public class MisakaTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestMisaka()
        {
            // Create buffers
            var buf = new byte[1 * MiB + 3 * KiB];
            //var bufOut = new byte[buf.Length];

            // Prepare data
            var miaouB = MemoryMarshal.Cast<char, byte>(TestConstants.Miaou);
            for (int i = 0; i < buf.Length; i += miaouB.Length)
                miaouB.Slice(0, Math.Min(buf.Length - i, miaouB.Length)).CopyTo(buf.AsSpan(i));

            // Streams
            var msProto = new MemoryStream(buf);
            var msProto2 = new MemoryStream(buf);
            var msProto3 = new MemoryStream(buf);
            var ms = new MemoryStream();

            // Test enforcing offsets
            var arr = new Func<(Stream, bool, int?)>[]
            {
                () => (msProto, true, null), () => (msProto2, true, 13), () => (msProto3, true, null)
            };

            var locations = Worst.WriteData(arr, ms, false, out var blockList, true, out _);
            Assert.AreEqual(locations[1].Offset, 13);
            var arr2 = new[] {"path1", "path2", "path3"};
            var dict = new SortedDictionary<string, Location>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < locations.Count; i++)
                dict.Add(arr2[i], locations[i]);
            Worst.WriteWrapper(ms, blockList.ToArray(), dict);

            var worst = Worst.Read(ms);

            // proto2
            var msProto2A = new MemoryStream();
            Assert.IsTrue(worst.TryReadToStream("path2", msProto2A));
            Assert.AreEqual(buf, msProto2A.ToArray());
            Assert.IsTrue(worst.TryGetArray("path2", out var arrProto2B));
            Assert.AreEqual(buf, arrProto2B);
            Assert.IsTrue(worst.TryGetStream("path2", out var streamProto2C));
            msProto2A.SetLength(0);
            streamProto2C.CopyTo(msProto2A);
            Assert.AreEqual(buf, msProto2A.ToArray());
        }
    }
}
