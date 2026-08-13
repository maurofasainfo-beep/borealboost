using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BorealBoost.Core.Common;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;

namespace BorealBoost.Infrastructure.Persistence;

public sealed class FileOptimizationSessionStore : IOptimizationSessionStore, IOptimizationSessionArtifactStore
{
    public const string StoreSchemaVersion = "4.0.0";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _sessionsDirectory;

    public FileOptimizationSessionStore(string sessionsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionsDirectory);
        _sessionsDirectory = Path.GetFullPath(sessionsDirectory);
    }

    public async Task<Result> SaveAsync(OptimizationSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.SchemaVersion != StoreSchemaVersion)
        {
            return Result.Failure("optimization.session.schema_unsupported", "OptimizationSession schema version is unsupported.");
        }

        try
        {
            Directory.CreateDirectory(_sessionsDirectory);
            var sessionBytes = JsonSerializer.SerializeToUtf8Bytes(session, JsonOptions);
            var envelope = new StoredOptimizationSessionEnvelope(
                StoreSchemaVersion,
                ComputeHash(sessionBytes),
                session);
            var envelopeBytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
            var finalPath = GetSessionPath(session.SessionId);
            var tempPath = finalPath + ".tmp";

            await using (var stream = new FileStream(
                             tempPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 16 * 1024,
                             FileOptions.WriteThrough | FileOptions.SequentialScan))
            {
                await stream.WriteAsync(envelopeBytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, finalPath, overwrite: true);
            return Result.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return Result.Failure("optimization.session.persist_failed", exception.Message);
        }
    }

    public async Task<Result<OptimizationSession>> LoadAsync(SessionId sessionId, CancellationToken cancellationToken)
    {
        if (sessionId.Value == Guid.Empty)
        {
            return Result<OptimizationSession>.Failure("optimization.session.id_invalid", "SessionId is invalid.");
        }

        try
        {
            var path = GetSessionPath(sessionId);
            if (!File.Exists(path))
            {
                return Result<OptimizationSession>.Failure("optimization.session.not_found", "OptimizationSession was not found.");
            }

            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var envelope = JsonSerializer.Deserialize<StoredOptimizationSessionEnvelope>(bytes, JsonOptions);
            if (envelope is null)
            {
                return Result<OptimizationSession>.Failure("optimization.session.empty", "OptimizationSession file is empty.");
            }

            if (envelope.SchemaVersion != StoreSchemaVersion)
            {
                return Result<OptimizationSession>.Failure("optimization.session.schema_unsupported", "OptimizationSession schema version is unsupported.");
            }

            var sessionBytes = JsonSerializer.SerializeToUtf8Bytes(envelope.Session, JsonOptions);
            var hash = ComputeHash(sessionBytes);
            if (!FixedTimeEquals(hash, envelope.IntegrityHash))
            {
                return Result<OptimizationSession>.Failure("optimization.session.integrity_failed", "OptimizationSession integrity hash does not match.");
            }

            if (envelope.Session.SessionId != sessionId)
            {
                return Result<OptimizationSession>.Failure("optimization.session.id_mismatch", "OptimizationSession file does not match requested SessionId.");
            }

            return Result<OptimizationSession>.Success(envelope.Session);
        }
        catch (JsonException exception)
        {
            return Result<OptimizationSession>.Failure("optimization.session.malformed", exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Result<OptimizationSession>.Failure("optimization.session.load_failed", exception.Message);
        }
    }

    public async Task<IReadOnlyList<OptimizationSession>> ListAsync(CancellationToken cancellationToken)
    {
        var artifacts = await ListArtifactsAsync(cancellationToken).ConfigureAwait(false);
        return artifacts
            .Where(artifact => artifact is { IsValid: true, Session: not null })
            .Select(artifact => artifact.Session!)
            .ToArray();
    }

    public async Task<IReadOnlyList<OptimizationSessionArtifact>> ListArtifactsAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_sessionsDirectory))
        {
            return [];
        }

        var artifacts = new List<OptimizationSessionArtifact>();
        foreach (var file in Directory.EnumerateFiles(_sessionsDirectory, "*.json", SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!SessionId.TryParse(fileName, out var sessionId))
            {
                continue;
            }

            var loaded = await LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
            artifacts.Add(loaded.IsSuccess && loaded.Value is not null
                ? new OptimizationSessionArtifact(Path.GetFileName(file), sessionId, loaded.Value.Plan.PlanId, loaded.Value, true, null, null)
                : new OptimizationSessionArtifact(Path.GetFileName(file), sessionId, null, null, false, loaded.ErrorCode, loaded.ErrorMessage));
        }

        foreach (var tempFile in Directory.EnumerateFiles(_sessionsDirectory, "*.json.tmp", SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(tempFile);
            var sessionName = fileName[..^".json.tmp".Length];
            if (!SessionId.TryParse(sessionName, out var sessionId))
            {
                continue;
            }

            artifacts.Add(new OptimizationSessionArtifact(
                fileName,
                sessionId,
                null,
                null,
                false,
                "optimization.session.temp_artifact",
                "Temporary session artifact remains after an interrupted atomic write."));
        }

        return artifacts;
    }

    private string GetSessionPath(SessionId sessionId)
    {
        if (sessionId.Value == Guid.Empty)
        {
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        }

        var fileName = sessionId + ".json";
        var combined = Path.GetFullPath(Path.Combine(_sessionsDirectory, fileName));
        var rootWithSeparator = _sessionsDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? _sessionsDirectory
            : _sessionsDirectory + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved session path escaped BorealBoost session root.");
        }

        return combined;
    }

    private static string ComputeHash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        var actualBytes = Encoding.ASCII.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private sealed record StoredOptimizationSessionEnvelope(
        string SchemaVersion,
        string IntegrityHash,
        OptimizationSession Session);
}
