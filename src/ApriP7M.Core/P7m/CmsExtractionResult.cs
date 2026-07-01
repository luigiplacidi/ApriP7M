using ApriP7M.Core.Detection;

namespace ApriP7M.Core.P7m;

/// <summary>
/// Esito dell'estrazione del contenuto da un file CMS/PKCS#7 (.p7m).
/// Contiene SOLO il documento estratto, non informazioni sulla firma:
/// Apri P7M estrae, non verifica la validità legale della firma.
/// </summary>
public sealed class CmsExtractionResult
{
    public required byte[] Content { get; init; }

    /// <summary>Tipo logico del contenuto estratto.</summary>
    public required FileKind ContentKind { get; init; }

    /// <summary>
    /// Estensione suggerita per il salvataggio (es. "pdf", "xml"), senza punto.
    /// </summary>
    public required string SuggestedExtension { get; init; }
}
