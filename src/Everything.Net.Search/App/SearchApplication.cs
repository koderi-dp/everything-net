using Everything.Net.Configuration;
using Everything.Net.Models;
using Everything.Net.Search.Models;
using Everything.Net.Search.Rendering;
using Everything.Net.Search.Services;
using Everything.Net.Services;
using Microsoft.Extensions.Options;

namespace Everything.Net.Search.App;

internal static class SearchApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        SearchCliOptions options;

        try
        {
            options = SearchCliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            SearchConsoleRenderer.RenderHeader();
            Console.WriteLine($"Argument error: {ex.Message}");
            SearchConsoleRenderer.RenderHelp();
            return 1;
        }

        if (options.ShowHelp)
        {
            SearchConsoleRenderer.RenderHeader();
            SearchConsoleRenderer.RenderHelp();
            return 0;
        }

        var client = new EverythingClient(Options.Create(new EverythingClientOptions
        {
            ThrowOnUnavailableClient = false,
            DefaultMaxResults = options.Limit
        }));

        SearchConsoleRenderer.RenderHeader();

        if (!client.IsAvailable())
        {
            SearchConsoleRenderer.RenderUnavailable();
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(options.QueryText) || options.NoPrompt)
        {
            return await RunSingleSearchAsync(client, options);
        }

        return await new LiveFinderSession(client).RunAsync(options);
    }

    private static async Task<int> RunSingleSearchAsync(EverythingClient client, SearchCliOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.QueryText))
        {
            Console.WriteLine("A search query is required.");
            SearchConsoleRenderer.RenderHelp();
            return 1;
        }

        var response = await ExecuteSearchAsync(client, options);
        if (!response.Success)
        {
            SearchConsoleRenderer.RenderSearchFailure(response);
            return 4;
        }

        SearchConsoleRenderer.RenderResults(response, options);
        return 0;
    }

    private static async Task<EverythingQueryResponse> ExecuteSearchAsync(EverythingClient client, SearchCliOptions options)
    {
        var query = SearchQueryFactory.Build(options);

        try
        {
            return await client.SearchAsync(query);
        }
        catch (Exception ex)
        {
            return new EverythingQueryResponse
            {
                SearchText = options.QueryText,
                Success = false,
                TotalResults = 0,
                Results = Array.Empty<EverythingSearchResult>(),
                ErrorCode = 0,
                ErrorMessage = ex.Message
            };
        }
    }
}
