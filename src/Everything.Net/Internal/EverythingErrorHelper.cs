using Everything.Net.Enums;

namespace Everything.Net.Internal;

internal static class EverythingErrorHelper
{
    public static string ToMessage(EverythingErrorCode errorCode) => errorCode switch
    {
        EverythingErrorCode.Ok => "No error.",
        EverythingErrorCode.Memory => "Failed to allocate memory.",
        EverythingErrorCode.Ipc => "IPC communication with Everything failed.",
        EverythingErrorCode.RegisterClassEx => "Failed to register the IPC window class.",
        EverythingErrorCode.CreateWindow => "Failed to create the IPC window.",
        EverythingErrorCode.CreateThread => "Failed to create the IPC thread.",
        EverythingErrorCode.InvalidIndex => "An invalid result index was requested.",
        EverythingErrorCode.InvalidCall => "The SDK call was invalid in the current state.",
        _ => $"Unknown Everything SDK error: {(uint)errorCode}."
    };
}
