using System.IO;
using System.IO.Compression;
using System.Text;
using Esper.Zstandard;
using NUnit.Framework;

namespace Esper.Test
{
    public class ZstandardTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestZstandard()
        {
            var strBytes = Encoding.UTF8.GetBytes(TestConstants.Miaou);
            var ms = new MemoryStream();
            var streamIn = new ZstandardStream(ms, ZstandardStream.MaxCompressionLevel, true);
            streamIn.Write(strBytes, 0, strBytes.Length);
            streamIn.Flush();
            ms.Position = 0;
            var ms2 = new MemoryStream();
            using (var stream = new ZstandardStream(ms, CompressionMode.Decompress, true))
                stream.CopyTo(ms2);
            Assert.AreEqual(strBytes, ms2.ToArray());
        }
    }
}
