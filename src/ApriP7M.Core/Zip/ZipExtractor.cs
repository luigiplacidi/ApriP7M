using System.IO.Compression;
using ApriP7M.Core.Detection;

namespace ApriP7M.Core.Zip;

/// <summary>
/// Estrae in memoria le voci rilevanti (XML, P7M, PDF, ZIP) da un archivio ZIP.
/// Protetto contro zip-slip / path traversal: i nomi delle entry vengono
/// normalizzati a solo file, mai usati come percorso di scrittura grezzo.
/// </summary>
public static class ZipExtractor
{
    private const string Stage = "Zip.ZipExtractor";

    // Limiti difensivi contro zip-bomb.
    private const long MaxTotalUncompressedBytes = 500L * 1024 * 1024; // 500 MB
    private const int MaxEntries = 2000;

    public static IReadOnlyList<ZipEntryResult> Extract(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new ApriP7MException(ErrorCode.FileNotFound,
                "L'archivio ZIP non esiste o non è più disponibile.", Stage);
        }

        using var stream = File.OpenRead(filePath);
        return Extract(stream);
    }

    public static IReadOnlyList<ZipEntryResult> Extract(Stream zipStream)
    {
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (Exception ex)
        {
            throw new ApriP7MException(ErrorCode.ZipCorrupted,
                "L'archivio ZIP è danneggiato o non valido.", Stage, ex);
        }

        var results = new List<ZipEntryResult>();
        long total = 0;
        var count = 0;

        using (archive)
        {
            foreach (var entry in archive.Entries)
            {
                // Salta le cartelle.
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                if (++count > MaxEntries)
                {
                    throw new ApriP7MException(ErrorCode.ZipCorrupted,
                        "L'archivio contiene troppi file: per sicurezza non viene aperto.", Stage);
                }

                // Protezione zip-slip: usiamo SOLO il nome del file, scartando
                // qualsiasi componente di percorso (../, percorsi assoluti, ecc.).
                var safeName = Path.GetFileName(entry.FullName);
                if (string.IsNullOrEmpty(safeName) || ContainsTraversal(entry.FullName))
                {
                    throw new ApriP7MException(ErrorCode.ZipPathTraversal,
                        "L'archivio contiene un percorso non sicuro ed è stato bloccato.", Stage);
                }

                byte[] content = ReadEntry(entry, ref total);
                var kind = FileTypeDetector.Detect(content, safeName);

                // Ci interessano solo i tipi gestibili da Apri P7M.
                if (kind is FileKind.P7m or FileKind.Pdf or FileKind.InvoiceXml or FileKind.Xml or FileKind.Zip)
                {
                    results.Add(new ZipEntryResult
                    {
                        EntryName = safeName,
                        Content = content,
                        Kind = kind
                    });
                }
            }
        }

        if (results.Count == 0)
        {
            throw new ApriP7MException(ErrorCode.ZipEmpty,
                "L'archivio non contiene file P7M, XML o PDF da aprire.", Stage);
        }

        return results;
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry, ref long total)
    {
        total += entry.Length;
        if (total > MaxTotalUncompressedBytes)
        {
            throw new ApriP7MException(ErrorCode.ZipCorrupted,
                "L'archivio è troppo grande una volta decompresso: per sicurezza non viene aperto.", Stage);
        }

        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        return ms.ToArray();
    }

    private static bool ContainsTraversal(string fullName)
    {
        var normalized = fullName.Replace('\\', '/');
        return normalized.Contains("../", StringComparison.Ordinal)
            || normalized.StartsWith('/')
            || (normalized.Length > 1 && normalized[1] == ':'); // C:\ ...
    }
}
