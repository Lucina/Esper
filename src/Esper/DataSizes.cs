namespace Esper
{
    /// <summary>
    /// Constants for data sizes
    /// </summary>
    public static class DataSizes
    {
        /// <summary>
        /// Kibibyte
        /// </summary>
        public const int KiB = 1024;

        /// <summary>
        /// Mebibyte
        /// </summary>
        public const int MiB = 1024 * 1024;

        /// <summary>
        /// Gibibyte
        /// </summary>
        public const int GiB = 1024 * 1024 * 1024;

        /// <summary>
        /// Kibibyte
        /// </summary>
        public const long KiBl = 1024;

        /// <summary>
        /// Mebibyte
        /// </summary>
        public const long MiBl = 1024 * 1024;

        /// <summary>
        /// Gibibyte
        /// </summary>
        public const long GiBl = 1024 * 1024 * 1024;

        /// <summary>
        /// Tebibyte
        /// </summary>
        public const long TiBl = 1024L * 1024 * 1024 * 1024;

        /// <summary>
        /// Pebibyte
        /// </summary>
        public const long PiBl = 1024L * 1024 * 1024 * 1024 * 1024;

        /// <summary>
        /// Exbibyte
        /// </summary>
        public const long EiBl = 1024L * 1024 * 1024 * 1024 * 1024 * 1024;

        private static readonly string[] UnitNames = {"B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"};

        /// <summary>
        /// Get size of datum with units
        /// </summary>
        /// <param name="size">Datum size</param>
        /// <param name="value">Value for given units</param>
        /// <param name="unit">Unit (e.g. B, KiB, MiB, etc.)</param>
        public static void GetSize(long size, out double value, out string unit)
        {
            int i = 0;
            long past = 0L;
            long maxPast = 1L;
            while (i < UnitNames.Length && size >= 1024)
            {
                past += size % 1024 * maxPast;
                maxPast *= 1024;
                size /= 1024;
                i++;
            }

            value = size + (double)past / maxPast;
            unit = UnitNames[i];
        }
    }
}
