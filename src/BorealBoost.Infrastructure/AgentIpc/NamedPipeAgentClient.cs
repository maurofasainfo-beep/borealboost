using System.IO.Pipes;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Common;

namespace BorealBoost.Infrastructure.AgentIpc;

public sealed class NamedPipeAgentClient : IAsyncDisposable
{
    private readonly NamedPipeClientStream _stream;

    private NamedPipeAgentClient(NamedPipeClientStream stream)
    {
        _stream = stream;
    }

    public static async Task<Result<NamedPipeAgentClient>> ConnectAsync(
        string fullPipeName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var pipeNameResult = AgentPipeName.ParseFullName(fullPipeName);
        if (pipeNameResult.IsFailure || pipeNameResult.Value is null)
        {
            return Result<NamedPipeAgentClient>.Failure(
                pipeNameResult.ErrorCode ?? "agent.pipe.invalid",
                pipeNameResult.ErrorMessage ?? "Agent pipe name is invalid.");
        }

        var stream = new NamedPipeClientStream(
            ".",
            pipeNameResult.Value.LocalName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await stream.ConnectAsync(cancellationToken).WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            return Result<NamedPipeAgentClient>.Success(new NamedPipeAgentClient(stream));
        }
        catch (Exception exception) when (exception is TimeoutException or IOException or OperationCanceledException)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            return Result<NamedPipeAgentClient>.Failure("agent.pipe.connect_failed", exception.Message);
        }
    }

    public Task<Result> WriteMessageAsync(AgentProtocolMessage message, CancellationToken cancellationToken)
    {
        return AgentPipeProtocol.WriteMessageAsync(_stream, message, cancellationToken);
    }

    public Task<Result<AgentProtocolMessage>> ReadMessageAsync(CancellationToken cancellationToken)
    {
        return AgentPipeProtocol.ReadMessageAsync(_stream, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }
}
