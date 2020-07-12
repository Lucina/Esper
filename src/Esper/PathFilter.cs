using System;
using System.Collections.Generic;
using System.Linq;

namespace Esper
{
    /// <summary>
    /// Path filters
    /// </summary>
    public static class PathFilter
    {
        /// <summary>
        /// Generate filter for filter strings
        /// </summary>
        /// <param name="filterStrings"></param>
        /// <returns>Filter</returns>
        public static Filter GenerateFilter(IEnumerable<string> filterStrings)
        {
            var filters = new List<Func<ReadOnlyMemory<string>, FilterType>>();
            foreach (string add in filterStrings)
            {
                // Get any leading ! to ignore for main filters
                int sc = 0;
                foreach (char c in add)
                    if (c == '!')
                        sc++;
                    else
                        break;
                string xAdd = sc == 0 ? add : add.Substring(sc);
                if (string.IsNullOrWhiteSpace(xAdd)) continue; // Blank filter - ignore
                var xAddSplit = xAdd.Split('/', '\\');
                Func<ReadOnlyMemory<string>, FilterType> func = F_None; // Default deny
                for (int i = xAddSplit.Length - 1; i >= 0; i--)
                {
                    switch (xAddSplit[i])
                    {
                        case "**":
                        {
                            // Make sure any sequential * / ** are handled together
                            int j = i - 1;
                            while (j >= 0 && (xAddSplit[j] == "**" || xAddSplit[j] == "*"))
                                j--;
                            // Any if at top of chain, otherwise existing
                            func = F_DeepAny(i == xAddSplit.Length - 1 ? F_Any : func);
                            i = j + 1;
                            break;
                        }
                        case "*":
                        {
                            // Make sure any sequential * / ** are handled together
                            int j = i - 1;
                            while (j >= 0 && (xAddSplit[j] == "**" || xAddSplit[j] == "*"))
                                j--;
                            // If only one *, single any, otherwise [any if at top of chain, otherwise existing]
                            func = j == i - 1 ? F_Any(func) : F_DeepAny(i == xAddSplit.Length - 1 ? F_Any : func);
                            i = j + 1;
                            break;
                        }
                        default:
                            func = F_Match(xAddSplit[i], func);
                            // Simple regex match
                            break;
                    }
                }

                // If odd number of ! is prefixed, invert filter
                if (sc % 2 == 1)
                    func = F_Invert(func);
                filters.Add(func);
            }

            return new Filter {Filters = filters};
        }

        private static FilterType F_Any(ReadOnlyMemory<string> arg) => FilterType.Affirm;

        private static FilterType F_None(ReadOnlyMemory<string> arg) => FilterType.NoMatch;

        private static Func<ReadOnlyMemory<string>, FilterType>
            F_Match(string pattern, Func<ReadOnlyMemory<string>, FilterType> after)
        {
            //var altMatch = $"^{pattern.Replace("*", @"\S+")}$";
            // #FrontierSetter
            int sCount1 = 0;
            int sLast = -1;
            if (pattern[0] == '*')
                sCount1++;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == '*') continue;
                if (sLast + 1 != i)
                    sCount1++;
                sLast = i;
            }

            if (sLast != pattern.Length - 1)
                sCount1++;

            if (sCount1 == 0)
                return path => string.Equals(path.Span[0], pattern, StringComparison.InvariantCulture)
                    ? path.Length > 1 ? after.Invoke(path.Slice(1)) : FilterType.Affirm
                    : FilterType.NoMatch;

            var info = new int[sCount1 * 2];

            int idx1 = 0;
            sLast = -1;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == '*') continue;
                if (sLast + 1 != i)
                {
                    idx1++;
                    info[idx1 * 2] = i;
                }

                sLast = i;
                info[idx1 * 2 + 1]++;
            }

            return path =>
            {
                var strSpan = path.Span[0].AsSpan();
                var patternSpan = pattern.AsSpan();
                int idx = 0;
                int sLoc = 0;
                int sCount = info.Length / 2;
                while (true)
                {
                    int pos = FixedIndexOf(strSpan.Slice(sLoc), patternSpan.Slice(info[idx * 2], info[idx * 2 + 1]));
                    if (pos == -1) return FilterType.NoMatch;
                    if (idx == sCount - 1)
                        return info[idx * 2] + info[idx * 2 + 1] != patternSpan.Length ||
                               pos + info[idx * 2 + 1] == strSpan.Length
                            ? path.Length > 1 ? after.Invoke(path.Slice(1)) : FilterType.Affirm
                            : FilterType.NoMatch;
                    sLoc += info[idx * 2 + 1];
                    idx++;
                }
            };
        }

        private static int FixedIndexOf(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
        {
            if (pattern.Length == 0) return 0;
            if (text.Length == 0) return -1;
            int baseIdx = 0;
            int curTextIdx = 0;
            int curPatternIdx = 0;
            while (curTextIdx < text.Length)
            {
                bool keep = true;
                char c = pattern[curPatternIdx];
                switch (c)
                {
                    case '?':
                        curTextIdx++;
                        curPatternIdx++;
                        break;
                    case '[':
                        int eLoc = pattern.Slice(curPatternIdx).IndexOf(']'); // No escaping concept
                        if (eLoc == -1) throw new ApplicationException("Missing end sqbracket");
                        eLoc--; // Now count of elements
                        if (pattern.Slice(curPatternIdx + 1, eLoc).IndexOf(text[curPatternIdx]) != -1)
                        {
                            curTextIdx++;
                            curPatternIdx += eLoc + 2;
                        }
                        else
                            keep = false;

                        break;
                    default:
                        if (c == text[curTextIdx])
                        {
                            curTextIdx++;
                            curPatternIdx++;
                        }
                        else
                            keep = false;

                        break;
                }

                if (curPatternIdx == pattern.Length) return baseIdx;

                if (keep) continue;

                baseIdx++;
                if (baseIdx >= text.Length) return -1;
                curTextIdx = baseIdx;
                curPatternIdx = 0;
            }

            return -1;
        }

        private static Func<ReadOnlyMemory<string>, FilterType> F_Any(Func<ReadOnlyMemory<string>, FilterType> after) =>
            path =>
            {
                if (path.Length == 1) return FilterType.Affirm;
                return after.Invoke(path.Slice(1)) == FilterType.Affirm ? FilterType.Affirm : FilterType.NoMatch;
            };

        private static Func<ReadOnlyMemory<string>, FilterType> F_DeepAny(
            Func<ReadOnlyMemory<string>, FilterType> after) => path =>
        {
            for (int i = path.Length - 1; i >= 0; i--)
                if (after.Invoke(path.Slice(i)) == FilterType.Affirm)
                    return FilterType.Affirm;
            return FilterType.NoMatch;
        };

        private static Func<ReadOnlyMemory<string>, FilterType> F_Invert(
            Func<ReadOnlyMemory<string>, FilterType> filter) => path =>
            filter.Invoke(path) switch
            {
                FilterType.Affirm => FilterType.Deny,
                FilterType.Deny => FilterType.Affirm,
                _ => FilterType.NoMatch
            };

        /// <summary>
        /// Filter result type
        /// </summary>
        internal enum FilterType
        {
            /// <summary>
            /// Affirmation from current filter
            /// </summary>
            Affirm,

            /// <summary>
            /// Not matched from current filter
            /// </summary>
            NoMatch,

            /// <summary>
            /// Denial from current filter (should only be for root level filter)
            /// </summary>
            Deny
        }

        /// <summary>
        /// Path filter
        /// </summary>
        public class Filter
        {
            /// <summary>
            /// Path filters
            /// </summary>
            internal List<Func<ReadOnlyMemory<string>, FilterType>> Filters;

            /// <summary>
            /// Test relative path for inclusion
            /// </summary>
            /// <param name="path">Path to test</param>
            /// <returns>True if includes</returns>
            public bool Test(string path)
            {
                var split = path.Split('/', '\\');
                return Filters.Aggregate(false, (current, filter) => filter.Invoke(split) switch
                {
                    FilterType.Affirm => true,
                    FilterType.Deny => false,
                    _ => current
                });
            }
        }
    }
}
