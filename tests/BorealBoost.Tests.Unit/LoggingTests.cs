using System.Text.Json;
using BorealBoost.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Tests.Unit;

public sealed class LoggingTests
{
    [Fact]
    public async Task App_and_agent_logs_can_be_written_concurrently_without_file_lock()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BorealBoost.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        using var appProvider = new JsonFileLoggerProvider(directory, "app", 1001, () => DateTimeOffset.Parse("2026-08-12T00:00:00Z"), TextWriter.Null);
        using var agentProvider = new JsonFileLoggerProvider(directory, "agent", 1002, () => DateTimeOffset.Parse("2026-08-12T00:00:00Z"), TextWriter.Null);
        var appLogger = appProvider.CreateLogger("BorealBoost.App");
        var agentLogger = agentProvider.CreateLogger("BorealBoost.Agent");

        await Task.WhenAll(
            Task.Run(() => WriteMany(appLogger, "app")),
            Task.Run(() => WriteMany(agentLogger, "agent")));

        appProvider.Dispose();
        agentProvider.Dispose();

        var files = Directory.GetFiles(directory, "*.jsonl").Select(path => Path.GetFileName(path)!).OrderBy(item => item).ToArray();
        Assert.Equal(new[] { "agent-20260812-1002.jsonl", "app-20260812-1001.jsonl" }, files);

        foreach (var file in Directory.GetFiles(directory, "*.jsonl"))
        {
            var lines = File.ReadAllLines(file);
            Assert.Equal(100, lines.Length);
            foreach (var line in lines)
            {
                using var document = JsonDocument.Parse(line);
                Assert.True(document.RootElement.TryGetProperty("timestampUtc", out _));
                Assert.True(document.RootElement.TryGetProperty("level", out _));
                Assert.True(document.RootElement.TryGetProperty("source", out _));
                Assert.True(document.RootElement.TryGetProperty("message", out _));
            }
        }
    }

    [Fact]
    public void Logger_reports_fallback_when_directory_is_unavailable_without_throwing()
    {
        var root = Path.Combine(Path.GetTempPath(), "BorealBoost.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var unavailableDirectory = Path.Combine(root, "not-a-directory");
        File.WriteAllText(unavailableDirectory, "file");
        using var fallback = new StringWriter();

        using var provider = new JsonFileLoggerProvider(unavailableDirectory, "app", 1003, () => DateTimeOffset.UtcNow, fallback);
        var logger = provider.CreateLogger("BorealBoost.App");

        logger.LogInformation("Message that should use fallback.");

        Assert.Contains("LOGGING_FAILURE", fallback.ToString(), StringComparison.Ordinal);
    }

    private static void WriteMany(ILogger logger, string role)
    {
        for (var index = 0; index < 100; index++)
        {
            logger.LogInformation("Concurrent {Role} message {Index}", role, index);
        }
    }
}
