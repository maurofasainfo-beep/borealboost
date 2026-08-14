using System.Security.Cryptography;
using System.Text;

namespace BorealBoost.Core.Optimization;

public static class OperationSnapshotHasher
{
    public static OperationSnapshotItem Stamp(OperationSnapshotItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item with { SnapshotHash = Compute(item) };
    }

    public static bool IsValid(OperationSnapshotItem item)
    {
        return ExecutionPlanHasher.IsValidHash(item.SnapshotHash) &&
               string.Equals(Compute(item), item.SnapshotHash, StringComparison.Ordinal);
    }

    public static string Compute(OperationSnapshotItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var builder = new StringBuilder();
        Append(builder, "snapshotItemId", item.SnapshotItemId.ToString("D"));
        Append(builder, "operationId", item.OperationId.Value);
        Append(builder, "resourceType", item.ResourceType.ToString());
        Append(builder, "resourceIdentity", item.ResourceIdentity);
        Append(builder, "existedBefore", item.ExistedBefore.ToString());
        Append(builder, "registryKeyExistedBefore", item.RegistryKeyExistedBefore?.ToString() ?? string.Empty);
        Append(builder, "registryHive", item.RegistryTarget?.Hive.ToString() ?? string.Empty);
        Append(builder, "registryKey", item.RegistryTarget?.KeyPath ?? string.Empty);
        Append(builder, "registryValue", item.RegistryTarget?.ValueName ?? string.Empty);
        Append(builder, "registryView", item.RegistryTarget?.View.ToString() ?? string.Empty);
        Append(builder, "previousKind", item.PreviousValueKind?.ToString() ?? string.Empty);
        Append(builder, "previousString", item.PreviousStringValue ?? string.Empty);
        Append(builder, "previousDWord", item.PreviousDWordValue?.ToString(global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        Append(builder, "previousQWord", item.PreviousQWordValue?.ToString(global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        AppendValues(builder, "previousMulti", item.PreviousMultiStringValue ?? []);
        Append(builder, "previousBinary", item.PreviousBinaryValue is null ? string.Empty : Convert.ToHexString(item.PreviousBinaryValue));
        Append(builder, "captureMethod", item.CaptureMethod);
        Append(builder, "capturedAt", item.CapturedAtUtc.ToUniversalTime().ToString("O"));
        Append(builder, "restoreKind", item.RestorationStrategy.Kind.ToString());
        Append(builder, "restoreDescription", item.RestorationStrategy.Description);
        AppendValues(builder, "limitations", item.Limitations);
        Append(builder, "verification", item.VerificationMetadata);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void AppendValues(StringBuilder builder, string key, IEnumerable<string> values)
    {
        var array = values.ToArray();
        Append(builder, key + ".count", array.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        foreach (var value in array)
        {
            Append(builder, key, value);
        }
    }

    private static void Append(StringBuilder builder, string key, string value)
    {
        builder
            .Append(key.Length)
            .Append(':')
            .Append(key)
            .Append('=')
            .Append(value.Length)
            .Append(':')
            .Append(value)
            .Append('\n');
    }
}
