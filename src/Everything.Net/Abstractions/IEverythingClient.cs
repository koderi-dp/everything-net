using Everything.Net.Models;

namespace Everything.Net.Abstractions;

public interface IEverythingClient
{
    bool IsAvailable();

    EverythingQueryResponse Search(EverythingQuery query);

    Task<EverythingQueryResponse> SearchAsync(
        EverythingQuery query,
        CancellationToken cancellationToken = default);
}
