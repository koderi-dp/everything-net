using Everything.Net.Models;

namespace Everything.Net.Abstractions;

/// <summary>
/// Provides synchronous and asynchronous search access to the Everything SDK.
/// </summary>
public interface IEverythingClient
{
    /// <summary>
    /// Determines whether the Everything native SDK is available to the current process.
    /// </summary>
    /// <returns><see langword="true"/> when the client can communicate with Everything; otherwise, <see langword="false"/>.</returns>
    bool IsAvailable();

    /// <summary>
    /// Executes a search against Everything.
    /// </summary>
    /// <param name="query">The query configuration to execute.</param>
    /// <returns>The search response, including visible results and total match count.</returns>
    EverythingQueryResponse Search(EverythingQuery query);

    /// <summary>
    /// Executes a search against Everything asynchronously.
    /// </summary>
    /// <param name="query">The query configuration to execute.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A task that resolves to the search response.</returns>
    Task<EverythingQueryResponse> SearchAsync(
        EverythingQuery query,
        CancellationToken cancellationToken = default);
}
