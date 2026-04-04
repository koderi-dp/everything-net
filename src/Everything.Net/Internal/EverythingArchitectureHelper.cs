using System.Runtime.InteropServices;
using Everything.Net.Enums;

namespace Everything.Net.Internal;

internal static class EverythingArchitectureHelper
{
    public static EverythingArchitecture GetCurrentArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => EverythingArchitecture.X64,
            Architecture.Arm64 => EverythingArchitecture.Arm64,
            _ => throw new PlatformNotSupportedException(
                $"Everything.Net supports only Windows x64 and ARM64. Current process architecture: {RuntimeInformation.ProcessArchitecture}.")
        };
    }

    public static string GetDllName()
    {
        return GetCurrentArchitecture() switch
        {
            EverythingArchitecture.X64 => "Everything64.dll",
            EverythingArchitecture.Arm64 => "EverythingARM64.dll",
            _ => throw new PlatformNotSupportedException("Unsupported architecture.")
        };
    }
}
