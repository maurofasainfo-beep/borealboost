namespace BorealBoost.Core.AgentProtocol;

public readonly record struct ProtocolVersion(int Major, int Minor, int Patch) : IComparable<ProtocolVersion>
{
    public static ProtocolVersion Current { get; } = new(1, 0, 0);

    public bool IsCompatibleWith(ProtocolVersion supported)
    {
        return Major == supported.Major && CompareTo(supported) <= 0;
    }

    public int CompareTo(ProtocolVersion other)
    {
        var major = Major.CompareTo(other.Major);
        if (major != 0)
        {
            return major;
        }

        var minor = Minor.CompareTo(other.Minor);
        return minor != 0 ? minor : Patch.CompareTo(other.Patch);
    }

    public static bool TryParse(string? value, out ProtocolVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            return false;
        }

        if (major < 0 || minor < 0 || patch < 0)
        {
            return false;
        }

        version = new ProtocolVersion(major, minor, patch);
        return true;
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
