namespace Esper.Accelerator
{
    /// <summary>
    /// Enumerates standard platforms with loaders
    /// </summary>
    public enum AcceleratePlatform
    {
        /// <summary>
        /// Auto-select from current defaults
        /// </summary>
        Default,

        /// <summary>
        /// macOS x64 (osx-x64)
        /// </summary>
        MacosX64,

        /// <summary>
        /// Windows x64 (win-x64)
        /// </summary>
        WindowsX64,

        /// <summary>
        /// Windows x86 (win-x86)
        /// </summary>
        WindowsX86,

        /// <summary>
        /// Linux x64 (linux-x64)
        /// </summary>
        LinuxX64,

        /// <summary>
        /// Unknown platform - uses <see cref="Accelerate.GlobalUnknownPlatformLoader"/>
        /// </summary>
        UnknownPlatform
    }
}
