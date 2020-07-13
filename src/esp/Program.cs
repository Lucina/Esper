using System;
using System.Collections.Generic;

namespace esp {
    internal static class Program {
        internal const string ProgramName = "esp";
        private const string UsageBase = "Usage:";

        private const string UsageMain =
            ProgramName + " <utility> <verb> [options]";

        private static readonly Dictionary<string, Func<ArraySegment<string>, ArraySegment<string>, int>> Utilities =
            new Dictionary<string, Func<ArraySegment<string>, ArraySegment<string>, int>> {
                {MwUtility.UtilityName, MwUtility.Operate}
            };

        private static int Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine(UsageBase);
                Console.WriteLine(UsageMain);
                Console.WriteLine("Available utilities:");
                foreach (string utility in Utilities.Keys)
                    Console.WriteLine(utility);
                return 1;
            }

            if (!Utilities.TryGetValue(args[0].ToLowerInvariant(), out var func))
                func = UnknownUtilityHandler;
            return func.Invoke(new ArraySegment<string>(args, 0, 1),
                new ArraySegment<string>(args, 1, args.Length - 1));
        }

        private static int UnknownUtilityHandler(ArraySegment<string> processed, ArraySegment<string> args) {
            Console.WriteLine($"Unknown utility {processed[0]}");
            Console.WriteLine(UsageBase);
            Console.WriteLine(UsageMain);
            Console.WriteLine("Available utilities:");
            foreach (string utility in Utilities.Keys)
                Console.WriteLine(utility);
            return 2;
        }
    }
}