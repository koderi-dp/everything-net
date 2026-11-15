namespace Everything.Net.Internal;

internal static class FileTimeConverter
{
    /// <summary>Converts to a file time, using 0 for "not set" and for dates a file time cannot hold.</summary>
    public static long ToNative(DateTimeOffset? value)
    {
        if (value is not { } date)
            return 0;

        try
        {
            return Math.Max(date.ToFileTime(), 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            return 0;
        }
    }

    public static DateTimeOffset? FromNative(long fileTime)
    {
        if (fileTime <= 0)
            return null;

        try
        {
            return DateTimeOffset.FromFileTime(fileTime);
        }
        catch
        {
            return null;
        }
    }
}
