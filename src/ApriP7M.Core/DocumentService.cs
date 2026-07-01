using ApriP7M.Core.Detection;
using ApriP7M.Core.Invoice;
using ApriP7M.Core.P7m;
using ApriP7M.Core.Pdf;
using ApriP7M.Core.Zip;

namespace ApriP7M.Core;

/// <summary>Un risultato pronto per essere mostrato/salvato.</summary>
public sealed class OpenedDocument
{
    public required string DisplayName { get; init; }
    public required FileKind Kind { get; init; }

    /// <summary>Contenuto originale estratto (PDF/XML/altro).</summary>
    public required byte[] OriginalContent { get; init; }
    public required string OriginalExtension { get; init; }

    /// <summary>PDF leggibile di cortesia, presente solo per le fatture XML.</summary>
    public byte[]? ReadablePdf { get; init; }

    /// <summary>Allegati estratti (es. da fattura XML).</summary>
    public IReadOnlyList<EmbeddedAttachment> Attachments { get; init; } = Array.Empty<EmbeddedAttachment>();
}

/// <summary>
/// Punto d'ingresso della logica: dato un file, decide cosa fare e produce uno o
/// più <see cref="OpenedDocument"/>. Tutto in locale, niente rete.
/// </summary>
public sealed class DocumentService
{
    /// <summary>Apre un file dal disco e restituisce i documenti risultanti.</summary>
    public IReadOnlyList<OpenedDocument> Open(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new ApriP7MException(ErrorCode.FileNotFound,
                "Il file non esiste o non è più disponibile.", "DocumentService");
        }

        var kind = FileTypeDetector.Detect(filePath);
        var fileName = Path.GetFileName(filePath);
        var bytes = File.ReadAllBytes(filePath);
        return Open(bytes, fileName, kind);
    }

    /// <summary>Apre un buffer (es. una entry di uno ZIP) e ne ricava i documenti.</summary>
    public IReadOnlyList<OpenedDocument> Open(byte[] bytes, string fileName, FileKind? known = null)
    {
        var kind = known ?? FileTypeDetector.Detect(bytes, fileName);

        return kind switch
        {
            FileKind.P7m => OpenFromP7m(bytes, fileName),
            FileKind.InvoiceXml => new[] { OpenFromInvoiceXml(bytes, fileName) },
            FileKind.Xml => new[] { PassThrough(bytes, fileName, FileKind.Xml, "xml") },
            FileKind.Pdf => new[] { PassThrough(bytes, fileName, FileKind.Pdf, "pdf") },
            FileKind.Zip => OpenFromZip(bytes),
            _ => throw new ApriP7MException(ErrorCode.UnsupportedFormat,
                "Questo tipo di file non è supportato da Apri P7M.", "DocumentService")
        };
    }

    private IReadOnlyList<OpenedDocument> OpenFromP7m(byte[] bytes, string fileName)
    {
        var extracted = CmsExtractor.Extract(bytes, fileName);

        // Se dentro il P7M c'è una fattura, generiamo anche il PDF leggibile.
        if (extracted.ContentKind == FileKind.InvoiceXml)
        {
            return new[] { OpenFromInvoiceXml(extracted.Content, StripExt(fileName)) };
        }

        if (extracted.ContentKind == FileKind.Zip)
        {
            return OpenFromZip(extracted.Content);
        }

        return new[]
        {
            new OpenedDocument
            {
                DisplayName = StripExt(fileName),
                Kind = extracted.ContentKind,
                OriginalContent = extracted.Content,
                OriginalExtension = extracted.SuggestedExtension
            }
        };
    }

    private OpenedDocument OpenFromInvoiceXml(byte[] xmlBytes, string fileName)
    {
        var invoice = XmlInvoiceParser.Parse(xmlBytes);
        var pdf = InvoicePdfRenderer.Render(invoice);

        return new OpenedDocument
        {
            DisplayName = StripExt(fileName),
            Kind = FileKind.InvoiceXml,
            OriginalContent = xmlBytes,
            OriginalExtension = "xml",
            ReadablePdf = pdf,
            Attachments = invoice.Attachments
        };
    }

    private IReadOnlyList<OpenedDocument> OpenFromZip(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        var entries = ZipExtractor.Extract(ms);

        var docs = new List<OpenedDocument>();
        foreach (var entry in entries)
        {
            try
            {
                // Ogni entry viene aperta ricorsivamente con la stessa logica.
                docs.AddRange(Open(entry.Content, entry.EntryName, entry.Kind));
            }
            catch (ApriP7MException ex) when (ex.Code is not ErrorCode.ZipPathTraversal and not ErrorCode.ZipCorrupted)
            {
                // In un archivio misto non scartiamo i documenti validi per una
                // singola entry corrotta/non supportata. Gli errori di sicurezza
                // ZIP invece bloccano sempre l'intero archivio.
            }
        }

        if (docs.Count == 0)
        {
            throw new ApriP7MException(ErrorCode.ZipEmpty,
                "L'archivio non contiene file P7M, XML o PDF apribili.", "DocumentService");
        }

        return docs;
    }

    private static OpenedDocument PassThrough(byte[] bytes, string fileName, FileKind kind, string ext)
        => new()
        {
            DisplayName = fileName,
            Kind = kind,
            OriginalContent = bytes,
            OriginalExtension = ext
        };

    private static string StripExt(string fileName)
    {
        var name = fileName;
        if (name.EndsWith(".p7m", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }
        return name;
    }
}
