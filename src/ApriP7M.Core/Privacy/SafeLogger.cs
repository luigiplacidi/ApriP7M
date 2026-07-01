namespace ApriP7M.Core.Privacy;

public enum LogLevel { Debug, Info, Warning, Error }

/// <summary>
/// Logger minimizzato e rispettoso della privacy.
///
/// Regole non negoziabili:
/// - mai contenuto di documenti;
/// - mai nomi file completi né percorsi locali;
/// - solo informazioni tecniche (modulo, fase, codice errore).
///
/// I metodi accettano testo già sanificato dal chiamante. Per sicurezza,
/// <see cref="Redact"/> rimuove percorsi e riduce i nomi file alla sola estensione.
/// </summary>
public sealed class SafeLogger
{
    private readonly Action<LogLevel, string> _sink;

    public SafeLogger(Action<LogLevel, string>? sink = null)
        => _sink = sink ?? ((_, _) => { });

    public void Info(string module, string message) => Write(LogLevel.Info, module, message);
    public void Warning(string module, string message) => Write(LogLevel.Warning, module, message);

    public void Error(string module, string stage, ErrorCode code, string message)
        => Write(LogLevel.Error, module, $"stage={stage} code={(int)code}:{code} {Redact(message)}");

    public void Exception(string module, ApriP7MException ex)
        => Error(module, ex.Stage, ex.Code, ex.Message);

    private void Write(LogLevel level, string module, string message)
        => _sink(level, $"[{module}] {Redact(message)}");

    /// <summary>
    /// Rimuove percorsi locali e riduce i nomi file alla sola estensione, così
    /// che nel log non finiscano dati identificativi del documento.
    /// </summary>
    public static string Redact(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Percorsi Windows (C:\...\file) e Unix (/home/...).
        var result = System.Text.RegularExpressions.Regex.Replace(
            input,
            @"([A-Za-z]:\\[^\s""]+|/[^\s""]+/[^\s""]+)",
            m => $"<percorso:{ExtOf(m.Value)}>");

        // Nomi file isolati con estensione (documento.pdf -> <file:pdf>).
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"\b[\w\-. ]+\.(pdf|xml|p7m|zip|p7s|txt|jpg|png)\b",
            m => $"<file:{m.Groups[1].Value}>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return result;
    }

    private static string ExtOf(string pathOrName)
    {
        var ext = Path.GetExtension(pathOrName).TrimStart('.');
        return string.IsNullOrEmpty(ext) ? "?" : ext.ToLowerInvariant();
    }
}
