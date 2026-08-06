using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApriP7M.Store;

namespace ApriP7M.App.Services;

/// <summary>Esito del controllo aggiornamenti pronto per la UI.</summary>
public sealed record AppUpdateInfo(bool Available, string? Version, IReadOnlyList<string> Notes);

/// <summary>
/// Controlla se c'è un aggiornamento sul Microsoft Store e, quando disponibile,
/// recupera le novità (changelog) da un piccolo file statico sul sito ufficiale.
/// Nessun dato personale viene inviato: solo una richiesta di lettura al nostro
/// dominio, e solo se un aggiornamento esiste davvero.
/// </summary>
public static class UpdateService
{
    private const string NotesUrl = "https://aprip7m.it/app/novita.json";
    private static readonly IStoreService Store = new MicrosoftStoreService();

    public static async Task<AppUpdateInfo> CheckAsync()
    {
        try
        {
            var result = await Store.CheckForUpdatesAsync();
            if (!result.UpdateAvailable)
            {
                return new AppUpdateInfo(false, null, Array.Empty<string>());
            }

            var (version, notes) = await FetchNotesAsync();
            return new AppUpdateInfo(true, version, notes);
        }
        catch
        {
            return new AppUpdateInfo(false, null, Array.Empty<string>());
        }
    }

    public static Task OpenStoreAsync() => Store.OpenStorePageForUpdateAsync();

    private static async Task<(string? Version, IReadOnlyList<string> Notes)> FetchNotesAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await http.GetStringAsync(NotesUrl);
            var dto = JsonSerializer.Deserialize<NotesDto>(json);
            return (dto?.Version, dto?.Notes ?? Array.Empty<string>());
        }
        catch
        {
            // Offline o file non raggiungibile: mostreremo un messaggio generico.
            return (null, Array.Empty<string>());
        }
    }

    private sealed class NotesDto
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("notes")]
        public string[]? Notes { get; set; }
    }
}
