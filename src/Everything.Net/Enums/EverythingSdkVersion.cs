namespace Everything.Net.Enums;

/// <summary>
/// Identifies which Everything SDK the client uses to talk to Everything.
/// </summary>
public enum EverythingSdkVersion
{
    /// <summary>
    /// Use SDK 3 when Everything 1.5 is running, otherwise SDK 2.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// The SDK for Everything 1.4 (<c>Everything64.dll</c>). It also works with Everything 1.5.
    /// </summary>
    V2 = 2,

    /// <summary>
    /// The SDK for Everything 1.5 (<c>Everything3_x64.dll</c>). It does not work with Everything 1.4.
    /// </summary>
    V3 = 3
}
