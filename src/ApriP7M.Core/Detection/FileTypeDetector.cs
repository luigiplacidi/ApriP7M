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

        // .p7m può essere .pdf.p7m, .xml.p7m o semplicemente .p7m
        if (lower.EndsWith(".p7m"))
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
