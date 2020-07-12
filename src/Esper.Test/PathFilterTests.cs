using System.Collections.Generic;
using NUnit.Framework;

namespace Esper.Test
{
    public class PathFilterTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void PathFilterTest()
        {
            var filters = new List<string>
            {
                "**/json.json",
                "jacob/**/beans.bat",
                "jacob/**/*.png",
                "!jacob/**/x.png",
                "alphabet/soup/1234.*",
                "anything/in/this/folder/**",
                "start/with/something/pure",
                "the/compromises/*",
                "hydra/was/founded/on/the_*/belief",
                "*.cs",
                "starts_with_this*"
            };
            var f = PathFilter.GenerateFilter(filters);
            Assert.AreEqual(true, f.Test("harry/json.json"));
            Assert.AreEqual(true, f.Test("harry/pote/potter/json.json"));
            Assert.AreEqual(false, f.Test("jacob/spiegan/x.b"));
            Assert.AreEqual(true, f.Test("jacob/spiegan/sniper/beans.bat"));
            Assert.AreEqual(true, f.Test("jacob/spiegan/beans.bat"));
            Assert.AreEqual(true, f.Test("jacob/spiegan/beans.png"));
            Assert.AreEqual(false, f.Test("jacob/spiegan/not/x.png"));
            Assert.AreEqual(true, f.Test("alphabet/soup/1234.png"));
            Assert.AreEqual(false, f.Test("alphabet/soup/1233.png"));
            Assert.AreEqual(true, f.Test("anything/in/this/folder/jacob.png"));
            Assert.AreEqual(true, f.Test("anything/in/this/folder/jacob_lewd.png"));
            Assert.AreEqual(true, f.Test("start/with/something/pure"));
            Assert.AreEqual(false, f.Test("start/with/something/pure/then/come/the/mistakes"));
            Assert.AreEqual(true, f.Test("the/compromises/we"));
            Assert.AreEqual(false, f.Test("the/compromises/we/create/our/own/demons"));
            Assert.AreEqual(true, f.Test("hydra/was/founded/on/the_samwell/belief"));
            Assert.AreEqual(false, f.Test("hydra/was/founded/on/thetis/belief"));
            Assert.AreEqual(false, f.Test("x.csproj"));
            Assert.AreEqual(false, f.Test("starts_with_thif"));
            Assert.AreEqual(true, f.Test("starts_with_this"));
            Assert.AreEqual(true, f.Test("starts_with_this_plus_more"));

            var filter2 = new List<string> {"**"};
            var f2 = PathFilter.GenerateFilter(filter2);
            Assert.AreEqual(true, f2.Test("anything/should.go"));
            Assert.AreEqual(true, f2.Test("literally/anything/should.go"));
        }
    }
}
