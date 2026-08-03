using System.Text;

namespace ApriP7M.Core.Detection;

/// <summary>
/// Riconosce il tipo logico di un file in base a estensione e firma binaria
/// (magic bytes), senza fidarsi solo del nome. Non legge mai più del necessario.
/// </summary>
public static class FileTypeDetector
{
    // Magic bytes noti
    private static readonly byte[] PdfMagic = "%PDF"u8.ToArray();
    private static readonly byte[] ZipMagic = { 0x50, 0x4B, 0x03, 0x04 }; // PK..

    // Corpo dell'OID PKCS#7 signedData (1.2.840.113549.1.7.2): identifica un
    // contenitore CMS firmato indipendentemente dal nome del file. Serve a
    // riconoscere i .p7m annidati (doppia firma) o rinominati.
    private static readonly byte[] SignedDataOid =
        { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02 };

    /// <summary>
    /// Determina il <see cref="FileKind"/> da un percorso file.
    /// Legge solo un'intestazione limitata dal disco.
    /// </summary>
    public static FileKind Detect(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var header = ReadHeader(filePath, 4096);
        return Detect(header, Path.GetFileName(filePath));
    }

    /// <summary>
    /// Determina il <see cref="FileKind"/> da un buffer in memoria e da un nome
    /// (anche solo logico, es. una entry di uno ZIP).
    /// </summary>
    public static FileKind Detect(ReadOnlySpan<byte> header, string fileName)
    {
        var lower = fileName.ToLowerInvariant();

        // .p7m può essere .pdf.p7m, .xml.p7m o semplicemente .p7m. Riconosciamo
        // anche il contenitore CMS firmato dai byte, così i file firmati due
        // volte o rinominati vengono comunque srotolati fino al documento.
        if (lower.EndsWith(".p7m") || LooksLikeCmsSignedData(header))
        {
            return FileKind.P7m;
        }

        if (StartsWith(header, ZipMagic) || lower.EndsWith(".zip"))
        {
            return FileKind.Zip;
        }

        if (StartsWith(header, PdfMagic) || lower.EndsWith(".pdf"))
        {
            return FileKind.Pdf;
        }

        if (lower.EndsWith(".xml") || LooksLikeXml(header))
        {
            return LooksLikeInvoice(header) ? FileKind.InvoiceXml : FileKind.Xml;
        }

        // Un .p7m senza estensione: euristica su DER SEQUENCE (0x30) — non
        // affidabile da sola, quindi resta Unknown e lascia decidere all'estrattore.
        return FileKind.Unknown;
    }

    /// <summary>
    /// Restituisce il tipo generico da inviare nella diagnostica anonima
    /// (mai il nome reale del file).
    /// </summary>
    public static string ToGenericLabel(FileKind kind) => kind switch
    {
        FileKind.P7m => "P7M",
        FileKind.Pdf => "PDF",
        FileKind.InvoiceXml => "XML_FATTURA",
        FileKind.Xml => "XML",
        FileKind.Zip => "ZIP",
        _ => "SCONOSCIUTO"
    };

    private static byte[] ReadHeader(string filePath, int maxBytes)
    {
        using var stream = File.OpenRead(filePath);
        var length = (int)Math.Min(maxBytes, stream.Length);
        var buffer = new byte[length];
        var read = stream.Read(buffer, 0, length);
        return read == length ? buffer : buffer[..read];
    }

    private static bool StartsWith(ReadOnlySpan<byte> data, ReadOnlySpan<byte> prefix)
        => data.Length >= prefix.Length && data[..prefix.Length].SequenceEqual(prefix);

    /// <summary>
    /// Riconosce un contenitore CMS/PKCS#7 SignedData (.p7m) dai byte: SEQUENCE
    /// DER iniziale (0x30) e presenza dell'OID signedData nell'intestazione.
    /// Non valida la firma: serve solo a instradare l'estrazione.
    /// </summary>
    private static bool LooksLikeCmsSignedData(ReadOnlySpan<byte> header)
    {
        // Un CMS DER inizia con SEQUENCE. Escludiamo subito gli altri formati.
        if (header.Length < 2 || header[0] != 0x30)
        {
            return false;
        }

        // L'OID signedData compare all'inizio del ContentInfo. Lo cerchiamo in
        // una finestra ristretta per evitare falsi positivi su file grandi.
        var window = header.Length < 64 ? header : header[..64];
        return IndexOf(window, SignedDataOid) >= 0;
    }

    private static int IndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
        {
            return -1;
        }

        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.Slice(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool LooksLikeXml(ReadOnlySpan<byte> header)
    {
        var text = DecodeStart(header, 256).TrimStart('﻿', ' ', '\r', '\n', '\t');
        return text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<", StringComparison.Ordinal);
    }

    private static bool LooksLikeInvoice(ReadOnlySpan<byte> header)
    {
        var text = DecodeStart(header, 2048);
        // Elemento radice della FatturaPA, con o senza prefisso di namespace.
        return text.Contains("FatturaElettronica", StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodeStart(ReadOnlySpan<byte> header, int max)
    {
        var len = Math.Min(max, header.Length);
        return Encoding.UTF8.GetString(header[..len]);
    }
}
