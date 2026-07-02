namespace ApriP7M.Store;

/// <summary>
/// Esito di un controllo aggiornamenti sul Microsoft Store.
/// <see cref="CheckSucceeded"/> è falso quando il controllo non è possibile,
/// ad esempio con l'installazione standalone fuori dallo Store.
/// </summary>
public sealed record UpdateCheckResult(bool UpdateAvailable, string? LatestVersion, bool CheckSucceeded = true);

/// <summary>
/// Astrae l'integrazione con il Microsoft Store, così da poterla simulare in
/// sviluppo (dove StoreContext non è disponibile fuori da un pacchetto firmato).
///
/// Nessuna di queste operazioni invia documenti, nomi file o dati personali.
/// </summary>
public interface IStoreService
{
    /// <summary>Controllo manuale aggiornamenti (sempre disponibile).</summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default);

    /// <summary>Apre la pagina dell'app sul Microsoft Store per aggiornare.</summary>
    Task OpenStorePageForUpdateAsync();

    /// <summary>Mostra la richiesta di recensione nativa del Microsoft Store.</summary>
    Task RequestReviewAsync();
}
