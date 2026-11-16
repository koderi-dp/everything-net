using Everything.Net.Enums;

namespace Everything.Net.Configuration;

/// <summary>
/// Configures default behavior for <c>EverythingClient</c>.
/// </summary>
public sealed class EverythingClientOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the client throws when Everything is unavailable.
    /// </summary>
    public bool ThrowOnUnavailableClient { get; set; } = true;

    /// <summary>
    /// Gets or sets the fallback maximum result count used when a query does not specify <see cref="Models.EverythingQuery.MaxResults"/>.
    /// </summary>
    public uint DefaultMaxResults { get; set; } = 100;

    /// <summary>
    /// Gets or sets the fallback offset used when a query does not specify <see cref="Models.EverythingQuery.Offset"/>.
    /// </summary>
    public uint DefaultOffset { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether file name and path fields are always requested, even if they are not included in <see cref="Models.EverythingQuery.RequestFlags"/>.
    /// </summary>
    public bool RequestPathAndFileNameByDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets which Everything SDK to use. The default, <see cref="EverythingSdkVersion.Auto"/>,
    /// uses SDK 3 when Everything 1.5 is running and SDK 2 otherwise.
    /// </summary>
    public EverythingSdkVersion SdkVersion { get; set; } = EverythingSdkVersion.Auto;

    /// <summary>
    /// Gets or sets the Everything instance name to connect to with SDK 3. When not set, the unnamed
    /// instance is tried first and then <c>1.5a</c>, the name the Everything 1.5 alpha uses.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Gets or sets where to load the Everything SDK DLLs from, as the path of the folder that contains them
    /// (or of one of the DLLs). When set, it is tried before any other location.
    /// </summary>
    /// <remarks>
    /// The DLL is loaded once per process, so the options of the first client that is created decide.
    /// </remarks>
    public string? NativeLibraryPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an SDK DLL found in the installed Everything folder
    /// is used ahead of the copy bundled with this package. Set to <see langword="false"/> to always use the bundled copy.
    /// </summary>
    /// <remarks>
    /// The DLL is loaded once per process, so the options of the first client that is created decide.
    /// </remarks>
    public bool PreferInstalledNativeLibrary { get; set; } = true;
}
