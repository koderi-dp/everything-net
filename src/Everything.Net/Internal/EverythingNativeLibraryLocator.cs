using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Everything.Net.Internal;

/// <summary>
/// Decides which copy of an Everything SDK DLL the process loads: a configured path first,
/// then a copy in the installed Everything folder, then the one bundled with this package.
/// </summary>
internal static class EverythingNativeLibraryLocator
{
    private const string InstallKey = @"SOFTWARE\voidtools\Everything";

    private static string? _configuredPath;
    private static bool _preferInstalled = true;

    private static readonly ConcurrentDictionary<string, string> LoadedPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the full path the named DLL was loaded from, or null if it has not been loaded.</summary>
    public static string? GetLoadedPath(string fileName) => LoadedPaths.GetValueOrDefault(fileName);

    /// <summary>
    /// Applies client options. Each DLL is loaded once per process, so this only has an effect
    /// before the first native call.
    /// </summary>
    public static void Configure(string? configuredPath, bool preferInstalled)
    {
        if (!LoadedPaths.IsEmpty)
        {
            return;
        }

        _configuredPath = configuredPath;
        _preferInstalled = preferInstalled;
    }

    public static IntPtr Load(string fileName, string runtimeIdentifier)
    {
        var installLocation = _preferInstalled ? FindInstallLocation() : null;

        foreach (var candidate in GetCandidates(fileName, runtimeIdentifier, _configuredPath, installLocation, AppContext.BaseDirectory))
        {
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
            {
                LoadedPaths[fileName] = Path.GetFullPath(candidate);
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>Lists the places to look, in the order they are tried.</summary>
    internal static IEnumerable<string> GetCandidates(
        string fileName,
        string runtimeIdentifier,
        string? configuredPath,
        string? installLocation,
        string baseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            // Either the DLL itself or the folder that holds it. A file path only counts for the
            // DLL it names; the other SDK's DLL is then looked for next to it.
            if (string.Equals(Path.GetFileName(configuredPath), fileName, StringComparison.OrdinalIgnoreCase))
            {
                yield return configuredPath;
            }
            else if (Path.GetExtension(configuredPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(Path.GetDirectoryName(configuredPath) ?? string.Empty, fileName);
            }
            else
            {
                yield return Path.Combine(configuredPath, fileName);
            }
        }

        if (!string.IsNullOrWhiteSpace(installLocation))
        {
            yield return Path.Combine(installLocation, "sdk", "dll", fileName);
            yield return Path.Combine(installLocation, fileName);
        }

        yield return Path.Combine(baseDirectory, "runtimes", runtimeIdentifier, "native", fileName);
        yield return Path.Combine(baseDirectory, fileName);
    }

    /// <summary>
    /// Reads where Everything is installed. Only the machine-wide registry is trusted: it takes
    /// administrator rights to change, unlike the per-user one.
    /// </summary>
    private static string? FindInstallLocation()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        foreach (var view in (ReadOnlySpan<RegistryView>)[RegistryView.Registry64, RegistryView.Registry32])
        {
            try
            {
                using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = machine.OpenSubKey(InstallKey);

                if (key?.GetValue("InstallLocation") is string { Length: > 0 } location)
                {
                    return location;
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // No access to this view; try the next one.
            }
        }

        return null;
    }
}
