using System.Text;
using ApriP7M.Core.Detection;
using Xunit;

namespace ApriP7M.Core.Tests;

public class FileTypeDetectorTests
{
    [Fact]
    public void Detect_P7mByExtension_ReturnsP7m()
    {
        var header = "anything"u8.ToArray();
        Assert.Equal(FileKind.P7m, FileTypeDetector.Detect(header, "documento.pdf.p7m"));
        Assert.Equal(FileKind.P7m, FileTypeDetector.Detect(header, "fattura.xml.p7m"));
        Assert.Equal(FileKind.P7m, FileTypeDetector.Detect(header, "file.p7m"));
    }

    [Fact]
    public void Detect_PdfByMagic_ReturnsPdf()
    {
        var header = "%PDF-1.7 ..."u8.ToArray();
        Assert.Equal(FileKind.Pdf, FileTypeDetector.Detect(header, "senza-estensione"));
    }

    [Fact]
    public void Detect_ZipByMagic_ReturnsZip()
    {
        byte[] header = { 0x50, 0x4B, 0x03, 0x04, 0x00 };
        Assert.Equal(FileKind.Zip, FileTypeDetector.Detect(header, "archivio"));
    }

    [Fact]
    public void Detect_InvoiceXml_ReturnsInvoiceXml()
    {
        var xml = Encoding.UTF8.GetBytes(
            "<?xml version=\"1.0\"?><p:FatturaElettronica xmlns:p=\"x\"><a/></p:FatturaElettronica>");
        Assert.Equal(FileKind.InvoiceXml, FileTypeDetector.Detect(xml, "fattura.xml"));
    }

    [Fact]
    public void Detect_GenericXml_ReturnsXml()
    {
        var xml = "<?xml version=\"1.0\"?><root/>"u8.ToArray();
        Assert.Equal(FileKind.Xml, FileTypeDetector.Detect(xml, "dati.xml"));
    }

    [Fact]
    public void ToGenericLabel_NeverLeaksFileName()
    {
        Assert.Equal("P7M", FileTypeDetector.ToGenericLabel(FileKind.P7m));
        Assert.Equal("XML_FATTURA", FileTypeDetector.ToGenericLabel(FileKind.InvoiceXml));
        Assert.Equal("SCONOSCIUTO", FileTypeDetector.ToGenericLabel(FileKind.Unknown));
    }
}
