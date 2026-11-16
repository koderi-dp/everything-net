using System.Reflection;
using System.Runtime.InteropServices;
using Everything.Net.Enums;

namespace Everything.Net.Internal;

/// <summary>
/// Maps the library names used in the P/Invoke declarations to the DLL for the current
/// processor architecture. An assembly can only have one resolver, so both SDKs share this one.
/// </summary>
internal static class NativeLibraryResolver
{
    public const string Sdk2X64 = "Everything64.dll";
    public const string Sdk2Arm64 = "EverythingARM64.dll";

    /// <summary>The library name the SDK 3 declarations use; it is not a file name.</summary>
    public const string Sdk3 = "Everything3";
    public const string Sdk3X64 = "Everything3_x64.dll";
    public const string Sdk3Arm64 = "Everything3_ARM64.dll";

    private static readonly Lock Gate = new();
    private static bool _registered;

    public static string Sdk2FileName =>
        EverythingArchitectureHelper.GetCurrentArchitecture() == EverythingArchitecture.Arm64 ? Sdk2Arm64 : Sdk2X64;

    public static string Sdk3FileName =>
        EverythingArchitectureHelper.GetCurrentArchitecture() == EverythingArchitecture.Arm64 ? Sdk3Arm64 : Sdk3X64;

    public static void EnsureRegistered()
    {
        lock (Gate)
        {
            if (_registered)
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, Resolve);
            _registered = true;
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        string fileName;

        if (string.Equals(libraryName, Sdk3, StringComparison.OrdinalIgnoreCase))
        {
            fileName = Sdk3FileName;
        }
        else if (string.Equals(libraryName, Sdk2X64, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(libraryName, Sdk2Arm64, StringComparison.OrdinalIgnoreCase))
        {
            fileName = Sdk2FileName;
        }
        else
        {
            return IntPtr.Zero;
        }

        var rid = EverythingArchitectureHelper.GetCurrentArchitecture() == EverythingArchitecture.Arm64 ? "win-arm64" : "win-x64";
        return EverythingNativeLibraryLocator.Load(fileName, rid);
    }
}
