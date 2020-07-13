using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Esper;
using Esper.Misaka;
using YamlDotNet.RepresentationModel;

namespace esp
{
    internal static class MwUtility
    {
        internal const string UtilityName = "mw";
        private const string UsageBase = "Usage:";

        private const string UsageVerb =
            Program.ProgramName + " " + UtilityName + " <verb> [options]";

        private const string MwExtension = ".mw";
        private const string LoExtension = ".lo";

        private static readonly Dictionary<string, Func<ArraySegment<string>, ArraySegment<string>, int>> Verbs =
            new Dictionary<string, Func<ArraySegment<string>, ArraySegment<string>, int>>
            {
                {NameVerbPack, VerbPack}, {NameVerbUnpack, VerbUnpack}, {NameVerbAlloctest, VerbAlloctest}
            };

        internal static int Operate(ArraySegment<string> processed, ArraySegment<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine(UsageBase);
                Console.WriteLine(UsageVerb);
                Console.WriteLine("Available verbs:");
                foreach (string verb in Verbs.Keys)
                    Console.WriteLine(verb);
                return 1;
            }

            if (!Verbs.TryGetValue(args[0].ToLowerInvariant(), out var func))
                func = UnknownVerbHandler;
            return func.Invoke(args.Slice(0, 1), args.Slice(1, args.Count - 1));
        }

        private static int UnknownVerbHandler(ArraySegment<string> processed, ArraySegment<string> args)
        {
            Console.WriteLine($"Unknown verb {processed[0]}");
            Console.WriteLine(UsageBase);
            Console.WriteLine(UsageVerb);
            Console.WriteLine("Available verbs:");
            foreach (string verb in Verbs.Keys)
                Console.WriteLine(verb);
            return 2;
        }

        private const string NameVerbPack = "pack";

        private const string UsageVerbPack =
            Program.ProgramName + " " + UtilityName + " " + NameVerbPack + " <filterfile> <source> <target>";

        private static int VerbPack(ArraySegment<string> processed, ArraySegment<string> args)
        {
            if (args.Count < 3)
            {
                Console.WriteLine(UsageBase);
                Console.WriteLine(UsageVerbPack);
                return 3;
            }

            Directory.CreateDirectory(args[2]);

            var hasher = new Blake2BHashAlgorithm();

            using var filterFs = new FileStream(args[0], FileMode.Open, FileAccess.Read);

            var baseFiles = new List<(string path, string fullPath, long length)>(FlatFilesystemReader(args[1]));

            foreach ((string id, IEnumerable<string> filterSrc) in LoadFilterStreamYaml(filterFs))
            {
                using var outputFs = new FileStream(Path.Combine(args[2], $"{id}{MwExtension}"), FileMode.CreateNew,
                    FileAccess.ReadWrite);

                var filter = PathFilter.GenerateFilter(filterSrc);

                var files = new List<(string path, string fullPath, long length)>();
                foreach (var file in baseFiles)
                    if (filter.Test(file.path))
                        files.Add(file);
                long totalSize = files.Sum(f => f.length);
                long processedSize = 0L;
                DataSizes.GetSize(totalSize, out double totalV, out string totalU);
                string totalSizeStr = $"{totalV:N3}{totalU}";

                var fileHashes = new long[files.Count];
                var fileHashedSet = new HashSet<string>();

                // TODO enhancement proc
                /*
                 * Copy offsets except when dist is smaller than threshold1 or file bigger than threshold2
                 * If file fills more than threshold3 blocks, enable hash lookback for replacement
                 * Post-processing: create patch info based on block hashes
                 */

                var locations = Worst.WriteData(
                    files.Select<(string path, string fullPath, long length), Func<(Stream, bool, int?)>>(
                        (x, y)
                            => () =>
                            {
                                var fs = new FileStream(x.fullPath, FileMode.Open, FileAccess.Read);
                                if (fileHashedSet.Add(x.path))
                                {
                                    DataSizes.GetSize(processedSize += x.length, out double size, out string unit);
                                    DataSizes.GetSize(x.length, out double size2, out string unit2);
                                    Console.WriteLine(
                                        $"[{size2:N3}{unit2} ({size:N3}{unit}) / {totalSizeStr}] {x.path}");
                                    long hash = MemoryMarshal.Read<long>(hasher.ComputeHash(fs));
                                    fileHashes[y] = BitConverter.IsLittleEndian
                                        ? BinaryPrimitives.ReverseEndianness(hash)
                                        : hash;
                                    fs.Position = 0;
                                }

                                return (fs, false, null);
                            }),
                    outputFs, false, out var blockList, true, out var blockHashList
                );

                var sortedDict = new SortedDictionary<string, Location>(StringComparer.InvariantCultureIgnoreCase);
                for (int i = 0; i < files.Count; i++)
                    sortedDict.Add(files[i].path, locations[i]);

                Worst.WriteWrapper(outputFs, blockList.ToArray(), sortedDict);

                using var outputFs2 =
                    new FileStream(Path.Combine(args[2], $"{id}{LoExtension}"), FileMode.CreateNew, FileAccess.Write);
                LastOrder.WriteLastOrder(outputFs2, blockHashList!.ToArray(), fileHashes,
                    locations.ToArray());
            }

            return 0;
        }

        private static IEnumerable<(string path, string fullPath, long length)>
            FlatFilesystemReader(string sourceDir)
        {
            var dQueue = new Queue<string>();
            //var fQueue = new Queue<(string, long)>();
            var fInfo = new FileInfo(sourceDir);
            if (fInfo.Exists)
            {
                string? path = Path.GetFileName(sourceDir);
                yield return (path, Path.Combine(sourceDir, path), fInfo.Length);
                yield break;
            }

            dQueue.Enqueue(string.Empty);

            while (dQueue.Count != 0)
            {
                string src = dQueue.Dequeue();
                string curDir = Path.Combine(sourceDir, src);
                if (!Directory.Exists(curDir)) continue;
                foreach (string file in Directory.EnumerateFiles(curDir))
                {
                    string path = Path.Combine(src, Path.GetFileName(file));
                    string fullPath = Path.Combine(sourceDir, path);
                    yield return (path, fullPath, new FileInfo(fullPath).Length);
                }

                foreach (string folder in Directory.EnumerateDirectories(curDir))
                    dQueue.Enqueue(Path.Combine(src, Path.GetFileName(folder)));
            }
        }

        private static IEnumerable<(string id, IEnumerable<string> filters)> LoadFilterStreamYaml(Stream stream)
        {
            var yaml = new YamlStream();
            using var reader = new StreamReader(stream);
            yaml.Load(reader);
            if (!(yaml.Documents[0].RootNode is YamlSequenceNode mapping)) yield break;
            foreach (var entry in mapping.Children)
            {
                var eNode = (YamlMappingNode)entry;
                if (!eNode.Children.TryGetValue(new YamlScalarNode("id"), out var idNode)) continue;
                if (!(idNode is YamlScalarNode idScalarNode)) continue;
                if (!eNode.Children.TryGetValue(new YamlScalarNode("filters"), out var filterNode)) continue;
                if (!(filterNode is YamlSequenceNode filterSequenceNode)) continue;
                yield return (idScalarNode.Value ?? throw new ApplicationException("Empty scalar node???"),
                    LoadFilterListYaml(filterSequenceNode));
            }
        }

        private static IEnumerable<string> LoadFilterListYaml(YamlSequenceNode sequence)
        {
            foreach (var node in sequence.Children)
                if (node is YamlScalarNode scalarNode)
                    yield return scalarNode.Value ?? throw new ApplicationException("Empty scalar node???");
        }

        private const string NameVerbUnpack = "unpack";

        private const string UsageVerbUnpack =
            Program.ProgramName + " " + UtilityName + " " + NameVerbUnpack + " <mwfile> <target>";

        private static int VerbUnpack(ArraySegment<string> processed, ArraySegment<string> args)
        {
            if (args.Count < 2)
            {
                Console.WriteLine(UsageBase);
                Console.WriteLine(UsageVerbUnpack);
                return 3;
            }

            using var fs = new FileStream(args[0], FileMode.Open, FileAccess.Read);
            var mw = Worst.Read(fs);

            foreach (string name in mw.GetEntries())
            {
                if (!mw.TryGetStream(name, out var stream))
                    throw new ApplicationException($"Failed to obtain stream for entry {name}");
                DataSizes.GetSize(stream!.Length, out double value, out string unit);
                Console.WriteLine($"{value:N3}{unit} {name}");
                string path = Path.Combine(args[1], name);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using var fso = new FileStream(path, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fso);
            }

            return 0;
        }

        private const string NameVerbAlloctest = "alloctest";

        private const string UsageVerbAlloctest =
            Program.ProgramName + " " + UtilityName + " " + NameVerbAlloctest + " <mwfile>";

        private static int VerbAlloctest(ArraySegment<string> processed, ArraySegment<string> args)
        {
            if (args.Count < 1)
            {
                Console.WriteLine(UsageBase);
                Console.WriteLine(UsageVerbAlloctest);
                return 3;
            }

            using var fs = new FileStream(args[0], FileMode.Open, FileAccess.Read);
            var mw = Worst.Read(fs);

            var ms = new MemoryStream();
            foreach (string name in mw.GetEntries())
            {
                if (!mw.TryGetStream(name, out var stream))
                    throw new ApplicationException($"Failed to obtain stream for entry {name}");
                DataSizes.GetSize(stream!.Length, out double value, out string unit);
                Console.WriteLine($"{value:N3}{unit} {name}");
                ms.SetLength(0);
                stream.CopyTo(ms);
            }

            return 0;
        }
    }
}
