using ApriP7M.Core.Detection;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Cms;

namespace ApriP7M.Core.P7m;

/// <summary>
/// Estrae il documento originale incapsulato in un file CMS/PKCS#7 (.p7m),
/// incluso il profilo CAdES (CAdES-BES/-T), usando BouncyCastle.
///
/// IMPORTANTE: questa classe NON verifica la validità legale della firma né la
/// catena di certificati. Si limita a recuperare il contenuto firmato
/// (encapContentInfo), come richiesto dal progetto.
/// </summary>
public static class CmsExtractor
{
    private const string Stage = "P7m.CmsExtractor";

    /// <summary>
    /// Estrae il contenuto da un file .p7m su disco.
    /// </summary>
    public static CmsExtractionResult Extract(string filePath, string originalFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new ApriP7MException(ErrorCode.FileNotFound,
                "Il file non esiste o non è più disponibile.", Stage);
        }

        byte[] raw = File.ReadAllBytes(filePath);
        return Extract(raw, originalFileName);
    }

    /// <summary>
    /// Estrae il contenuto da un buffer .p7m in memoria.
    /// </summary>
    /// <param name="p7mBytes">Contenuto del file .p7m (DER o Base64).</param>
    /// <param name="originalFileName">
    /// Nome del file di partenza (es. "documento.pdf.p7m"), usato solo per dedurre
    /// l'estensione del contenuto. Non viene mai loggato.
    /// </param>
    public static CmsExtractionResult Extract(byte[] p7mBytes, string originalFileName)
    {
        if (p7mBytes is null || p7mBytes.Length == 0)
        {
            throw new ApriP7MException(ErrorCode.EmptyFile,
                "Il file è vuoto: non c'è nulla da aprire.", Stage);
        }

        byte[] der = NormalizeToDer(p7mBytes);

        CmsSignedData signedData;
        try
        {
            signedData = new CmsSignedData(der);
        }
        catch (Exception ex)
        {
            throw new ApriP7MException(ErrorCode.NotValidCms,
                "Questo file non sembra un .p7m valido. Potrebbe essere danneggiato o non firmato.",
                Stage, ex);
        }

        var signedContent = signedData.SignedContent;
        if (signedContent is null)
        {
            // Firma "detached": il documento originale non è dentro il .p7m.
            throw new ApriP7MException(ErrorCode.CmsHasNoContent,
                "Questo .p7m non contiene il documento al suo interno (firma separata). Non è possibile estrarlo.",
                Stage);
        }

        byte[] content;
        try
        {
            using var ms = new MemoryStream();
            signedContent.Write(ms);
            content = ms.ToArray();
        }
        catch (Exception ex)
        {
            throw new ApriP7MException(ErrorCode.CmsContentUnreadable,
                "Il documento dentro il .p7m non è leggibile.", Stage, ex);
        }

        if (content.Length == 0)
        {
            throw new ApriP7MException(ErrorCode.CmsHasNoContent,
                "Il documento estratto è vuoto.", Stage);
        }

        var innerName = StripP7mSuffix(originalFileName);
        var kind = FileTypeDetector.Detect(content, innerName);
        return new CmsExtractionResult
        {
            Content = content,
            ContentKind = kind,
            SuggestedExtension = ExtensionFor(kind, innerName)
        };
    }

    /// <summary>
    /// Alcuni .p7m sono codificati in Base64 (PEM-like) anziché DER binario.
    /// Riconosce il caso e converte in DER; altrimenti restituisce l'input.
    /// </summary>
    private static byte[] NormalizeToDer(byte[] bytes)
    {
        // DER inizia tipicamente con 0x30 (SEQUENCE). Se il primo byte è
        // stampabile, proviamo a interpretarlo come Base64.
        if (bytes.Length > 0 && bytes[0] == 0x30)
        {
            return bytes;
        }

        try
        {
            var text = System.Text.Encoding.ASCII.GetString(bytes).Trim();
            // Rimuove eventuali header PEM.
            text = text.Replace("-----BEGIN PKCS7-----", string.Empty)
                       .Replace("-----END PKCS7-----", string.Empty)
                       .Replace("\r", string.Empty)
                       .Replace("\n", string.Empty)
                       .Trim();
            return Convert.FromBase64String(text);
        }
        catch
        {
            // Non era Base64: torna il buffer originale e lascia decidere al parser.
            return bytes;
        }
    }

    private static string StripP7mSuffix(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return fileName;
        }

        return fileName.EndsWith(".p7m", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^4]
            : fileName;
    }

    private static string ExtensionFor(FileKind kind, string innerName) => kind switch
    {
        FileKind.Pdf => "pdf",
        FileKind.InvoiceXml or FileKind.Xml => "xml",
        FileKind.Zip => "zip",
        _ => GuessExtension(innerName)
    };

    private static string GuessExtension(string innerName)
    {
        var ext = Path.GetExtension(innerName).TrimStart('.');
        return string.IsNullOrEmpty(ext) ? "bin" : ext.ToLowerInvariant();
    }
}
