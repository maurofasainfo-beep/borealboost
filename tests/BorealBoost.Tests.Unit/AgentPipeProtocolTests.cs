using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Identity;
using BorealBoost.Core.Optimization;
using BorealBoost.Infrastructure.AgentIpc;
using BorealBoost.Optimization.Catalog;
using System.Text.Json;

namespace BorealBoost.Tests.Unit;

public sealed class AgentPipeProtocolTests
{
    [Fact]
    public async Task ReadMessage_rejects_payload_larger_than_limit()
    {
        await using var stream = new MemoryStream();
        await stream.WriteAsync(BitConverter.GetBytes(AgentProtocolValidator.MaxMessageBytes + 1));
        stream.Position = 0;

        var result = await AgentPipeProtocol.ReadMessageAsync(stream, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.message.size_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task ReadMessage_rejects_truncated_payload()
    {
        await using var stream = new MemoryStream();
        await stream.WriteAsync(BitConverter.GetBytes(32));
        await stream.WriteAsync(new byte[] { 1, 2, 3 });
        stream.Position = 0;

        var result = await AgentPipeProtocol.ReadMessageAsync(stream, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.read.failed", result.ErrorCode);
    }

    [Fact]
    public void DeserializePayload_rejects_extra_dangerous_payload_member()
    {
        using var document = JsonDocument.Parse("""
        {
          "planSchemaVersion": "4.0.0",
          "catalogVersion": "4.0.0-built-in-foundation",
          "optimizationId": { "value": "BB.OPT.INTEGRATION.REGISTRY_PROOF" },
          "operation": null,
          "command": "cmd.exe /c whoami"
        }
        """);

        var message = new AgentProtocolMessage(
            new AgentMessageEnvelope(
                ProtocolVersion.Current,
                MessageType.ValidateOperationRequest,
                SessionId.New(),
                CorrelationId.New(),
                RequestId.New(),
                1,
                DateTimeOffset.UtcNow,
                AgentNonce.GenerateBootstrapNonce(),
                PayloadType.ValidateOperationRequest,
                128),
            document.RootElement.Clone());

        var result = AgentPipeProtocol.DeserializePayload<ValidateOperationRequestPayload>(message);

        Assert.True(result.IsFailure);
        Assert.Equal("protocol.payload.malformed", result.ErrorCode);
    }
}
