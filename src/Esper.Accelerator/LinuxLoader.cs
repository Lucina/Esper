using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Esper.Accelerator
{
    internal class LinuxLoader : IAccelerateLoader
    {
        [DllImport("libdl.so.2")]
        private static extern IntPtr dlopen(string path, int flags);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlerror();

        [DllImport("libdl.so.2")]
        private static extern int dlclose(IntPtr handle);

        private static string? DlError()
        {
            return Marshal.PtrToStringAuto(dlerror());
        }

        public IntPtr LoadLibrary(string dll, string? basePath, string? version)
        {
            dll = version != null ? $"lib{dll}.so.{version}" : $"lib{dll}.so";

            if (basePath != null)
                dll = System.IO.Path.Combine(basePath, dll);
            var handle = dlopen(dll, 1);
            if (handle == IntPtr.Zero)
                throw new Win32Exception(DlError());
            return handle;
        }

        public IntPtr GetProcAddress(IntPtr dll, string proc)
        {
            var ptr = dlsym(dll, proc);
            if (ptr == IntPtr.Zero)
                throw new Win32Exception(DlError());
            return ptr;
        }

        public void UnloadLibrary(IntPtr dll)
        {
            int res = dlclose(dll);
            if (res != 0)
                throw new Win32Exception(DlError());
        }
    }
}
