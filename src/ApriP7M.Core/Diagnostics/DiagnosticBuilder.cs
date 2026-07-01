using ApriP7M.Core.Detection;

namespace ApriP7M.Core.Diagnostics;

/// <summary>
/// Costruisce un <see cref="DiagnosticPayload"/> da un errore, applicando le regole
/// di minimizzazione (tipo generico, fascia dimensione, timestamp arrotondato).
/// Non riceve mai contenuto né nome reale del documento.
/// </summary>
public sealed class DiagnosticBuilder
{
    private readonly string _appVersion;
    private readonly string _windowsVersion;
    private readonly string _appLanguage;

    public DiagnosticBuilder(string appVersion, string windowsVersion, string appLanguage)
    {
        _appVersion = appVersion;
        _windowsVersion = windowsVersion;
        _appLanguage = appLanguage;
    }

    public DiagnosticPayload FromError(
        ApriP7MException error,
        FileKind fileKind,
        long fileSizeBytes,
        DateTimeOffset? nowUtc = null)
    {
        var now = (nowUtc ?? DateTimeOffset.UtcNow);
        // Arrotonda all'ora: niente precisione utile a correlare un singolo utente.
        var rounded = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);

        return new DiagnosticPayload
        {
            AppVersion = _appVersion,
            WindowsVersion = _windowsVersion,
            FileKind = FileTypeDetector.ToGenericLabel(fileKind),
            FileSizeBucket = SizeBucket.For(fileSizeBytes),
            ErrorCode = (int)error.Code,
            ErrorStage = error.Stage,
            Module = ModuleFromStage(error.Stage),
            TimestampHour = rounded.ToString("yyyy-MM-ddTHH:00:00Z"),
            AppLanguage = _appLanguage
        };
    }

    private static string ModuleFromStage(string stage)
    {
        var dot = stage.IndexOf('.');
        return dot > 0 ? stage[..dot] : stage;
    }
}
