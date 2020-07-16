using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Esper.Test
{
    public class EsperTests
    {
        [Test]
        public void TestCircleBuffer()
        {
            // Test some operations with circlebuffer
            CircleBuffer<byte> cb = new CircleBuffer<byte>(100);
            Random r = new Random();
            byte[] a = new byte[60];
            r.NextBytes(a);
            foreach (byte b in a)
                cb.Add(b);
            List<byte> list = new List<byte>(a);
            Assert.IsTrue(cb.SequenceEqual(list));
            cb.RemoveAt(40);
            list.RemoveAt(40);
            Assert.IsTrue(cb.SequenceEqual(list));
            cb.RemoveAt(10);
            list.RemoveAt(10);
            Assert.IsTrue(cb.SequenceEqual(list));
            cb.Insert(5, 10);
            list.Insert(5, 10);
            Assert.IsTrue(cb.SequenceEqual(list));
            cb.Insert(50, 60);
            list.Insert(50, 60);
            Assert.IsTrue(cb.SequenceEqual(list));
        }

        [Test]
        public void TestMultiBufferStream()
        {
            // Test a bunch of random location reads
            Random r = new Random();
            byte[] a = new byte[4096*4];
            r.NextBytes(a);
            MemoryStream ms = new MemoryStream(a);
            using MultiBufferStream mbs = new MultiBufferStream(ms, true, 8, 128);
            mbs.LargeReadOverride = false;
            byte[] temp = new byte[256];
            for (int i = 0; i < 128; i++)
            {
                int position = 16 * (r.Next() % 256) * 4;
                mbs.Position = position;
                int read = mbs.Read(temp, 0, 256);
                //Console.WriteLine($"{i} {position} {read}");
                Assert.AreEqual(new ArraySegment<byte>(a, position, read), new ArraySegment<byte>(temp, 0, read));
            }

            // Test full read
            mbs.Position = 0;
            MemoryStream ms2 = new MemoryStream();
            mbs.CopyTo(ms2);
            ms2.TryGetBuffer(out ArraySegment<byte> ms2b);
            Assert.AreEqual(new ArraySegment<byte>(a), ms2b);
        }

        [Test]
        public void TestMultiBufferStream2()
        {
            // Test a bunch of random location reads
            Random r = new Random();
            byte[] a = new byte[4096*4];
            r.NextBytes(a);
            MemoryStream ms = new MemoryStream(a);
            using MultiBufferStream mbs = new MultiBufferStream(ms, false, 8, 128);
            mbs.LargeReadOverride = false;
            byte[] temp = new byte[256];
            for (int i = 0; i < 128; i++)
            {
                int position = 16 * (r.Next() % 256) * 4;
                mbs.Position = position;
                int read = mbs.Read(temp, 0, 256);
                //Console.WriteLine($"{i} {position} {read}");
                Assert.AreEqual(new ArraySegment<byte>(a, position, read), new ArraySegment<byte>(temp, 0, read));
            }

            // Test full read
            mbs.Position = 0;
            MemoryStream ms2 = new MemoryStream();
            mbs.CopyTo(ms2);
            ms2.TryGetBuffer(out ArraySegment<byte> ms2b);
            Assert.AreEqual(new ArraySegment<byte>(a), ms2b);
        }
    }
}
