using System.Security.Cryptography;

namespace BorealBoost.Core.AgentProtocol;

public static class AgentNonce
{
    public const int BootstrapNonceBytes = 32;
    public const int BootstrapNonceLength = 43;
    public const int PipeTokenBytes = 16;
    public const int PipeTokenLength = 22;

    public static string GenerateBootstrapNonce()
    {
        return GenerateBase64Url(BootstrapNonceBytes);
    }

    public static string GeneratePipeToken()
    {
        return GenerateBase64Url(PipeTokenBytes);
    }

    public static bool IsValidBootstrapNonce(string? value)
    {
        return IsBase64Url(value, BootstrapNonceLength);
    }

    public static bool IsValidPipeToken(string? value)
    {
        return IsBase64Url(value, PipeTokenLength);
    }

    private static string GenerateBase64Url(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool IsBase64Url(string? value, int expectedLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != expectedLength)
        {
            return false;
        }

        return value.All(character =>
            character is >= 'A' and <= 'Z' ||
            character is >= 'a' and <= 'z' ||
            character is >= '0' and <= '9' ||
            character is '-' or '_');
    }
}
