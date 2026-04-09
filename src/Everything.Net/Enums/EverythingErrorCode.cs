namespace Everything.Net.Enums;

/// <summary>
/// Error codes reported by the Everything SDK.
/// </summary>
public enum EverythingErrorCode : uint
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Ok = 0,

    /// <summary>
    /// The SDK could not allocate memory.
    /// </summary>
    Memory = 1,

    /// <summary>
    /// The client could not communicate with the Everything process.
    /// </summary>
    Ipc = 2,

    /// <summary>
    /// The SDK failed to register its window class.
    /// </summary>
    RegisterClassEx = 3,

    /// <summary>
    /// The SDK failed to create its window.
    /// </summary>
    CreateWindow = 4,

    /// <summary>
    /// The SDK failed to create a required thread.
    /// </summary>
    CreateThread = 5,

    /// <summary>
    /// A result index was invalid.
    /// </summary>
    InvalidIndex = 6,

    /// <summary>
    /// The SDK was called in an invalid state.
    /// </summary>
    InvalidCall = 7
}
