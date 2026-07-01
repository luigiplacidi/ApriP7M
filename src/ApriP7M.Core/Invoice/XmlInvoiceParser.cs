using System.Xml;
using System.Xml.Linq;

namespace ApriP7M.Core.Invoice;

/// <summary>
/// Parser della FatturaPA (FatturaElettronica). Estrae i campi necessari alla
/// copia leggibile in PDF.
///
/// Sicurezza: la lettura XML disabilita la risoluzione delle entità esterne
/// (niente DTD, niente XXE). Tutta l'analisi è in locale.
/// </summary>
public static class XmlInvoiceParser
{
    private const string Stage = "Invoice.XmlInvoiceParser";

    public static InvoiceModel Parse(byte[] xmlBytes)
    {
        if (xmlBytes is null || xmlBytes.Length == 0)
        {
            throw new ApriP7MException(ErrorCode.EmptyFile,
                "Il file XML è vuoto.", Stage);
        }

        XDocument doc;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, // no DTD
                XmlResolver = null,                     // no entità esterne (anti-XXE)
                MaxCharactersFromEntities = 0
            };

            using var ms = new MemoryStream(xmlBytes);
            using var reader = XmlReader.Create(ms, settings);
            doc = XDocument.Load(reader);
        }
        catch (Exception ex)
        {
            throw new ApriP7MException(ErrorCode.InvoiceParseFailed,
                "Il file XML della fattura non è leggibile o è danneggiato.", Stage, ex);
        }

        var root = doc.Root;
        if (root is null || !root.Name.LocalName.Equals("FatturaElettronica", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApriP7MException(ErrorCode.InvoiceParseFailed,
                "Questo XML non sembra una fattura elettronica.", Stage);
        }

        var model = new InvoiceModel
        {
            FormatVersion = Attr(root, "versione")
        };

        var header = El(root, "FatturaElettronicaHeader");
        if (header is not null)
        {
            model.Supplier = ParseParty(El(header, "CedentePrestatore"));
            model.Customer = ParseParty(El(header, "CessionarioCommittente"));
        }

        foreach (var body in Els(root, "FatturaElettronicaBody"))
        {
            model.Documents.Add(ParseBody(body));
            CollectAttachments(body, model.Attachments);
        }

        if (model.Documents.Count == 0)
        {
            throw new ApriP7MException(ErrorCode.InvoiceParseFailed,
                "La fattura non contiene alcun corpo documento.", Stage);
        }

        return model;
    }

    private static Party ParseParty(XElement? partyEl)
    {
        var party = new Party();
        if (partyEl is null)
        {
            return party;
        }

        var anagrafica = Descendant(partyEl, "Anagrafica");
        if (anagrafica is not null)
        {
            var denom = Val(anagrafica, "Denominazione");
            if (!string.IsNullOrEmpty(denom))
            {
                party.Name = denom;
            }
            else
            {
                var nome = Val(anagrafica, "Nome");
                var cognome = Val(anagrafica, "Cognome");
                party.Name = $"{nome} {cognome}".Trim();
            }
        }

        var idIva = Descendant(partyEl, "IdFiscaleIVA");
        if (idIva is not null)
        {
            var paese = Val(idIva, "IdPaese");
            var codice = Val(idIva, "IdCodice");
            party.VatNumber = $"{paese}{codice}".Trim();
        }

        party.FiscalCode = Descendant(partyEl, "CodiceFiscale")?.Value.Trim() ?? "";

        var sede = Descendant(partyEl, "Sede");
        if (sede is not null)
        {
            var addr = string.Join(" ", new[]
            {
                Val(sede, "Indirizzo"),
                Val(sede, "NumeroCivico"),
                "-",
                Val(sede, "CAP"),
                Val(sede, "Comune"),
                Val(sede, "Provincia") is { Length: > 0 } prov ? $"({prov})" : ""
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
            party.Address = addr.Replace(" - ", " - ").Trim();
        }

        return party;
    }

    private static InvoiceDocument ParseBody(XElement body)
    {
        var doc = new InvoiceDocument();

        var dati = El(body, "DatiGenerali");
        var generale = dati is null ? null : El(dati, "DatiGeneraliDocumento");
        if (generale is not null)
        {
            doc.DocumentType = Val(generale, "TipoDocumento");
            doc.Number = Val(generale, "Numero");
            doc.Date = Val(generale, "Data");
            doc.Currency = Val(generale, "Divisa");
            doc.TotalAmount = Val(generale, "ImportoTotaleDocumento");
        }

        var beniServizi = El(body, "DatiBeniServizi");
        if (beniServizi is not null)
        {
            foreach (var linea in Els(beniServizi, "DettaglioLinee"))
            {
                doc.Lines.Add(new InvoiceLine
                {
                    LineNumber = Val(linea, "NumeroLinea"),
                    Description = Val(linea, "Descrizione"),
                    Quantity = Val(linea, "Quantita"),
                    UnitPrice = Val(linea, "PrezzoUnitario"),
                    TotalPrice = Val(linea, "PrezzoTotale"),
                    VatRate = Val(linea, "AliquotaIVA")
                });
            }

            foreach (var riepilogo in Els(beniServizi, "DatiRiepilogo"))
            {
                doc.VatSummaries.Add(new VatSummary
                {
                    VatRate = Val(riepilogo, "AliquotaIVA"),
                    TaxableAmount = Val(riepilogo, "ImponibileImporto"),
                    Tax = Val(riepilogo, "Imposta")
                });
            }
        }

        return doc;
    }

    private static void CollectAttachments(XElement body, List<EmbeddedAttachment> into)
    {
        foreach (var allegato in Els(body, "Allegati"))
        {
            var b64 = Val(allegato, "Attachment");
            if (string.IsNullOrWhiteSpace(b64))
            {
                continue;
            }

            byte[] content;
            try
            {
                content = Convert.FromBase64String(b64.Trim());
            }
            catch
            {
                continue; // allegato non decodificabile: lo saltiamo senza fallire la fattura
            }

            into.Add(new EmbeddedAttachment
            {
                FileName = Val(allegato, "NomeAttachment"),
                Format = Val(allegato, "FormatoAttachment"),
                Content = content
            });
        }
    }

    // --- Helper: la FatturaPA può avere o meno prefisso di namespace, quindi
    //     confrontiamo sempre per LocalName. ---

    private static XElement? El(XElement parent, string localName)
        => parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    private static IEnumerable<XElement> Els(XElement parent, string localName)
        => parent.Elements().Where(e => e.Name.LocalName == localName);

    private static XElement? Descendant(XElement parent, string localName)
        => parent.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

    private static string Val(XElement parent, string localName)
        => El(parent, localName)?.Value.Trim() ?? "";

    private static string Attr(XElement el, string localName)
        => el.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value.Trim() ?? "";
}
