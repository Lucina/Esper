using System;

namespace Esper.Accelerator
{
    /// <summary>
    /// Loader
    /// </summary>
    public interface IAccelerateLoader
    {
        /// <summary>
        /// Load library
        /// </summary>
        /// <param name="basePath">Filesystem base path</param>
        /// <param name="dll">Library name</param>
        /// <param name="version">Library version</param>
        /// <returns>Pointer to loaded library</returns>
        IntPtr LoadLibrary(string basePath, string dll, string version);

        /// <summary>
        /// Get procedure address
        /// </summary>
        /// <param name="dll">Library pointer</param>
        /// <param name="proc">Procedure name</param>
        /// <returns>Pointer to procedure</returns>
        IntPtr GetProcAddress(IntPtr dll, string proc);

        /// <summary>
        /// Unload library
        /// </summary>
        /// <param name="dll">Library pointer</param>
        void UnloadLibrary(IntPtr dll);
    }
}
