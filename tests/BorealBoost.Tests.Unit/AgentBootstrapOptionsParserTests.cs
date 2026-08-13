using BorealBoost.Agent;
using BorealBoost.Core.AgentProtocol;
using BorealBoost.Core.Identity;

namespace BorealBoost.Tests.Unit;

public sealed class AgentBootstrapOptionsParserTests
{
    [Fact]
    public void Parser_accepts_empty_foundation_invocation()
    {
        var result = AgentBootstrapOptionsParser.Parse([]);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsHandshakeBootstrapRequested);
    }

    [Fact]
    public void Parser_accepts_only_the_approved_bootstrap_contract()
    {
        var sessionId = SessionId.New();
        var pipeToken = AgentNonce.GeneratePipeToken();
        var args = new[]
        {
            "--pipeName", AgentPipeName.CreateFullName(sessionId, pipeToken),
            "--sessionId", sessionId.ToString(),
            "--bootstrapNonce", AgentNonce.GenerateBootstrapNonce(),
            "--protocolVersion", ProtocolVersion.Current.ToString()
        };

        var result = AgentBootstrapOptionsParser.Parse(args);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsHandshakeBootstrapRequested);
    }

    [Theory]
    [InlineData("--command")]
    [InlineData("--powershell")]
    [InlineData("--executable")]
    public void Parser_rejects_arbitrary_execution_options(string forbiddenOption)
    {
        var result = AgentBootstrapOptionsParser.Parse([forbiddenOption, "anything"]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.option_not_allowed", result.ErrorCode);
    }

    [Fact]
    public void Parser_rejects_duplicate_option()
    {
        var sessionId = SessionId.New();
        var result = AgentBootstrapOptionsParser.Parse([
            "--pipeName", AgentPipeName.CreateFullName(sessionId, AgentNonce.GeneratePipeToken()),
            "--pipeName", AgentPipeName.CreateFullName(sessionId, AgentNonce.GeneratePipeToken()),
            "--sessionId", sessionId.ToString(),
            "--bootstrapNonce", AgentNonce.GenerateBootstrapNonce(),
            "--protocolVersion", ProtocolVersion.Current.ToString()
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.option_duplicate", result.ErrorCode);
    }

    [Fact]
    public void Parser_rejects_option_without_value()
    {
        var result = AgentBootstrapOptionsParser.Parse(["--pipeName"]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.value_missing", result.ErrorCode);
    }

    [Theory]
    [InlineData(@"\\.\pipe\Other.Agent.00000000-0000-0000-0000-000000000000")]
    [InlineData(@"\\.\pipe\BorealBoost.Agent.not-a-guid.token")]
    public void Parser_rejects_invalid_pipe(string pipeName)
    {
        var sessionId = SessionId.New();
        var result = AgentBootstrapOptionsParser.Parse([
            "--pipeName", pipeName,
            "--sessionId", sessionId.ToString(),
            "--bootstrapNonce", AgentNonce.GenerateBootstrapNonce(),
            "--protocolVersion", ProtocolVersion.Current.ToString()
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.pipe_invalid", result.ErrorCode);
    }

    [Fact]
    public void Parser_rejects_pipe_that_does_not_match_session()
    {
        var result = AgentBootstrapOptionsParser.Parse([
            "--pipeName", AgentPipeName.CreateFullName(SessionId.New(), AgentNonce.GeneratePipeToken()),
            "--sessionId", SessionId.New().ToString(),
            "--bootstrapNonce", AgentNonce.GenerateBootstrapNonce(),
            "--protocolVersion", ProtocolVersion.Current.ToString()
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.pipe_session_mismatch", result.ErrorCode);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Parser_rejects_invalid_session_id(string sessionId)
    {
        var pipeSession = SessionId.New();
        var result = AgentBootstrapOptionsParser.Parse([
            "--pipeName", AgentPipeName.CreateFullName(pipeSession, AgentNonce.GeneratePipeToken()),
            "--sessionId", sessionId,
            "--bootstrapNonce", AgentNonce.GenerateBootstrapNonce(),
            "--protocolVersion", ProtocolVersion.Current.ToString()
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.session_invalid", result.ErrorCode);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("abc+invalid/base64url/characters")]
    public void Parser_rejects_invalid_nonce(string nonce)
    {
        var sessionId = SessionId.New();
        var result = AgentBootstrapOptionsParser.Parse([
            "--pipeName", AgentPipeName.CreateFullName(sessionId, AgentNonce.GeneratePipeToken()),
            "--sessionId", sessionId.ToString(),
            "--bootstrapNonce", nonce,
            "--protocolVersion", ProtocolVersion.Current.ToString()
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.nonce_invalid", result.ErrorCode);
    }

    [Theory]
    [InlineData("2.0.0")]
    [InlineData("invalid")]
    public void Parser_rejects_incompatible_protocol(string protocolVersion)
    {
        var sessionId = SessionId.New();
        var result = AgentBootstrapOptionsParser.Parse([
            "--pipeName", AgentPipeName.CreateFullName(sessionId, AgentNonce.GeneratePipeToken()),
            "--sessionId", sessionId.ToString(),
            "--bootstrapNonce", AgentNonce.GenerateBootstrapNonce(),
            "--protocolVersion", protocolVersion
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("agent.bootstrap.protocol_invalid", result.ErrorCode);
    }
}
