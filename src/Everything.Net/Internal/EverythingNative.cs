using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Everything.Net.Enums;

namespace Everything.Net.Internal;

internal static class EverythingNative
{
    private const string DllX64 = "Everything64.dll";
    private const string DllArm64 = "EverythingARM64.dll";

    static EverythingNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(EverythingNative).Assembly, ResolveDll);
        _ = EverythingArchitectureHelper.GetCurrentArchitecture();
    }

    private static IntPtr ResolveDll(string libraryName, Assembly? assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, DllX64, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(libraryName, DllArm64, StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        var baseDir = AppContext.BaseDirectory;
        var arch = EverythingArchitectureHelper.GetCurrentArchitecture();
        var rid = arch == EverythingArchitecture.Arm64 ? "win-arm64" : "win-x64";
        var fileName = arch == EverythingArchitecture.Arm64 ? DllArm64 : DllX64;

        var runtimesPath = Path.Combine(baseDir, "runtimes", rid, "native", fileName);
        if (File.Exists(runtimesPath))
            return NativeLibrary.Load(runtimesPath);

        var flatPath = Path.Combine(baseDir, fileName);
        if (File.Exists(flatPath))
            return NativeLibrary.Load(flatPath);

        return IntPtr.Zero;
    }

    [DllImport(DllX64, EntryPoint = "Everything_SetSearchW", CharSet = CharSet.Unicode)]
    private static extern void Everything64_SetSearch(string lpString);

    [DllImport(DllArm64, EntryPoint = "Everything_SetSearchW", CharSet = CharSet.Unicode)]
    private static extern void EverythingArm64_SetSearch(string lpString);

    [DllImport(DllX64, EntryPoint = "Everything_SetRequestFlags")]
    private static extern void Everything64_SetRequestFlags(uint flags);

    [DllImport(DllArm64, EntryPoint = "Everything_SetRequestFlags")]
    private static extern void EverythingArm64_SetRequestFlags(uint flags);

    [DllImport(DllX64, EntryPoint = "Everything_SetSort")]
    private static extern void Everything64_SetSort(uint sortType);

    [DllImport(DllArm64, EntryPoint = "Everything_SetSort")]
    private static extern void EverythingArm64_SetSort(uint sortType);

    [DllImport(DllX64, EntryPoint = "Everything_SetOffset")]
    private static extern void Everything64_SetOffset(uint offset);

    [DllImport(DllArm64, EntryPoint = "Everything_SetOffset")]
    private static extern void EverythingArm64_SetOffset(uint offset);

    [DllImport(DllX64, EntryPoint = "Everything_SetMax")]
    private static extern void Everything64_SetMax(uint max);

    [DllImport(DllArm64, EntryPoint = "Everything_SetMax")]
    private static extern void EverythingArm64_SetMax(uint max);

    [DllImport(DllX64, EntryPoint = "Everything_SetMatchPath")]
    private static extern void Everything64_SetMatchPath(bool enable);

    [DllImport(DllArm64, EntryPoint = "Everything_SetMatchPath")]
    private static extern void EverythingArm64_SetMatchPath(bool enable);

    [DllImport(DllX64, EntryPoint = "Everything_SetMatchCase")]
    private static extern void Everything64_SetMatchCase(bool enable);

    [DllImport(DllArm64, EntryPoint = "Everything_SetMatchCase")]
    private static extern void EverythingArm64_SetMatchCase(bool enable);

    [DllImport(DllX64, EntryPoint = "Everything_SetMatchWholeWord")]
    private static extern void Everything64_SetMatchWholeWord(bool enable);

    [DllImport(DllArm64, EntryPoint = "Everything_SetMatchWholeWord")]
    private static extern void EverythingArm64_SetMatchWholeWord(bool enable);

    [DllImport(DllX64, EntryPoint = "Everything_SetRegex")]
    private static extern void Everything64_SetRegex(bool enable);

    [DllImport(DllArm64, EntryPoint = "Everything_SetRegex")]
    private static extern void EverythingArm64_SetRegex(bool enable);

    [DllImport(DllX64, EntryPoint = "Everything_QueryW", CharSet = CharSet.Unicode)]
    private static extern bool Everything64_Query(bool wait);

    [DllImport(DllArm64, EntryPoint = "Everything_QueryW", CharSet = CharSet.Unicode)]
    private static extern bool EverythingArm64_Query(bool wait);

    [DllImport(DllX64, EntryPoint = "Everything_GetNumResults")]
    private static extern uint Everything64_GetNumResults();

    [DllImport(DllArm64, EntryPoint = "Everything_GetNumResults")]
    private static extern uint EverythingArm64_GetNumResults();

    [DllImport(DllX64, EntryPoint = "Everything_GetTotResults")]
    private static extern uint Everything64_GetTotResults();

    [DllImport(DllArm64, EntryPoint = "Everything_GetTotResults")]
    private static extern uint EverythingArm64_GetTotResults();

    [DllImport(DllX64, EntryPoint = "Everything_GetLastError")]
    private static extern uint Everything64_GetLastError();

    [DllImport(DllArm64, EntryPoint = "Everything_GetLastError")]
    private static extern uint EverythingArm64_GetLastError();

    [DllImport(DllX64, EntryPoint = "Everything_IsDBLoaded")]
    private static extern bool Everything64_IsDbLoaded();

    [DllImport(DllArm64, EntryPoint = "Everything_IsDBLoaded")]
    private static extern bool EverythingArm64_IsDbLoaded();

    [DllImport(DllX64, EntryPoint = "Everything_GetResultFullPathNameW", CharSet = CharSet.Unicode)]
    private static extern uint Everything64_GetResultFullPathName(uint index, StringBuilder buffer, uint size);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultFullPathNameW", CharSet = CharSet.Unicode)]
    private static extern uint EverythingArm64_GetResultFullPathName(uint index, StringBuilder buffer, uint size);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything64_GetResultFileName(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr EverythingArm64_GetResultFileName(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultPathW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything64_GetResultPath(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultPathW", CharSet = CharSet.Unicode)]
    private static extern IntPtr EverythingArm64_GetResultPath(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultExtensionW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything64_GetResultExtension(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultExtensionW", CharSet = CharSet.Unicode)]
    private static extern IntPtr EverythingArm64_GetResultExtension(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultSize")]
    private static extern bool Everything64_GetResultSize(uint index, out long size);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultSize")]
    private static extern bool EverythingArm64_GetResultSize(uint index, out long size);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultDateCreated")]
    private static extern bool Everything64_GetResultDateCreated(uint index, out long fileTime);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultDateCreated")]
    private static extern bool EverythingArm64_GetResultDateCreated(uint index, out long fileTime);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultDateModified")]
    private static extern bool Everything64_GetResultDateModified(uint index, out long fileTime);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultDateModified")]
    private static extern bool EverythingArm64_GetResultDateModified(uint index, out long fileTime);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultDateAccessed")]
    private static extern bool Everything64_GetResultDateAccessed(uint index, out long fileTime);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultDateAccessed")]
    private static extern bool EverythingArm64_GetResultDateAccessed(uint index, out long fileTime);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultAttributes")]
    private static extern uint Everything64_GetResultAttributes(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultAttributes")]
    private static extern uint EverythingArm64_GetResultAttributes(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultFileListFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything64_GetResultFileListFileName(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultFileListFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr EverythingArm64_GetResultFileListFileName(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultRunCount")]
    private static extern uint Everything64_GetResultRunCount(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultRunCount")]
    private static extern uint EverythingArm64_GetResultRunCount(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultDateRun")]
    private static extern bool Everything64_GetResultDateRun(uint index, out long fileTime);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultDateRun")]
    private static extern bool EverythingArm64_GetResultDateRun(uint index, out long fileTime);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultDateRecentlyChanged")]
    private static extern bool Everything64_GetResultDateRecentlyChanged(uint index, out long fileTime);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultDateRecentlyChanged")]
    private static extern bool EverythingArm64_GetResultDateRecentlyChanged(uint index, out long fileTime);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultHighlightedFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything64_GetResultHighlightedFileName(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultHighlightedFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr EverythingArm64_GetResultHighlightedFileName(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultHighlightedPathW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything64_GetResultHighlightedPath(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultHighlightedPathW", CharSet = CharSet.Unicode)]
    private static extern IntPtr EverythingArm64_GetResultHighlightedPath(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_GetResultHighlightedFullPathAndFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr Everything64_GetResultHighlightedFullPath(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_GetResultHighlightedFullPathAndFileNameW", CharSet = CharSet.Unicode)]
    private static extern IntPtr EverythingArm64_GetResultHighlightedFullPath(uint index);

    [DllImport(DllX64, EntryPoint = "Everything_IsFolderResult")]
    private static extern bool Everything64_IsFolderResult(uint index);

    [DllImport(DllArm64, EntryPoint = "Everything_IsFolderResult")]
    private static extern bool EverythingArm64_IsFolderResult(uint index);

    public static string ExpectedDllName => EverythingArchitectureHelper.GetDllName();

    public static void SetSearch(string value)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetSearch(value);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetSearch(value);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetRequestFlags(EverythingRequestFlags flags)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetRequestFlags((uint)flags);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetRequestFlags((uint)flags);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetSort(uint sortType)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetSort(sortType);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetSort(sortType);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetOffset(uint offset)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetOffset(offset);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetOffset(offset);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetMax(uint max)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetMax(max);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetMax(max);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetMatchPath(bool enable)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetMatchPath(enable);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetMatchPath(enable);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetMatchCase(bool enable)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetMatchCase(enable);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetMatchCase(enable);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetMatchWholeWord(bool enable)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetMatchWholeWord(enable);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetMatchWholeWord(enable);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static void SetRegex(bool enable)
    {
        switch (EverythingArchitectureHelper.GetCurrentArchitecture())
        {
            case EverythingArchitecture.X64:
                Everything64_SetRegex(enable);
                break;
            case EverythingArchitecture.Arm64:
                EverythingArm64_SetRegex(enable);
                break;
            default:
                throw new PlatformNotSupportedException();
        }
    }

    public static bool Query(bool wait)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_Query(wait),
            EverythingArchitecture.Arm64 => EverythingArm64_Query(wait),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static uint GetNumResults()
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetNumResults(),
            EverythingArchitecture.Arm64 => EverythingArm64_GetNumResults(),
            _ => throw new PlatformNotSupportedException()
        };
    }

    /// <summary>Total matches for the current search (not capped by <see cref="SetMax"/>).</summary>
    public static uint GetTotResults()
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetTotResults(),
            EverythingArchitecture.Arm64 => EverythingArm64_GetTotResults(),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static EverythingErrorCode GetLastError()
    {
        return (EverythingErrorCode)(EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetLastError(),
            EverythingArchitecture.Arm64 => EverythingArm64_GetLastError(),
            _ => throw new PlatformNotSupportedException()
        });
    }

    public static bool IsDbLoaded()
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_IsDbLoaded(),
            EverythingArchitecture.Arm64 => EverythingArm64_IsDbLoaded(),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static string GetResultFullPath(uint index)
    {
        const int initialCapacity = 32768;
        var sb = new StringBuilder(initialCapacity);

        var copied = EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultFullPathName(index, sb, (uint)sb.Capacity),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultFullPathName(index, sb, (uint)sb.Capacity),
            _ => throw new PlatformNotSupportedException()
        };

        if (copied == 0)
            return string.Empty;

        return sb.ToString();
    }

    public static string GetResultFileName(uint index) => PtrToString(
        EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultFileName(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultFileName(index),
            _ => throw new PlatformNotSupportedException()
        });

    public static string GetResultPath(uint index) => PtrToString(
        EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultPath(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultPath(index),
            _ => throw new PlatformNotSupportedException()
        });

    public static string GetResultExtension(uint index) => PtrToString(
        EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultExtension(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultExtension(index),
            _ => throw new PlatformNotSupportedException()
        });

    public static bool TryGetResultSize(uint index, out long size)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultSize(index, out size),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultSize(index, out size),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static bool TryGetResultDateCreated(uint index, out long fileTime)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultDateCreated(index, out fileTime),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultDateCreated(index, out fileTime),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static bool TryGetResultDateModified(uint index, out long fileTime)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultDateModified(index, out fileTime),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultDateModified(index, out fileTime),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static bool TryGetResultDateAccessed(uint index, out long fileTime)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultDateAccessed(index, out fileTime),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultDateAccessed(index, out fileTime),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static uint GetResultAttributes(uint index)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultAttributes(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultAttributes(index),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static string GetResultFileListFileName(uint index) => PtrToString(
        EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultFileListFileName(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultFileListFileName(index),
            _ => throw new PlatformNotSupportedException()
        });

    public static uint GetResultRunCount(uint index)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultRunCount(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultRunCount(index),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static bool TryGetResultDateRun(uint index, out long fileTime)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultDateRun(index, out fileTime),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultDateRun(index, out fileTime),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static bool TryGetResultDateRecentlyChanged(uint index, out long fileTime)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultDateRecentlyChanged(index, out fileTime),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultDateRecentlyChanged(index, out fileTime),
            _ => throw new PlatformNotSupportedException()
        };
    }

    public static string GetResultHighlightedFileName(uint index) => PtrToString(
        EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultHighlightedFileName(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultHighlightedFileName(index),
            _ => throw new PlatformNotSupportedException()
        });

    public static string GetResultHighlightedPath(uint index) => PtrToString(
        EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultHighlightedPath(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultHighlightedPath(index),
            _ => throw new PlatformNotSupportedException()
        });

    public static string GetResultHighlightedFullPath(uint index) => PtrToString(
        EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_GetResultHighlightedFullPath(index),
            EverythingArchitecture.Arm64 => EverythingArm64_GetResultHighlightedFullPath(index),
            _ => throw new PlatformNotSupportedException()
        });

    public static bool IsFolderResult(uint index)
    {
        return EverythingArchitectureHelper.GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => Everything64_IsFolderResult(index),
            EverythingArchitecture.Arm64 => EverythingArm64_IsFolderResult(index),
            _ => throw new PlatformNotSupportedException()
        };
    }

    private static string PtrToString(IntPtr ptr)
    {
        return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(ptr) ?? string.Empty;
    }
}
