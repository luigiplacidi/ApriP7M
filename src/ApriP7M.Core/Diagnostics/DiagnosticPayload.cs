using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApriP7M.Core.Diagnostics;

/// <summary>
/// Payload della diagnostica anonima. Contiene SOLO metadati tecnici minimizzati.
///
/// Schema chiuso: nessun campo libero, così è impossibile farci finire per errore
/// il contenuto di un documento. Vedi PRIVACY.md per l'elenco dei campi ammessi.
///
/// Vietato per costruzione: contenuto file, nomi file completi, percorsi locali,
/// dati fiscali/personali, identificativi persistenti.
/// </summary>
public sealed class DiagnosticPayload
{
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; init; } = "";

    [JsonPropertyName("windowsVersion")]
    public string WindowsVersion { get; init; } = "";

    /// <summary>Tipo generico: P7M, PDF, XML_FATTURA, XML, ZIP, SCONOSCIUTO.</summary>
    [JsonPropertyName("fileKind")]
    public string FileKind { get; init; } = "";

    /// <summary>Fascia di dimensione, es. "1-5MB".</summary>
    [JsonPropertyName("fileSizeBucket")]
    public string FileSizeBucket { get; init; } = "";

    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; init; }

    [JsonPropertyName("errorStage")]
    public string ErrorStage { get; init; } = "";

    [JsonPropertyName("module")]
    public string Module { get; init; } = "";

    /// <summary>Timestamp arrotondato all'ora (UTC), niente precisione al secondo.</summary>
    [JsonPropertyName("timestampHour")]
    public string TimestampHour { get; init; } = "";

    [JsonPropertyName("appLanguage")]
    public string AppLanguage { get; init; } = "";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>
    /// Rappresentazione mostrata all'utente PRIMA dell'invio ("cosa verrà inviato").
    /// È esattamente ciò che verrebbe trasmesso.
    /// </summary>
    public string ToPreviewJson() => JsonSerializer.Serialize(this, JsonOptions);
}
