using ApriP7M.Core.Privacy;

namespace ApriP7M.App.Services;

/// <summary>
/// Registra gli errori non gestiti in un log locale, minimizzato e rispettoso
/// della privacy: solo tipo di eccezione e prima riga di stack, senza contenuti
/// dei documenti, nomi file completi o percorsi. Serve a rendere diagnosticabili
/// i crash che sullo Store compaiono come "unknown".
/// </summary>
public static class CrashLogService
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Apri P7M",
        "crash.log");

    public static void Record(string source, Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

            var type = ex?.GetType().Name ?? "Unknown";
            var message = SafeLogger.Redact(ex?.Message ?? "");
            var frame = ex?.StackTrace?
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim();

            var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm}Z [{source}] {type}: {message}";
            if (!string.IsNullOrEmpty(frame))
            {
                line += $" | {SafeLogger.Redact(frame)}";
            }

            File.AppendAllText(LogPath, line + Environment.NewLine);
            TrimLog();
        }
        catch
        {
            // Il logging non deve mai far cadere l'app.
        }
    }

    private static void TrimLog()
    {
        try
        {
            var lines = File.ReadAllLines(LogPath);
            if (lines.Length > 200)
            {
                File.WriteAllLines(LogPath, lines[^200..]);
            }
        }
        catch
        {
            // best effort
        }
    }
}
