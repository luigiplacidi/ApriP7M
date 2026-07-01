using ApriP7M.App.Services;
using ApriP7M.Core.Settings;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace ApriP7M.App;

/// <summary>
/// Punto d'ingresso dell'applicazione WinUI 3.
/// </summary>
public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public static SettingsService SettingsService { get; } = new();
    public static AppSettings Settings { get; private set; } = new();

    public App()
    {
        InitializeComponent();
        Settings = SettingsService.Load();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var activationFilePath = Environment.GetCommandLineArgs()
            .Skip(1)
            .FirstOrDefault(File.Exists);

        MainWindow = new MainWindow(activationFilePath);
        ApplyTheme(Settings.Theme);
        MainWindow.Activate();
    }

    public static void SaveSettings()
    {
        SettingsService.Save(Settings);
    }

    public static void ApplyTheme(string theme)
    {
        Settings.Theme = theme;

        if (MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = ToElementTheme(theme);
        }

        SyncTitleBar(theme);
    }

    private static ElementTheme ToElementTheme(string theme)
        => theme switch
        {
            "light" => ElementTheme.Light,
            "dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

    private static void SyncTitleBar(string theme)
    {
        if (MainWindow is null || !AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var isLight = theme == "light" ||
            theme == "system" && Application.Current.RequestedTheme == ApplicationTheme.Light;
        var titleBar = MainWindow.AppWindow.TitleBar;

        titleBar.ButtonForegroundColor = isLight ? Colors.Black : Colors.White;
        titleBar.ButtonHoverForegroundColor = isLight ? Colors.Black : Colors.White;
        titleBar.ButtonPressedForegroundColor = isLight ? Colors.Black : Colors.White;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = isLight ? Color.FromArgb(24, 0, 0, 0) : Color.FromArgb(32, 255, 255, 255);
        titleBar.ButtonPressedBackgroundColor = isLight ? Color.FromArgb(36, 0, 0, 0) : Color.FromArgb(48, 255, 255, 255);
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }
}
