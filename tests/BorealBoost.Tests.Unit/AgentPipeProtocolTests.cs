using BorealBoost.Core.AgentProtocol;
using BorealBoost.Infrastructure.AgentIpc;

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
}
