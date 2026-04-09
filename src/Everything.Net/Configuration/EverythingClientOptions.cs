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
}
