using System;
using System.Runtime.InteropServices;

namespace Esper.Accelerator
{
    /// <summary>
    /// Native library context
    /// </summary>
    public class AccelerateContext : IDisposable
    {
        /// <summary>
        /// Library address
        /// </summary>
        public readonly IntPtr Lib;

        private readonly IAccelerateLoader _loader;

        /// <summary>
        /// Create new instance of <see cref="AccelerateContext"/>
        /// </summary>
        /// <param name="dll">Library to load</param>
        /// <param name="version">Version to load</param>
        /// <param name="platform">Loader platform</param>
        /// <param name="basePath">Base library path or null for default</param>
        /// <returns>Pointer to loaded library</returns>
        /// <exception cref="ArgumentOutOfRangeException">If platform is invalid for enum or if both <paramref name="platform"/> and <see cref="Accelerate.DefaultPlatform"/> are <see cref="AcceleratePlatform.Default"/></exception>
        public AccelerateContext(string dll, string? version = null,
            AcceleratePlatform platform = AcceleratePlatform.Default, string? basePath = null)
            : this(Accelerate.This(platform), dll, version, basePath)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="AccelerateContext"/>
        /// </summary>
        /// <param name="customLoader">Custom loader</param>
        /// <param name="dll">Library to load</param>
        /// <param name="version">Version to load</param>
        /// <param name="basePath">Base library path or null for default</param>
        /// <returns>Pointer to loaded library</returns>
        public AccelerateContext(IAccelerateLoader customLoader, string dll, string? version = null,
            string? basePath = null)
        {
            basePath ??= Accelerate.DefaultPath;
            _loader = customLoader;
            Lib = _loader.LoadLibrary(basePath, dll, version);
        }

        /// <summary>
        /// Load procedure
        /// </summary>
        /// <param name="proc">Procedure name</param>
        /// <returns>Pointer to loaded procedure</returns>
        public IntPtr This(string proc)
        {
            return _loader.GetProcAddress(Lib, proc);
        }

        /// <summary>
        /// Get delegate
        /// </summary>
        /// <param name="proc">Procedure name</param>
        /// <typeparam name="T">Delegate type</typeparam>
        /// <returns>Delegate for procedure</returns>
        public T This<T>(string proc) where T : Delegate
        {
            return Marshal.GetDelegateForFunctionPointer<T>(_loader.GetProcAddress(Lib, proc));
        }

        private void ReleaseUnmanagedResources()
        {
            _loader.UnloadLibrary(Lib);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~AccelerateContext()
        {
            ReleaseUnmanagedResources();
        }
    }
}
