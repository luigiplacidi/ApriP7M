using System.IO.Compression;
using System.Text;
using ApriP7M.Core;
using ApriP7M.Core.Detection;
using ApriP7M.Core.Zip;
using Xunit;

namespace ApriP7M.Core.Tests;

public class ZipExtractorTests
{
    private static MemoryStream BuildZip(Action<ZipArchive> fill)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            fill(archive);
        }
        ms.Position = 0;
        return ms;
    }

    private static void AddEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name);
        using var s = entry.Open();
        s.Write(content);
    }

    [Fact]
    public void Extract_ReturnsOnlyRelevantEntries()
    {
        using var zip = BuildZip(a =>
        {
            AddEntry(a, "fattura.xml",
                Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><FatturaElettronica/>"));
            AddEntry(a, "documento.pdf", "%PDF-1.4"u8.ToArray());
            AddEntry(a, "note.txt", "irrilevante"u8.ToArray());
        });

        var results = ZipExtractor.Extract(zip);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Kind == FileKind.InvoiceXml);
        Assert.Contains(results, r => r.Kind == FileKind.Pdf);
        Assert.DoesNotContain(results, r => r.EntryName == "note.txt");
    }

    [Fact]
    public void Extract_PathTraversalEntry_Throws()
    {
        using var zip = BuildZip(a =>
            AddEntry(a, "../../evil.xml", "<?xml version=\"1.0\"?><FatturaElettronica/>"u8.ToArray()));

        var ex = Assert.Throws<ApriP7MException>(() => ZipExtractor.Extract(zip));
        Assert.Equal(ErrorCode.ZipPathTraversal, ex.Code);
    }

    [Fact]
    public void Extract_NoRelevantFiles_ThrowsZipEmpty()
    {
        using var zip = BuildZip(a => AddEntry(a, "readme.txt", "ciao"u8.ToArray()));

        var ex = Assert.Throws<ApriP7MException>(() => ZipExtractor.Extract(zip));
        Assert.Equal(ErrorCode.ZipEmpty, ex.Code);
    }
}
