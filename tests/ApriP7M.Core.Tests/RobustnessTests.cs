using System.Text;
using ApriP7M.Core;
using ApriP7M.Core.Detection;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Xunit;

namespace ApriP7M.Core.Tests;

/// <summary>
/// Riproduzione dei casi in cui l'app non riesce a mostrare l'anteprima anche
/// se il contenuto è apribile: file firmati due volte (.p7m annidati), nomi
/// generici, contenuto rilevato solo dai magic bytes.
/// </summary>
public class RobustnessTests
{
    private static readonly byte[] MinimalPdf = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF");

    // --- Il caso principale: file firmato DUE volte -------------------------
    // Molti .p7m ricevuti via PEC sono stati firmati più volte (controfirma o
    // doppia firma). Il contenuto interno è a sua volta un .p7m.

    [Fact]
    public void Open_DoubleSignedPdf_YieldsPdf()
    {
        var signer = CreateTestSigner();
        var once = WrapInSignedCms(signer, MinimalPdf);
        var twice = WrapInSignedCms(signer, once);

        var docs = new DocumentService().Open(twice, "documento.pdf.p7m.p7m");

        var doc = Assert.Single(docs);
        Assert.Equal(FileKind.Pdf, doc.Kind);
        Assert.Equal(MinimalPdf, doc.OriginalContent);
    }

    [Fact]
    public void Open_DoubleSignedPdf_SingleP7mExtension_YieldsPdf()
    {
        // A volte il doppio incapsulamento non si riflette nel nome del file.
        var signer = CreateTestSigner();
        var twice = WrapInSignedCms(signer, WrapInSignedCms(signer, MinimalPdf));

        var docs = new DocumentService().Open(twice, "firmato.p7m");

        var doc = Assert.Single(docs);
        Assert.Equal(FileKind.Pdf, doc.Kind);
    }

    // --- Nome generico: il tipo va riconosciuto dal contenuto ---------------

    [Fact]
    public void Open_P7mWithGenericName_DetectsPdfFromContent()
    {
        var signer = CreateTestSigner();
        var p7m = WrapInSignedCms(signer, MinimalPdf);

        var docs = new DocumentService().Open(p7m, "firmato.p7m");

        Assert.Equal(FileKind.Pdf, Assert.Single(docs).Kind);
    }

    [Fact]
    public void Detect_PdfBytes_WithoutExtension_IsPdf()
    {
        Assert.Equal(FileKind.Pdf, FileTypeDetector.Detect(MinimalPdf, "senza-estensione"));
    }

    // --- Un .p7m dentro uno ZIP dev'essere aperto fino al contenuto ---------

    [Fact]
    public void Open_ZipContainingDoubleSignedPdf_YieldsPdf()
    {
        var signer = CreateTestSigner();
        var twice = WrapInSignedCms(signer, WrapInSignedCms(signer, MinimalPdf));

        using var zipStream = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(
                   zipStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = zip.CreateEntry("documento.pdf.p7m.p7m").Open();
            entry.Write(twice);
        }

        var docs = new DocumentService().Open(zipStream.ToArray(), "archivio.zip");

        Assert.Contains(docs, d => d.Kind == FileKind.Pdf);
    }

    // --- Helper CAdES di test -----------------------------------------------

    private static byte[] WrapInSignedCms(
        (AsymmetricCipherKeyPair KeyPair, Org.BouncyCastle.X509.X509Certificate Certificate) signer,
        byte[] content)
    {
        var generator = new CmsSignedDataGenerator();
        generator.AddSignerInfoGenerator(
            new SignerInfoGeneratorBuilder().Build(
                new Asn1SignatureFactory("SHA256WITHRSA", signer.KeyPair.Private), signer.Certificate));
        var signed = generator.Generate(new CmsProcessableByteArray(content), encapsulate: true);
        return signed.GetEncoded();
    }

    private static (AsymmetricCipherKeyPair, Org.BouncyCastle.X509.X509Certificate) CreateTestSigner()
    {
        var keyGen = new RsaKeyPairGenerator();
        keyGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
        var keyPair = keyGen.GenerateKeyPair();

        var name = new X509Name("CN=Firmatario di test, O=ApriP7M, C=IT");
        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(BigInteger.One);
        certGen.SetIssuerDN(name);
        certGen.SetSubjectDN(name);
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddDays(1));
        certGen.SetPublicKey(keyPair.Public);
        var cert = certGen.Generate(new Asn1SignatureFactory("SHA256WITHRSA", keyPair.Private));
        return (keyPair, cert);
    }
}
