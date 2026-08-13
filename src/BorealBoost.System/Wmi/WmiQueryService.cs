using System.Collections.ObjectModel;
using System.Management;

namespace BorealBoost.System.Wmi;

public sealed class WmiQueryService
{
    public Task<IReadOnlyList<WmiRow>> QueryAsync(string query, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return QueryAsync(@"root\cimv2", query, timeout, cancellationToken);
    }

    public Task<IReadOnlyList<WmiRow>> QueryAsync(string scopePath, string query, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "WMI timeout must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var options = new global::System.Management.EnumerationOptions
        {
            ReturnImmediately = true,
            Timeout = timeout
        };

        using var searcher = new ManagementObjectSearcher(new ManagementScope(scopePath), new ObjectQuery(query), options);
        using var collection = searcher.Get();

        var rows = new List<WmiRow>();
        foreach (ManagementBaseObject item in collection)
        {
            using (item)
            {
                cancellationToken.ThrowIfCancellationRequested();
                rows.Add(WmiRow.From(item));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((IReadOnlyList<WmiRow>)rows);
    }
}

public sealed class WmiRow
{
    private readonly IReadOnlyDictionary<string, object?> _values;

    private WmiRow(IReadOnlyDictionary<string, object?> values)
    {
        _values = values;
    }

    public static WmiRow From(ManagementBaseObject source)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (PropertyData property in source.Properties)
        {
            values[property.Name] = property.Value;
        }

        return new WmiRow(new ReadOnlyDictionary<string, object?>(values));
    }

    public string? String(string name)
    {
        return _values.TryGetValue(name, out var value) ? NormalizeString(value?.ToString()) : null;
    }

    public string[] StringArray(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return [];
        }

        if (value is string[] strings)
        {
            return strings.Select(NormalizeString).Where(item => item is not null).Select(item => item!).ToArray();
        }

        if (value is Array array)
        {
            return array
                .Cast<object?>()
                .Select(item => NormalizeString(Convert.ToString(item, global::System.Globalization.CultureInfo.InvariantCulture)))
                .Where(item => item is not null)
                .Select(item => item!)
                .ToArray();
        }

        return NormalizeString(value.ToString()) is { } single ? [single] : [];
    }

    public int? Int32(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return int.TryParse(Convert.ToString(value, global::System.Globalization.CultureInfo.InvariantCulture), out var parsed) ? parsed : null;
    }

    public uint? UInt32(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return uint.TryParse(Convert.ToString(value, global::System.Globalization.CultureInfo.InvariantCulture), out var parsed) ? parsed : null;
    }

    public ushort? UInt16(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return ushort.TryParse(Convert.ToString(value, global::System.Globalization.CultureInfo.InvariantCulture), out var parsed) ? parsed : null;
    }

    public ulong? UInt64(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return ulong.TryParse(Convert.ToString(value, global::System.Globalization.CultureInfo.InvariantCulture), out var parsed) ? parsed : null;
    }

    public bool? Bool(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return bool.TryParse(Convert.ToString(value, global::System.Globalization.CultureInfo.InvariantCulture), out var parsed) ? parsed : null;
    }

    public DateTimeOffset? CimDateTime(string name)
    {
        var value = String(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static string? NormalizeString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
