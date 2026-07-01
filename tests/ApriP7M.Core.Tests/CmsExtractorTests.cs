using ApriP7M.Core;
using ApriP7M.Core.P7m;
using Xunit;

namespace ApriP7M.Core.Tests;

public class CmsExtractorTests
{
    [Fact]
    public void Extract_EmptyInput_ThrowsEmptyFile()
    {
        var ex = Assert.Throws<ApriP7MException>(
            () => CmsExtractor.Extract(Array.Empty<byte>(), "vuoto.p7m"));
        Assert.Equal(ErrorCode.EmptyFile, ex.Code);
    }

    [Fact]
    public void Extract_NotCms_ThrowsNotValidCms()
    {
        // Sequenza DER plausibile ma non un CMS SignedData.
        byte[] garbage = { 0x30, 0x03, 0x02, 0x01, 0x05 };
        var ex = Assert.Throws<ApriP7MException>(
            () => CmsExtractor.Extract(garbage, "fake.p7m"));
        Assert.Equal(ErrorCode.NotValidCms, ex.Code);
    }

    [Fact]
    public void Extract_PlainText_ThrowsNotValidCms()
    {
        var ex = Assert.Throws<ApriP7MException>(
            () => CmsExtractor.Extract("non sono un p7m"u8.ToArray(), "fake.p7m"));
        Assert.Equal(ErrorCode.NotValidCms, ex.Code);
    }
}
