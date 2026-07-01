namespace ApriP7M.Core.Reviews;

/// <summary>Stato locale dei contatori usati per decidere se chiedere una recensione.</summary>
public sealed class ReviewState
{
    public DateTimeOffset FirstLaunchUtc { get; set; }
    public int SuccessfulOperations { get; set; }
    public DateTimeOffset? LastImportantErrorUtc { get; set; }
    public bool UserOptedOut { get; set; }       // "Non chiedermelo più"
    public bool AlreadyShown { get; set; }
}

/// <summary>
/// Decide se mostrare la richiesta gentile di recensione. Basata SOLO su contatori
/// locali — nessun dato esce dal PC.
///
/// Regole: mai alla prima apertura, mai dopo un errore importante recente, mai in
/// modo aggressivo, ignorabile, con opzione "Non chiedermelo più". Solo dopo uso
/// positivo: >= 3 operazioni riuscite e >= 3 giorni dalla prima apertura.
/// </summary>
public static class ReviewPromptPolicy
{
    public const int MinSuccessfulOperations = 3;
    public static readonly TimeSpan MinAgeSinceFirstLaunch = TimeSpan.FromDays(3);
    public static readonly TimeSpan RecentErrorWindow = TimeSpan.FromDays(1);

    public static bool ShouldPrompt(ReviewState state, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.UserOptedOut || state.AlreadyShown)
        {
            return false;
        }

        if (state.SuccessfulOperations < MinSuccessfulOperations)
        {
            return false;
        }

        if (nowUtc - state.FirstLaunchUtc < MinAgeSinceFirstLaunch)
        {
            return false;
        }

        if (state.LastImportantErrorUtc is { } err && nowUtc - err < RecentErrorWindow)
        {
            return false;
        }

        return true;
    }
}
