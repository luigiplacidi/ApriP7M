namespace ApriP7M.Core.Settings;

/// <summary>
/// Preferenze locali dell'app. Persistite in locale e mai inviate online.
/// </summary>
public sealed class AppSettings
{
    // Aggiornamenti (gestiti dal Microsoft Store)
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool ShowUpdateNotifications { get; set; } = true;
    public string? DismissedUpdateVersion { get; set; }

    // Diagnostica anonima — OPT-IN, disattivata di default
    public bool DiagnosticsEnabled { get; set; } = false;

    // Tema: "system" | "light" | "dark"
    public string Theme { get; set; } = "system";

    // Sostegno al progetto: quante volte l'utente ha aperto un documento con
    // successo (per proporre il caffè al momento giusto, non a ogni apertura)
    // e se ha scelto di non vedere più l'invito.
    public int SuccessfulOpenCount { get; set; }
    public bool SupportPromptOptOut { get; set; }
}
