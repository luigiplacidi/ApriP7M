namespace ApriP7M.Core.Invoice;

/// <summary>
/// Modello dati minimo di una fattura elettronica (FatturaPA), estratto dall'XML
/// per generare la copia leggibile in PDF. Non è una rappresentazione completa
/// dello standard: copre i campi mostrati nel PDF di cortesia.
/// </summary>
public sealed class InvoiceModel
{
    public string FormatVersion { get; set; } = "";   // es. FPR12 / FPA12

    public Party Supplier { get; set; } = new();        // CedentePrestatore
    public Party Customer { get; set; } = new();        // CessionarioCommittente

    public List<InvoiceDocument> Documents { get; set; } = new();

    public List<EmbeddedAttachment> Attachments { get; set; } = new();
}

/// <summary>Una fattura singola (un blocco FatturaElettronicaBody).</summary>
public sealed class InvoiceDocument
{
    public string DocumentType { get; set; } = "";     // TipoDocumento (es. TD01)
    public string Number { get; set; } = "";           // Numero
    public string Date { get; set; } = "";             // Data
    public string Currency { get; set; } = "";         // Divisa
    public string TotalAmount { get; set; } = "";      // ImportoTotaleDocumento

    public List<InvoiceLine> Lines { get; set; } = new();
    public List<VatSummary> VatSummaries { get; set; } = new();
}

public sealed class Party
{
    public string Name { get; set; } = "";             // Denominazione o Nome+Cognome
    public string VatNumber { get; set; } = "";        // IdFiscaleIVA
    public string FiscalCode { get; set; } = "";       // CodiceFiscale
    public string Address { get; set; } = "";          // Indirizzo + CAP + Comune + Prov
}

public sealed class InvoiceLine
{
    public string LineNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string UnitPrice { get; set; } = "";
    public string TotalPrice { get; set; } = "";
    public string VatRate { get; set; } = "";
}

public sealed class VatSummary
{
    public string VatRate { get; set; } = "";
    public string TaxableAmount { get; set; } = "";    // ImponibileImporto
    public string Tax { get; set; } = "";              // Imposta
}

/// <summary>Allegato incorporato nell'XML (Allegati/Attachment, base64).</summary>
public sealed class EmbeddedAttachment
{
    public string FileName { get; set; } = "";
    public string Format { get; set; } = "";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
