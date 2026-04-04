namespace Everything.Net.Internal;

internal static class FileTimeConverter
{
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
