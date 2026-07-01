using ApriP7M.Core;
using ApriP7M.Core.Detection;
using ApriP7M.Core.Diagnostics;
using ApriP7M.Core.Privacy;
using ApriP7M.Core.Reviews;
using Xunit;

namespace ApriP7M.Core.Tests;

public class PrivacyAndDiagnosticsTests
{
    [Theory]
    [InlineData(50_000, "<100KB")]
    [InlineData(500_000, "100KB-1MB")]
    [InlineData(3_000_000, "1-5MB")]
    [InlineData(200_000_000, ">100MB")]
    public void SizeBucket_MapsToCoarseRanges(long bytes, string expected)
        => Assert.Equal(expected, SizeBucket.For(bytes));

    [Fact]
    public void SafeLogger_Redact_RemovesPathsAndFileNames()
    {
        var input = @"Errore aprendo C:\Users\mario\Documenti\fattura_riservata.pdf";
        var redacted = SafeLogger.Redact(input);

        Assert.DoesNotContain("mario", redacted);
        Assert.DoesNotContain("fattura_riservata", redacted);
        Assert.DoesNotContain(@"C:\Users", redacted);
    }

    [Fact]
    public void DiagnosticBuilder_ProducesMinimizedPayload()
    {
        var builder = new DiagnosticBuilder("0.1.0", "Windows 11 23H2", "it");
        var error = new ApriP7MException(ErrorCode.NotValidCms, "Non valido", "P7m.CmsExtractor");
        var now = new DateTimeOffset(2026, 6, 23, 14, 37, 11, TimeSpan.Zero);

        var payload = builder.FromError(error, FileKind.P7m, 3_000_000, now);

        Assert.Equal("0.1.0", payload.AppVersion);
        Assert.Equal("P7M", payload.FileKind);
        Assert.Equal("1-5MB", payload.FileSizeBucket);
        Assert.Equal((int)ErrorCode.NotValidCms, payload.ErrorCode);
        Assert.Equal("P7m.CmsExtractor", payload.ErrorStage);
        Assert.Equal("P7m", payload.Module);
        // Timestamp arrotondato all'ora.
        Assert.Equal("2026-06-23T14:00:00Z", payload.TimestampHour);

        // Il payload NON deve poter contenere percorsi o contenuti.
        var json = payload.ToPreviewJson();
        Assert.DoesNotContain("C:\\", json);
    }
}

public class ReviewPromptPolicyTests
{
    private static ReviewState Healthy(DateTimeOffset now) => new()
    {
        FirstLaunchUtc = now.AddDays(-5),
        SuccessfulOperations = 3,
        UserOptedOut = false,
        AlreadyShown = false
    };

    [Fact]
    public void ShouldPrompt_AfterPositiveUse_True()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.True(ReviewPromptPolicy.ShouldPrompt(Healthy(now), now));
    }

    [Fact]
    public void ShouldPrompt_TooFewOperations_False()
    {
        var now = DateTimeOffset.UtcNow;
        var state = Healthy(now);
        state.SuccessfulOperations = 2;
        Assert.False(ReviewPromptPolicy.ShouldPrompt(state, now));
    }

    [Fact]
    public void ShouldPrompt_TooSoonAfterFirstLaunch_False()
    {
        var now = DateTimeOffset.UtcNow;
        var state = Healthy(now);
        state.FirstLaunchUtc = now.AddHours(-2);
        Assert.False(ReviewPromptPolicy.ShouldPrompt(state, now));
    }

    [Fact]
    public void ShouldPrompt_RecentImportantError_False()
    {
        var now = DateTimeOffset.UtcNow;
        var state = Healthy(now);
        state.LastImportantErrorUtc = now.AddMinutes(-30);
        Assert.False(ReviewPromptPolicy.ShouldPrompt(state, now));
    }

    [Fact]
    public void ShouldPrompt_OptedOut_False()
    {
        var now = DateTimeOffset.UtcNow;
        var state = Healthy(now);
        state.UserOptedOut = true;
        Assert.False(ReviewPromptPolicy.ShouldPrompt(state, now));
    }
}
