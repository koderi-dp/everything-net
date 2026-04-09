using Everything.Net.Enums;

namespace Everything.Net.Exceptions;

/// <summary>
/// Represents an error returned by the Everything SDK.
/// </summary>
/// <param name="message">The error message.</param>
/// <param name="errorCode">The native Everything error code.</param>
public sealed class EverythingException(string message, EverythingErrorCode errorCode) : Exception(message)
{
    /// <summary>
    /// Gets the underlying Everything error code.
    /// </summary>
    public EverythingErrorCode ErrorCode { get; } = errorCode;
}
