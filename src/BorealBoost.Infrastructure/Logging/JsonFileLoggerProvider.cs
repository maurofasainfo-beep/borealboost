using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BorealBoost.Infrastructure.Logging;

public sealed class JsonFileLoggerProvider : ILoggerProvider
{
    private static readonly Regex ProcessRolePattern = new("^[a-z][a-z0-9-]{0,31}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Encoding LogEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _logFilePath;
    private readonly object _syncRoot = new();
    private readonly TextWriter _fallbackWriter;
    private readonly Func<DateTimeOffset> _timestampProvider;
    private readonly ConcurrentDictionary<string, JsonFileLogger> _loggers = new(StringComparer.Ordinal);
    private StreamWriter? _writer;
    private bool _disposed;
    private bool _openFailureReported;

    public JsonFileLoggerProvider(string logsDirectory, string processRole)
        : this(logsDirectory, processRole, Environment.ProcessId, () => DateTimeOffset.UtcNow, Console.Error)
    {
    }

    public JsonFileLoggerProvider(
        string logsDirectory,
        string processRole,
        int processId,
        Func<DateTimeOffset> timestampProvider,
        TextWriter fallbackWriter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logsDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(processRole);
        ArgumentNullException.ThrowIfNull(timestampProvider);
        ArgumentNullException.ThrowIfNull(fallbackWriter);

        if (!ProcessRolePattern.IsMatch(processRole))
        {
            throw new ArgumentException("Log process role must be lowercase alphanumeric text with optional hyphens.", nameof(processRole));
        }

        var date = timestampProvider().UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        _logFilePath = Path.Combine(logsDirectory, $"{processRole}-{date}-{processId}.jsonl");
        _timestampProvider = timestampProvider;
        _fallbackWriter = fallbackWriter;

        TryOpenWriter(logsDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, static (name, provider) => new JsonFileLogger(name, provider), this);
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
            catch (Exception exception) when (IsRecoverableLoggingException(exception))
            {
                ReportLoggingFailure("dispose", exception);
            }
        }
    }

    private void TryOpenWriter(string logsDirectory)
    {
        try
        {
            Directory.CreateDirectory(logsDirectory);
            var stream = new FileStream(
                _logFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.SequentialScan);
            _writer = new StreamWriter(stream, LogEncoding) { AutoFlush = false };
        }
        catch (Exception exception) when (IsRecoverableLoggingException(exception))
        {
            ReportLoggingFailure("open", exception);
        }
    }

    private void WriteLogLine(string categoryName, LogLevel logLevel, EventId eventId, string message, Exception? exception, IReadOnlyDictionary<string, object?> properties)
    {
        var entry = new
        {
            timestampUtc = _timestampProvider(),
            level = logLevel.ToString(),
            source = categoryName,
            eventId = eventId.Id,
            eventName = eventId.Name,
            message,
            properties,
            exception = exception?.ToString()
        };

        var line = JsonSerializer.Serialize(entry);

        lock (_syncRoot)
        {
            if (_disposed)
            {
                ReportLoggingFailure("write", new ObjectDisposedException(nameof(JsonFileLoggerProvider)));
                return;
            }

            if (_writer is null)
            {
                if (!_openFailureReported)
                {
                    _openFailureReported = true;
                    ReportLoggingFailure("write", new IOException($"Log file '{_logFilePath}' is unavailable."));
                }

                return;
            }

            try
            {
                _writer.WriteLine(line);
                _writer.Flush();
            }
            catch (Exception writeException) when (IsRecoverableLoggingException(writeException))
            {
                ReportLoggingFailure("write", writeException);
            }
        }
    }

    private void ReportLoggingFailure(string operation, Exception exception)
    {
        var message =
            $"{DateTimeOffset.UtcNow:O} LOGGING_FAILURE operation={operation} path=\"{_logFilePath}\" exception={exception.GetType().Name}: {exception.Message}";

        try
        {
            _fallbackWriter.WriteLine(message);
            _fallbackWriter.Flush();
        }
        catch (Exception fallbackException) when (IsRecoverableLoggingException(fallbackException))
        {
            Debug.WriteLine(message);
            Debug.WriteLine(fallbackException);
        }
    }

    private static bool IsRecoverableLoggingException(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or ObjectDisposedException or NotSupportedException;
    }

    private sealed class JsonFileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly JsonFileLoggerProvider _provider;

        public JsonFileLogger(string categoryName, JsonFileLoggerProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var properties = ExtractStructuredProperties(state);
            _provider.WriteLogLine(_categoryName, logLevel, eventId, message, exception, properties);
        }

        private static IReadOnlyDictionary<string, object?> ExtractStructuredProperties<TState>(TState state)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                return new Dictionary<string, object?>();
            }

            return pairs
                .Where(pair => pair.Key != "{OriginalFormat}")
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
