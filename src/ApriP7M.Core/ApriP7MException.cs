namespace ApriP7M.Core;

/// <summary>
/// Codice di errore stabile, adatto sia a messaggi utente chiari sia alla
/// diagnostica anonima (non rivela contenuto del documento).
/// </summary>
public enum ErrorCode
{
    None = 0,

    // Input
    FileNotFound = 100,
    EmptyFile = 101,
    UnsupportedFormat = 102,

    // P7M / CMS
    NotValidCms = 200,
    CmsHasNoContent = 201,
    CmsContentUnreadable = 202,

    // Fattura XML
    InvoiceParseFailed = 300,
    InvoiceUnsupportedVersion = 301,

    // ZIP
    ZipCorrupted = 400,
    ZipPathTraversal = 401,
    ZipEmpty = 402,

    // PDF render
    PdfRenderFailed = 500,

    // Generico
    Unexpected = 900
}

/// <summary>
/// Eccezione di dominio con messaggio utente (italiano, chiaro) e codice stabile.
/// Il messaggio NON deve mai contenere il contenuto del documento.
/// </summary>
public sealed class ApriP7MException : Exception
{
    public ErrorCode Code { get; }

    /// <summary>Modulo/fase coinvolta, utile per la diagnostica anonima.</summary>
    public string Stage { get; }

    public ApriP7MException(ErrorCode code, string userMessage, string stage, Exception? inner = null)
        : base(userMessage, inner)
    {
        Code = code;
        Stage = stage;
    }
}
