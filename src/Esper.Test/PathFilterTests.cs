using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                "starts_with_this*",
                "abc?efg",
                "123[456]789",
                "ab*ec",
                "tes"
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
            Assert.AreEqual(false, f.Test("start/with/something/puree/then/come/the/mistakes"));
            Assert.AreEqual(true, f.Test("the/compromises/we"));
            Assert.AreEqual(false, f.Test("the/compromises/we/create/our/own/demons"));
            Assert.AreEqual(true, f.Test("hydra/was/founded/on/the_samwell/belief"));
            Assert.AreEqual(false, f.Test("hydra/was/founded/on/thetis/belief"));
            Assert.AreEqual(false, f.Test("x.csproj"));
            Assert.AreEqual(false, f.Test("starts_with_thif"));
            Assert.AreEqual(true, f.Test("starts_with_this"));
            Assert.AreEqual(true, f.Test("starts_with_this_plus_more"));
            Assert.AreEqual(true, f.Test("abcdefg"));
            Assert.AreEqual(true, f.Test("abcxefg"));
            Assert.AreEqual(true, f.Test("1235789"));
            Assert.AreEqual(false, f.Test("1230789"));
            Assert.AreEqual(false, f.Test("antes"));
            Assert.AreEqual(true, f.Test("abec"));

            var filter2 = new List<string> {"**"};
            var f2 = PathFilter.GenerateFilter(filter2);
            Assert.AreEqual(true, f2.Test("anything/should.go"));
            Assert.AreEqual(true, f2.Test("literally/anything/should.go"));
        }

        [Test]
        public void Svent_Gitignore_Test()
        {
            string srcPath = Path.Combine(Environment.CurrentDirectory, "../../../../../test/gitignore");
            List<string> src = new List<string> {"README.md", ".git", ".git/**", ".gitignore"};
            // Faulty src... Fucked test vector
            // jslib file: wasn't included, and inversion only works on included
            // findthis file: was reincluded with an inversion
            src.AddRange(new[] {"**/jslib.min.js", "**/findthis.log"});
            PathFilter.Filter absoluteIgnore =
                PathFilter.GenerateFilter(src);
            PathFilter.Filter gitignore =
                PathFilter.GenerateFilter(FilterIgnoreComments(File.ReadLines(Path.Combine(srcPath, ".gitignore"))),
                    true);
            TestFiles(srcPath, absoluteIgnore, gitignore, "foo: OK", "foo: FAIL");
        }

        private static IEnumerable<string> FilterIgnoreComments(IEnumerable<string> enumerable) =>
            enumerable.Where(str => !str.StartsWith("#"));

        private static void TestFiles(string input, PathFilter.Filter absoluteIgnore, PathFilter.Filter gitignore,
            string matchOk, string matchFail)
        {
            var dQueue = new Queue<string>();
            if (Directory.Exists(input))
                dQueue.Enqueue(input);
            else if (File.Exists(input))
                throw new ApplicationException($"Expected {input} to be directory");
            else
                throw new FileNotFoundException(default, input);
            while (dQueue.Count != 0)
            {
                string curDir = dQueue.Dequeue();
                if (!Directory.Exists(curDir)) continue;
                foreach (string file in Directory.EnumerateFiles(curDir))
                {
                    string rfile = Path.GetRelativePath(input, file);
                    if (absoluteIgnore.Test(rfile)) continue;
                    string text = File.ReadAllText(file).Trim();
                    if (text == matchOk)
                    {
                        Assert.AreEqual(false, gitignore.Test(rfile),
                            $"Expected gitignore to not ignore for file {rfile}");
                    }
                    else if (text == matchFail)
                    {
                        Assert.AreEqual(true, gitignore.Test(rfile),
                            $"Expected gitignore to ignore for file {rfile}");
                    }
                    else throw new ApplicationException($"Mismatch in expected values, got {text} in file {rfile}");
                }

                foreach (string folder in Directory.EnumerateDirectories(curDir))
                {
                    if (File.Exists(Path.Combine(folder, ".gitignore")))
                    {
                        // Support recursion - another gitignore (not fixing with stack because no)
                        TestFiles(folder, absoluteIgnore,
                            PathFilter.GenerateFilter(
                                FilterIgnoreComments(File.ReadLines(Path.Combine(folder, ".gitignore"))), true),
                            matchOk, matchFail);
                    }
                    else
                        dQueue.Enqueue(folder);
                }
            }
        }
    }
}
