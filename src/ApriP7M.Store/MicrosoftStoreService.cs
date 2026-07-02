#if WINDOWS
using Windows.Services.Store;
using Windows.System;
#endif

namespace ApriP7M.Store;

/// <summary>
/// Implementazione reale basata su <c>Windows.Services.Store.StoreContext</c>.
/// Funziona solo quando l'app gira come pacchetto installato dallo Store.
/// In sviluppo/non pacchettizzato usare <see cref="FakeStoreService"/>.
/// </summary>
public sealed class MicrosoftStoreService : IStoreService
{
    private const string ProductId = "9PN10B89Z7LR";

#if WINDOWS
    // Lazy: StoreContext.GetDefault() può lanciare se l'app non è installata
    // dal pacchetto Store (installer standalone). Non deve rompere il costruttore.
    private StoreContext? _context;
    private StoreContext Context => _context ??= StoreContext.GetDefault();
#endif

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
#if WINDOWS
        try
        {
            var updates = await Context.GetAppAndOptionalStorePackageUpdatesAsync().AsTask(ct);
            var available = updates.Count > 0;
            return new UpdateCheckResult(available, available ? "disponibile" : null);
        }
        catch (Exception)
        {
            // Fuori dal pacchetto Store il controllo non è disponibile:
            // gli aggiornamenti passano dal sito ufficiale.
            return new UpdateCheckResult(false, null);
        }
#else
        await Task.CompletedTask;
        return new UpdateCheckResult(false, null);
#endif
    }

    public async Task OpenStorePageForUpdateAsync()
    {
#if WINDOWS
        // Apre la pagina aggiornamenti dell'app nello Store. Nessun installer esterno.
        await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?ProductId={ProductId}"));
#else
        await Task.CompletedTask;
#endif
    }

    public async Task RequestReviewAsync()
    {
#if WINDOWS
        // Deep link alla finestra di recensione dello Store. Non usiamo
        // RequestRateAndReviewAppAsync: in WinUI 3 desktop richiede un hwnd
        // (InitializeWithWindow) e fuori dal pacchetto Store fallisce in
        // silenzio. Il deep link funziona sia per l'installazione Store sia
        // per l'installer standalone.
        await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://review/?ProductId={ProductId}"));
#else
        await Task.CompletedTask;
#endif
    }
}

/// <summary>Implementazione fittizia per sviluppo e test fuori dallo Store.</summary>
public sealed class FakeStoreService : IStoreService
{
    private readonly bool _updateAvailable;
    public FakeStoreService(bool updateAvailable = false) => _updateAvailable = updateAvailable;

    public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
        => Task.FromResult(new UpdateCheckResult(_updateAvailable, _updateAvailable ? "2.0.0" : null));

    public Task OpenStorePageForUpdateAsync() => Task.CompletedTask;
    public Task RequestReviewAsync() => Task.CompletedTask;
}
