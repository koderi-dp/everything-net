namespace Everything.Net.Internal;

/// <summary>
/// Hands out one shared string per distinct value while a response is being read, so results
/// in the same folder (or with the same extension) do not each carry their own copy.
/// </summary>
internal sealed class StringPool
{
    private readonly HashSet<string> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _lookup;
    private string _last = string.Empty;

    public StringPool()
    {
        _lookup = _values.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    public string GetOrAdd(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return string.Empty;
        }

        // Neighbouring results usually repeat the previous value; skip the hash lookup for those.
        if (value.SequenceEqual(_last))
        {
            return _last;
        }

        if (!_lookup.TryGetValue(value, out var pooled))
        {
            pooled = new string(value);
            _values.Add(pooled);
        }

        _last = pooled;
        return pooled;
    }
}
