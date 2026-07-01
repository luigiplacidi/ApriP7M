namespace ApriP7M.Core.Detection;

/// <summary>
/// Tipo logico di file riconosciuto da Apri P7M.
/// Volutamente generico: non rivela contenuto né nome del documento.
/// </summary>
public enum FileKind
{
    Unknown,

    /// <summary>Contenitore CMS/PKCS#7 firmato (.p7m) — contenuto da estrarre.</summary>
    P7m,

    /// <summary>PDF non firmato.</summary>
    Pdf,

    /// <summary>XML di fattura elettronica (FatturaPA).</summary>
    InvoiceXml,

    /// <summary>XML generico (non riconosciuto come fattura).</summary>
    Xml,

    /// <summary>Archivio ZIP che può contenere XML/P7M.</summary>
    Zip
}
