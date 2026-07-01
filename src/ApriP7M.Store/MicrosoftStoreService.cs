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
    private readonly StoreContext _context = StoreContext.GetDefault();
#endif

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
#if WINDOWS
        var updates = await _context.GetAppAndOptionalStorePackageUpdatesAsync().AsTask(ct);
        var available = updates.Count > 0;
        return new UpdateCheckResult(available, available ? "disponibile" : null);
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
        await _context.RequestRateAndReviewAppAsync();
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
