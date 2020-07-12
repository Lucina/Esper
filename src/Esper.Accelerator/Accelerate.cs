using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Esper.Accelerator
{
    /// <summary>
    /// Utility class to load native libraries
    /// </summary>
    public static class Accelerate
    {
        /// <summary>
        /// Default loader platform
        /// </summary>
        public static AcceleratePlatform DefaultPlatform { get; set; }

        /// <summary>
        /// Default library path
        /// </summary>
        public static string DefaultPath { get; set; }

        internal static readonly Dictionary<AcceleratePlatform, IAccelerateLoader> Loaders;

        static Accelerate()
        {
            Loaders = new Dictionary<AcceleratePlatform, IAccelerateLoader>();
            DefaultPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ??
                          throw new ApplicationException();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DefaultPlatform = Environment.Is64BitProcess
                    ? AcceleratePlatform.WindowsX64
                    : AcceleratePlatform.WindowsX86;
                DefaultPath = Path.Combine(DefaultPath, Environment.Is64BitProcess ? "win_x64" : "win_x86");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                DefaultPlatform = AcceleratePlatform.MacosX64;
                DefaultPath = Path.Combine(DefaultPath, "osx_x64");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DefaultPlatform = AcceleratePlatform.LinuxX64;
                DefaultPath = Path.Combine(DefaultPath, "linux_x64");
            }
        }

        /// <summary>
        /// Get accelerator
        /// </summary>
        /// <param name="platform">Loader platform</param>
        /// <returns>Accelerate loader</returns>
        /// <exception cref="ArgumentOutOfRangeException">If platform is invalid for enum or if both <paramref name="platform"/> and <see cref="DefaultPlatform"/> are <see cref="AcceleratePlatform.Default"/></exception>
        public static IAccelerateLoader This(AcceleratePlatform platform = AcceleratePlatform.Default)
        {
            if (platform == AcceleratePlatform.Default)
                platform = DefaultPlatform;
            if (!Loaders.TryGetValue(platform, out var value))
            {
                switch (platform)
                {
                    case AcceleratePlatform.MacosX64:
                        value = new MacosLoader();
                        break;
                    case AcceleratePlatform.WindowsX64:
                        value = new Win32Loader();
                        break;
                    case AcceleratePlatform.LinuxX64:
                        value = new LinuxLoader();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
                }

                Loaders[platform] = value;
            }

            return value;
        }

        /// <summary>
        /// Load library
        /// </summary>
        /// <param name="dll">Library to load</param>
        /// <param name="version">Version to load</param>
        /// <param name="platform">Loader platform</param>
        /// <param name="basePath">Base library path or null for default</param>
        /// <returns>Pointer to loaded library</returns>
        /// <exception cref="ArgumentOutOfRangeException">If platform is invalid for enum or if both <paramref name="platform"/> and <see cref="DefaultPlatform"/> are <see cref="AcceleratePlatform.Default"/></exception>
        public static IntPtr This(string dll, string version = null,
            AcceleratePlatform platform = AcceleratePlatform.Default, string basePath = null)
        {
            if (basePath == null)
                basePath = DefaultPath;
            return This(platform).LoadLibrary(basePath, dll, version);
        }

        /// <summary>
        /// Load library
        /// </summary>
        /// <param name="customLoader">Custom loader</param>
        /// <param name="dll">Library to load</param>
        /// <param name="version">Version to load</param>
        /// <param name="basePath">Base library path or null for default</param>
        /// <returns>Pointer to loaded library</returns>
        public static IntPtr This(IAccelerateLoader customLoader, string dll, string version = null,
            string basePath = null)
        {
            if (basePath == null)
                basePath = DefaultPath;
            return customLoader.LoadLibrary(basePath, dll, version);
        }

        /// <summary>
        /// Load procedure
        /// </summary>
        /// <param name="lib">Library pointer</param>
        /// <param name="proc">Procedure name</param>
        /// <param name="platform">Loader platform</param>
        /// <returns>Pointer to loaded procedure</returns>
        /// <exception cref="ArgumentOutOfRangeException">If platform is invalid for enum or if both <paramref name="platform"/> and <see cref="DefaultPlatform"/> are <see cref="AcceleratePlatform.Default"/></exception>
        public static IntPtr This(IntPtr lib, string proc, AcceleratePlatform platform = AcceleratePlatform.Default)
        {
            return This(platform).GetProcAddress(lib, proc);
        }

        /// <summary>
        /// Load procedure
        /// </summary>
        /// <param name="customLoader">Custom loader</param>
        /// <param name="lib">Library pointer</param>
        /// <param name="proc">Procedure name</param>
        /// <returns>Pointer to loaded procedure</returns>
        public static IntPtr This(IAccelerateLoader customLoader, IntPtr lib, string proc)
        {
            return customLoader.GetProcAddress(lib, proc);
        }

        /// <summary>
        /// Get delegate
        /// </summary>
        /// <param name="lib">Library pointer</param>
        /// <param name="proc">Procedure name</param>
        /// <param name="platform">Loader platform</param>
        /// <typeparam name="T">Delegate type</typeparam>
        /// <returns>Delegate for procedure</returns>
        public static T This<T>(IntPtr lib, string proc, AcceleratePlatform platform = AcceleratePlatform.Default)
            where T : Delegate
        {
            return Marshal.GetDelegateForFunctionPointer<T>(This(lib, proc, platform));
        }

        /// <summary>
        /// Get delegate
        /// </summary>
        /// <param name="customLoader">Custom loader</param>
        /// <param name="lib">Library pointer</param>
        /// <param name="proc">Procedure name</param>
        /// <typeparam name="T">Delegate type</typeparam>
        /// <returns>Delegate for procedure</returns>
        public static T This<T>(IAccelerateLoader customLoader, IntPtr lib, string proc) where T : Delegate
        {
            return Marshal.GetDelegateForFunctionPointer<T>(This(customLoader, lib, proc));
        }
    }

    /// <summary>
    /// Utility class to load native libraries
    /// </summary>
    public static class Decelerate
    {
        /// <summary>
        /// Unload library
        /// </summary>
        /// <param name="lib">Library pointer</param>
        /// <param name="platform">Loader platform</param>
        public static void This(IntPtr lib, AcceleratePlatform platform = AcceleratePlatform.Default)
        {
            Accelerate.This(platform).UnloadLibrary(lib);
        }

        /// <summary>
        /// Unload library
        /// </summary>
        /// <param name="customLoader">Custom loader</param>
        /// <param name="lib">Library pointer</param>
        public static void This(IAccelerateLoader customLoader, IntPtr lib)
        {
            customLoader.UnloadLibrary(lib);
        }
    }
}
