using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Esper.Accelerator
{
    internal class Win32Loader : IAccelerateLoader
    {
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpszLib);

        [DllImport("kernel32", EntryPoint = "FreeLibrary", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool UnloadLibrary(IntPtr hModule);

        IntPtr IAccelerateLoader.LoadLibrary(string basePath, string dll, string version)
        {
            dll = $"{dll}.dll";
            if (basePath != null)
                dll = System.IO.Path.Combine(basePath, dll);
            var handle = LoadLibrary(dll);
            if (handle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return handle;
        }

        IntPtr IAccelerateLoader.GetProcAddress(IntPtr dll, string proc)
        {
            var ptr = GetProcAddress(dll, proc);
            if (ptr == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return ptr;
        }

        void IAccelerateLoader.UnloadLibrary(IntPtr dll)
        {
            bool success = UnloadLibrary(dll);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
