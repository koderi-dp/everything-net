using System.Text;
using Everything.Net.Models;
using Everything.Net.Search.Models;

namespace Everything.Net.Search.Services;

internal sealed class FinderPreviewService
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".log", ".csv",
        ".cs", ".csproj", ".sln", ".props", ".targets", ".fs", ".vb",
        ".js", ".ts", ".jsx", ".tsx", ".html", ".htm", ".css", ".scss",
        ".ps1", ".cmd", ".bat", ".config", ".editorconfig", ".gitignore"
    };

    public FinderPreviewContent Build(EverythingSearchResult? selected)
    {
        if (selected is null)
        {
            return FinderPreviewContent.Empty("Preview", "No item selected.");
        }

        if (selected.IsFolder)
        {
            return BuildMetadata(selected, "Folder Preview");
        }

        if (!File.Exists(selected.FullPath))
        {
            return FinderPreviewContent.Empty(
                "Preview",
                "File is not available on disk.",
                selected.FullPath);
        }

        if (!IsTextCandidate(selected))
        {
            return BuildMetadata(selected, "Metadata");
        }

        try
        {
            if (LooksBinary(selected.FullPath))
            {
                return BuildMetadata(selected, "Metadata");
            }

            var lines = ReadPreviewLines(selected.FullPath, maxLines: 200, maxCharsPerLine: 240);
            if (lines.Count == 0)
            {
                return FinderPreviewContent.Empty("Preview", "File is empty.");
            }

            return new FinderPreviewContent(
                Header: selected.FullPath,
                IsTextPreview: true,
                Lines: lines);
        }
        catch (IOException)
        {
            return BuildMetadata(selected, "Metadata");
        }
        catch (UnauthorizedAccessException)
        {
            return BuildMetadata(selected, "Metadata");
        }
    }

    private static FinderPreviewContent BuildMetadata(EverythingSearchResult selected, string header)
    {
        return new FinderPreviewContent(
            Header: selected.FullPath,
            IsTextPreview: false,
            Lines:
            [
                $"Type       {(selected.IsFolder ? "Folder" : "File")}",
                $"Name       {selected.FileName}",
                $"Path       {selected.Path}",
                $"Extension  {selected.Extension ?? "-"}",
                $"Size       {(selected.IsFolder ? "-" : FormatSize(selected.Size))}",
                $"Modified   {FormatDate(selected.DateModified)}"
            ]);
    }

    private static bool IsTextCandidate(EverythingSearchResult selected)
    {
        var extension = Path.GetExtension(selected.FileName);
        return !string.IsNullOrWhiteSpace(extension) && TextExtensions.Contains(extension);
    }

    private static bool LooksBinary(string path)
    {
        using var stream = File.OpenRead(path);
        var buffer = new byte[Math.Min(2048, (int)stream.Length)];
        var read = stream.Read(buffer, 0, buffer.Length);

        for (var i = 0; i < read; i++)
        {
            if (buffer[i] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> ReadPreviewLines(string path, int maxLines, int maxCharsPerLine)
    {
        var result = new List<string>(maxLines);
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        while (!reader.EndOfStream && result.Count < maxLines)
        {
            var line = reader.ReadLine() ?? string.Empty;
            result.Add(Truncate(line, maxCharsPerLine));
        }

        if (!reader.EndOfStream)
        {
            result.Add("...");
        }

        return result;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..(maxLength - 3)]}...";
    }

    private static string FormatDate(DateTimeOffset? value) => value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";

    private static string FormatSize(long? bytes)
    {
        if (bytes is null)
        {
            return "-";
        }

        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes.Value;
        var order = 0;

        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {suffixes[order]}";
    }
}
