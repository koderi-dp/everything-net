namespace Everything.Net.Configuration;

public sealed class EverythingClientOptions
{
    public bool ThrowOnUnavailableClient { get; set; } = true;

    public uint DefaultMaxResults { get; set; } = 100;

    public uint DefaultOffset { get; set; } = 0;

    public bool RequestPathAndFileNameByDefault { get; set; } = true;
}
