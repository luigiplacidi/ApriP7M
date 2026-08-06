using ApriP7M.App.Services;
using ApriP7M.App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ApriP7M.App;

public sealed partial class MainWindow : Window
{
    private bool _updatingSelection;
    private string? _pendingUpdateVersion;

    public MainWindow(string? activationFilePath = null)
    {
        InitializeComponent();

        // Barra del titolo estesa nell'area client per il look Windows 11.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new MicaBackdrop();

        if (string.IsNullOrWhiteSpace(activationFilePath))
        {
            NavigateTo("home");
        }
        else
        {
            NavigateToFile(activationFilePath);
        }

        _ = CheckForUpdatesOnStartupAsync();
    }

    /// <summary>
    /// All'avvio, se esiste un aggiornamento sullo Store, lo comunica con le
    /// novità e un pulsante per aggiornare, più un pallino sull'icona Aiuto.
    /// </summary>
    private async Task CheckForUpdatesOnStartupAsync()
    {
        if (!App.Settings.CheckUpdatesOnStartup || !App.Settings.ShowUpdateNotifications)
        {
            return;
        }

        var info = await UpdateService.CheckAsync();
        if (!info.Available)
        {
            return;
        }

        // Non ripetere l'avviso per una versione che l'utente ha già chiuso.
        if (info.Version is not null && info.Version == App.Settings.DismissedUpdateVersion)
        {
            return;
        }

        _pendingUpdateVersion = info.Version;
        UpdateBar.Message = info.Notes.Count > 0
            ? "Novità di questa versione:\n• " + string.Join("\n• ", info.Notes)
            : "Sono disponibili miglioramenti e correzioni. Ti consigliamo di aggiornare.";
        UpdateBar.IsOpen = true;
        UpdateBadge.Visibility = Visibility.Visible;
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await UpdateService.OpenStoreAsync();
        }
        catch
        {
            // L'apertura dello Store non deve mai far cadere l'app.
        }
    }

    private void UpdateBar_CloseButtonClick(InfoBar sender, object args)
    {
        // Ricorda la versione chiusa: niente insistenza a ogni avvio.
        App.Settings.DismissedUpdateVersion = _pendingUpdateVersion;
        App.SaveSettings();
        UpdateBadge.Visibility = Visibility.Collapsed;
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_updatingSelection)
        {
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            NavigateTo(item.Tag as string ?? "home", updateSelection: false);
        }
    }

    public void NavigateTo(string tag, bool updateSelection = true)
    {
        if (updateSelection)
        {
            SelectNavigationItem(tag);
        }

        switch (tag)
        {
            case "home":
            case "open":
            case "history":
            case "convert":
                ContentFrame.Navigate(typeof(HomePage), new HomeNavigationRequest(tag));
                break;
            case "privacy":
                ContentFrame.Navigate(typeof(PrivacyPage));
                break;
            case "settings":
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
            case "about":
                ContentFrame.Navigate(typeof(AboutPage));
                break;
            default:
                ContentFrame.Navigate(typeof(HomePage), "home");
                break;
        }
    }

    public void NavigateToFile(string filePath)
    {
        SelectNavigationItem("open");
        ContentFrame.Navigate(typeof(HomePage), new HomeNavigationRequest("open", filePath));
    }

    private void SelectNavigationItem(string tag)
    {
        foreach (var item in Nav.MenuItems.Concat(Nav.FooterMenuItems).OfType<NavigationViewItem>())
        {
            if (item.Tag as string == tag)
            {
                _updatingSelection = true;
                Nav.SelectedItem = item;
                _updatingSelection = false;
                return;
            }
        }
    }
}
