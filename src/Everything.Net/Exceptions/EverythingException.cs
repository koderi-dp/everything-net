using Everything.Net.Enums;

namespace Everything.Net.Exceptions;

public sealed class EverythingException(string message, EverythingErrorCode errorCode) : Exception(message)
{
    public EverythingErrorCode ErrorCode { get; } = errorCode;
}
