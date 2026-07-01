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
}
