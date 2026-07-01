using System.Text;
using ApriP7M.Core;
using ApriP7M.Core.Invoice;
using Xunit;

namespace ApriP7M.Core.Tests;

public class XmlInvoiceParserTests
{
    // Fattura FatturaPA sintetica minima, con prefisso di namespace.
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

    [Fact]
    public void Parse_ExtractsHeaderAndBody()
    {
        var model = XmlInvoiceParser.Parse(Encoding.UTF8.GetBytes(SampleInvoice));

        Assert.Equal("FPR12", model.FormatVersion);
        Assert.Equal("Acme S.r.l.", model.Supplier.Name);
        Assert.Equal("IT01234567890", model.Supplier.VatNumber);
        Assert.Equal("Mario Rossi", model.Customer.Name);

        var doc = Assert.Single(model.Documents);
        Assert.Equal("TD01", doc.DocumentType);
        Assert.Equal("2026/001", doc.Number);
        Assert.Equal("122.00", doc.TotalAmount);

        var line = Assert.Single(doc.Lines);
        Assert.Equal("Consulenza", line.Description);

        var vat = Assert.Single(doc.VatSummaries);
        Assert.Equal("22.00", vat.VatRate);
    }

    [Fact]
    public void Parse_NonInvoiceXml_Throws()
    {
        var xml = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><root/>");
        var ex = Assert.Throws<ApriP7MException>(() => XmlInvoiceParser.Parse(xml));
        Assert.Equal(ErrorCode.InvoiceParseFailed, ex.Code);
    }

    [Fact]
    public void Parse_RendersCourtesyPdf()
    {
        var model = XmlInvoiceParser.Parse(Encoding.UTF8.GetBytes(SampleInvoice));
        var pdf = ApriP7M.Core.Pdf.InvoicePdfRenderer.Render(model);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0);
        // I PDF iniziano con "%PDF".
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
    }
}
