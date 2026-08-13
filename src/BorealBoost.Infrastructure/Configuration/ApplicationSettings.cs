using Microsoft.Extensions.Configuration;

namespace BorealBoost.Infrastructure.Configuration;

public sealed record ApplicationSettings(
    string EnvironmentName,
    string TechnicianDisplayName,
    bool EnableAgentHandshakeProbe)
{
    public static ApplicationSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("BorealBoost");
        var environmentName = section["EnvironmentName"] ?? "Development";
        var technicianDisplayName = section["TechnicianDisplayName"] ?? "Tecnico";
        var handshakeProbeRaw = section["EnableAgentHandshakeProbe"];

        ValidateText(environmentName, "BorealBoost:EnvironmentName", maxLength: 64);
        ValidateText(technicianDisplayName, "BorealBoost:TechnicianDisplayName", maxLength: 128);

        if (handshakeProbeRaw is not null && !bool.TryParse(handshakeProbeRaw, out _))
        {
            throw new InvalidOperationException("BorealBoost:EnableAgentHandshakeProbe must be true or false.");
        }

        return new ApplicationSettings(
            environmentName,
            technicianDisplayName,
            bool.TryParse(handshakeProbeRaw, out var enabled) && enabled);
    }

    private static void ValidateText(string value, string key, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
        {
            throw new InvalidOperationException($"{key} is required and must be at most {maxLength} characters.");
        }
    }
}
