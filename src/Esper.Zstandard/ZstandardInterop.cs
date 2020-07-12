using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Esper.Accelerator;

namespace Esper.Zstandard
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class ZstandardInterop
    {
        private const string LibName = "zstd";
        private const string LibVersion = "1.4.4";

        private static readonly AccelerateContext _accel = new AccelerateContext(LibName, LibVersion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Buffer
        {
            public IntPtr Data;
            public UIntPtr Size;
            public UIntPtr Position;
        }

        public static void ThrowIfError(UIntPtr code)
        {
            if (ZSTD_isError(code))
            {
                var errorPtr = ZSTD_getErrorName(code);
                string errorMsg = Marshal.PtrToStringAnsi(errorPtr);
                throw new IOException(errorMsg);
            }
        }

        //-----------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint ZSTD_versionNumberDelegate();

        public static readonly ZSTD_versionNumberDelegate ZSTD_versionNumber =
            _accel.This<ZSTD_versionNumberDelegate>("ZSTD_versionNumber");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ZSTD_maxCLevelDelegate();

        public static readonly ZSTD_maxCLevelDelegate ZSTD_maxCLevel =
            _accel.This<ZSTD_maxCLevelDelegate>("ZSTD_maxCLevel");

        //-----------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr ZSTD_createCStreamDelegate();

        public static readonly ZSTD_createCStreamDelegate ZSTD_createCStream =
            _accel.This<ZSTD_createCStreamDelegate>("ZSTD_createCStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_initCStreamDelegate(IntPtr zcs, int compressionLevel);

        public static readonly ZSTD_initCStreamDelegate ZSTD_initCStream =
            _accel.This<ZSTD_initCStreamDelegate>("ZSTD_initCStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_freeCStreamDelegate(IntPtr zcs);

        public static readonly ZSTD_freeCStreamDelegate ZSTD_freeCStream =
            _accel.This<ZSTD_freeCStreamDelegate>("ZSTD_freeCStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_CStreamInSizeDelegate();

        public static readonly ZSTD_CStreamInSizeDelegate ZSTD_CStreamInSize =
            _accel.This<ZSTD_CStreamInSizeDelegate>("ZSTD_CStreamInSize");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_CStreamOutSizeDelegate();

        public static readonly ZSTD_CStreamOutSizeDelegate ZSTD_CStreamOutSize =
            _accel.This<ZSTD_CStreamOutSizeDelegate>("ZSTD_CStreamOutSize");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate UIntPtr ZSTD_compressStreamDelegate(IntPtr zcs,
            Buffer* outputBuffer,
            Buffer* inputBuffer);

        public static readonly ZSTD_compressStreamDelegate ZSTD_compressStream =
            _accel.This<ZSTD_compressStreamDelegate>("ZSTD_compressStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr ZSTD_createCDictDelegate(IntPtr dictBuffer, UIntPtr dictSize, int compressionLevel);

        public static readonly ZSTD_createCDictDelegate ZSTD_createCDict =
            _accel.This<ZSTD_createCDictDelegate>("ZSTD_createCDict");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_freeCDictDelegate(IntPtr cdict);

        public static readonly ZSTD_freeCDictDelegate ZSTD_freeCDict =
            _accel.This<ZSTD_freeCDictDelegate>("ZSTD_freeCDict");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_initCStream_usingCDictDelegate(IntPtr zcs, IntPtr cdict);

        public static readonly ZSTD_initCStream_usingCDictDelegate ZSTD_initCStream_usingCDict =
            _accel.This<ZSTD_initCStream_usingCDictDelegate>("ZSTD_initCStream_usingCDict");

        //-----------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr ZSTD_createDStreamDelegate();

        public static readonly ZSTD_createDStreamDelegate ZSTD_createDStream =
            _accel.This<ZSTD_createDStreamDelegate>("ZSTD_createDStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_initDStreamDelegate(IntPtr zds);

        public static readonly ZSTD_initDStreamDelegate ZSTD_initDStream =
            _accel.This<ZSTD_initDStreamDelegate>("ZSTD_initDStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_freeDStreamDelegate(IntPtr zds);

        public static readonly ZSTD_freeDStreamDelegate ZSTD_freeDStream =
            _accel.This<ZSTD_freeDStreamDelegate>("ZSTD_freeDStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_DStreamInSizeDelegate();

        public static readonly ZSTD_DStreamInSizeDelegate ZSTD_DStreamInSize =
            _accel.This<ZSTD_DStreamInSizeDelegate>("ZSTD_DStreamInSize");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_DStreamOutSizeDelegate();

        public static readonly ZSTD_DStreamOutSizeDelegate ZSTD_DStreamOutSize =
            _accel.This<ZSTD_DStreamOutSizeDelegate>("ZSTD_DStreamOutSize");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate UIntPtr ZSTD_decompressStreamDelegate(IntPtr zds,
            Buffer* outputBuffer,
            Buffer* inputBuffer);

        public static readonly ZSTD_decompressStreamDelegate ZSTD_decompressStream =
            _accel.This<ZSTD_decompressStreamDelegate>("ZSTD_decompressStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr ZSTD_createDDictDelegate(IntPtr dictBuffer, UIntPtr dictSize);

        public static readonly ZSTD_createDDictDelegate ZSTD_createDDict =
            _accel.This<ZSTD_createDDictDelegate>("ZSTD_createDDict");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_freeDDictDelegate(IntPtr ddict);

        public static readonly ZSTD_freeDDictDelegate ZSTD_freeDDict =
            _accel.This<ZSTD_freeDDictDelegate>("ZSTD_freeDDict");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UIntPtr ZSTD_initDStream_usingDDictDelegate(IntPtr zds, IntPtr ddict);

        public static readonly ZSTD_initDStream_usingDDictDelegate ZSTD_initDStream_usingDDict =
            _accel.This<ZSTD_initDStream_usingDDictDelegate>("ZSTD_initDStream_usingDDict");

        //-----------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate UIntPtr ZSTD_flushStreamDelegate(IntPtr zcs,
            Buffer* outputBuffer);

        public static readonly ZSTD_flushStreamDelegate ZSTD_flushStream =
            _accel.This<ZSTD_flushStreamDelegate>("ZSTD_flushStream");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate UIntPtr ZSTD_endStreamDelegate(IntPtr zcs,
            Buffer* outputBuffer);

        public static readonly ZSTD_endStreamDelegate ZSTD_endStream =
            _accel.This<ZSTD_endStreamDelegate>("ZSTD_endStream");

        //-----------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool ZSTD_isErrorDelegate(UIntPtr code);

        public static readonly ZSTD_isErrorDelegate ZSTD_isError =
            _accel.This<ZSTD_isErrorDelegate>("ZSTD_isError");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr ZSTD_getErrorNameDelegate(UIntPtr code);

        public static readonly ZSTD_getErrorNameDelegate ZSTD_getErrorName =
            _accel.This<ZSTD_getErrorNameDelegate>("ZSTD_getErrorName");
    }
}
