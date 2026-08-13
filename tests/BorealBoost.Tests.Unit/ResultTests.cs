using BorealBoost.Core.Common;
using BorealBoost.Core.Operations;

namespace BorealBoost.Tests.Unit;

public sealed class ResultTests
{
    [Fact]
    public void SuccessResult_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void OperationResult_failed_preserves_error_details()
    {
        var result = OperationResult.Failed(
            "foundation.failure",
            "Foundation operation failed.",
            "InvalidState",
            "Detailed error",
            TimeSpan.FromMilliseconds(12));

        Assert.False(result.Success);
        Assert.Equal("InvalidState", result.ErrorType);
        Assert.Equal("Detailed error", result.ErrorMessage);
    }
}
