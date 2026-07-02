using System.Text;
using ApriP7M.Core;
using ApriP7M.Core.Detection;
using ApriP7M.Core.Pdf;
using ApriP7M.Core.Privacy;
using ApriP7M.Core.Settings;
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
/// Verifica le promesse fatte agli utenti su sito, README e scheda Store:
/// elaborazione in locale, estrazione fedele del contenuto P7M, PDF di
/// cortesia per le fatture, pulizia dei file temporanei, diagnostica opt-in.
/// Ogni test cita la promessa che protegge.
/// </summary>
public class ProductPromisesTests
{
    // Fattura FatturaPA sintetica minima (stessa base di XmlInvoiceParserTests).
    private const string SampleInvoice = """
        <?xml version="1.0" encoding="UTF-8"?>
        <p:FatturaElettronica versione="FPR12" xmlns:p="http://ivaservizi.agenziaentrate.gov.it/docs/xsd/fatture/v1.2">
          <FatturaElettronicaHeader>
            <CedentePrestatore>
              <DatiAnagrafici>
                <IdFiscaleIVA><IdPaese>IT</IdPaese><IdCodice>01234567890</IdCodice></IdFiscaleIVA>
                <Anagrafica><Denominazione>Acme S.r.l.</Denominazione></Anagrafica>
              </DatiAnagrafici>
              <Sede><Indirizzo>Via Roma</Indirizzo><NumeroCivico>1</NumeroCivico><CAP>20100</CAP><Comune>Milano</Comune><Provincia>MI</Provincia></Sede>
            </CedentePrestatore>
            <CessionarioCommittente>
              <DatiAnagrafici>
                <CodiceFiscale>RSSMRA80A01F205X</CodiceFiscale>
                <Anagrafica><Nome>Mario</Nome><Cognome>Rossi</Cognome></Anagrafica>
              </DatiAnagrafici>
              <Sede><Indirizzo>Via Verdi</Indirizzo><CAP>00100</CAP><Comune>Roma</Comune></Sede>
            </CessionarioCommittente>
          </FatturaElettronicaHeader>
          <FatturaElettronicaBody>
            <DatiGenerali>
              <DatiGeneraliDocumento>
                <TipoDocumento>TD01</TipoDocumento>
                <Divisa>EUR</Divisa>
                <Data>2026-01-15</Data>
                <Numero>2026/001</Numero>
                <ImportoTotaleDocumento>122.00</ImportoTotaleDocumento>
              </DatiGeneraliDocumento>
            </DatiGenerali>
            <DatiBeniServizi>
              <DettaglioLinee>
                <NumeroLinea>1</NumeroLinea>
                <Descrizione>Consulenza</Descrizione>
                <Quantita>1.00</Quantita>
                <PrezzoUnitario>100.00</PrezzoUnitario>
                <PrezzoTotale>100.00</PrezzoTotale>
                <AliquotaIVA>22.00</AliquotaIVA>
              </DettaglioLinee>
              <DatiRiepilogo>
                <AliquotaIVA>22.00</AliquotaIVA>
                <ImponibileImporto>100.00</ImponibileImporto>
                <Imposta>22.00</Imposta>
              </DatiRiepilogo>
            </DatiBeniServizi>
          </FatturaElettronicaBody>
        </p:FatturaElettronica>
        """;

    // ------------------------------------------------------------------
    // Promessa: "Nessun upload. Tutto lavora sul tuo PC."
    // La logica di elaborazione (ApriP7M.Core) non deve nemmeno referenziare
    // le API di rete: se qualcuno le introduce, questo test si rompe.
    // ------------------------------------------------------------------
    [Fact]
    public void Core_DoesNotReferenceNetworkAssemblies()
    {
        var referenced = typeof(DocumentService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        string[] forbidden =
        {
            "System.Net.Http",
            "System.Net.Sockets",
            "System.Net.Requests",
            "System.Net.WebClient",
            "System.Net.WebSockets",
            "System.Net.WebSockets.Client",
        };

        var violations = referenced.Where(forbidden.Contains).ToArray();
        Assert.Empty(violations);
    }

    // ------------------------------------------------------------------
    // Promessa: "Apre il file .p7m ricevuto via PEC ed estrae il documento
    // originale contenuto dentro."
    // Busta CAdES vera (firmata con certificato di test) → estrazione fedele.
    // ------------------------------------------------------------------
    [Fact]
    public void Open_XmlP7m_ExtractsOriginalContentUnchanged()
    {
        var original = Encoding.UTF8.GetBytes(SampleInvoice);
        var p7m = WrapInSignedCms(original);

        var docs = new DocumentService().Open(p7m, "fattura.xml.p7m");

        var doc = Assert.Single(docs);
        Assert.Equal(original, doc.OriginalContent);
    }

    // ------------------------------------------------------------------
    // Promessa: "Quando l'XML è una fattura, genera anche un PDF di
    // cortesia più facile da leggere e stampare."
    // ------------------------------------------------------------------
    [Fact]
    public void Open_InvoiceXml_ProducesCourtesyPdf()
    {
        var xml = Encoding.UTF8.GetBytes(SampleInvoice);

        var docs = new DocumentService().Open(xml, "fattura.xml");

        var doc = Assert.Single(docs);
        Assert.Equal(FileKind.InvoiceXml, doc.Kind);
        Assert.NotNull(doc.ReadablePdf);
        AssertIsPdf(doc.ReadablePdf!);
    }

    [Fact]
    public void Open_InvoiceXmlP7m_DetectsInvoiceAndProducesCourtesyPdf()
    {
        var xml = Encoding.UTF8.GetBytes(SampleInvoice);
        var p7m = WrapInSignedCms(xml);

        var docs = new DocumentService().Open(p7m, "fattura.xml.p7m");

        var doc = Assert.Single(docs);
        Assert.Equal(FileKind.InvoiceXml, doc.Kind);
        Assert.NotNull(doc.ReadablePdf);
        AssertIsPdf(doc.ReadablePdf!);
    }

    // ------------------------------------------------------------------
    // Promessa: "Il PDF è una copia di cortesia, il documento fiscale
    // resta l'XML originale." L'avviso deve dirlo esplicitamente.
    // ------------------------------------------------------------------
    [Fact]
    public void CourtesyNotice_StatesPdfHasNoFiscalValue()
    {
        Assert.Contains("copia leggibile di cortesia", InvoicePdfRenderer.CourtesyNotice);
        Assert.Contains("XML originale", InvoicePdfRenderer.CourtesyNotice);
    }

    // ------------------------------------------------------------------
    // Promessa: "I file temporanei restano sul tuo PC e vengono ripuliti
    // al termine."
    // ------------------------------------------------------------------
    [Fact]
    public void TempFileManager_Dispose_RemovesFilesAndFolder()
    {
        string root;
        string tempFile;

        using (var manager = new TempFileManager())
        {
            root = manager.Root;
            tempFile = manager.WriteTemp("contenuto di prova"u8.ToArray(), ".txt");
            Assert.True(File.Exists(tempFile));
        }

        Assert.False(File.Exists(tempFile));
        Assert.False(Directory.Exists(root));
    }

    // ------------------------------------------------------------------
    // Promessa: "Diagnostica anonima opzionale e disattivata di default."
    // ------------------------------------------------------------------
    [Fact]
    public void Diagnostics_AreDisabledByDefault()
    {
        Assert.False(new AppSettings().DiagnosticsEnabled);
    }

    // ------------------------------------------------------------------
    // Helper: costruisce una busta CMS SignedData (CAdES) reale con un
    // certificato self-signed di test e il contenuto incapsulato, come i
    // .p7m che arrivano via PEC.
    // ------------------------------------------------------------------
    private static byte[] WrapInSignedCms(byte[] content)
    {
        var keyGen = new RsaKeyPairGenerator();
        keyGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
        var keyPair = keyGen.GenerateKeyPair();

        var name = new X509Name("CN=Firmatario di test, O=ApriP7M Tests, C=IT");
        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(BigInteger.One);
        certGen.SetIssuerDN(name);
        certGen.SetSubjectDN(name);
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddDays(1));
        certGen.SetPublicKey(keyPair.Public);
        var cert = certGen.Generate(new Asn1SignatureFactory("SHA256WITHRSA", keyPair.Private));

        var generator = new CmsSignedDataGenerator();
        generator.AddSignerInfoGenerator(
            new SignerInfoGeneratorBuilder().Build(
                new Asn1SignatureFactory("SHA256WITHRSA", keyPair.Private), cert));

        var signed = generator.Generate(new CmsProcessableByteArray(content), encapsulate: true);
        return signed.GetEncoded();
    }

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.True(bytes.Length > 4, "PDF vuoto");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
