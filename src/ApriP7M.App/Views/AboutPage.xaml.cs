using ApriP7M.Store;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;
using Windows.System;

namespace ApriP7M.App.Views;

public sealed partial class AboutPage : Page
{
    // In sviluppo usiamo il fake; nel pacchetto Store si userà MicrosoftStoreService.
    private readonly IStoreService _store = new MicrosoftStoreService();

    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"Versione {GetVersion()}";
    }

    private static string GetVersion()
    {
        try
        {
            var v = Package.Current.Id.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
        catch
        {
            return "1.0.3";
        }
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        try
        {
            var result = await _store.CheckForUpdatesAsync();
            UpdateBar.IsOpen = result.UpdateAvailable;
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private async void OpenStore_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _store.OpenStorePageForUpdateAsync();
        }
        catch
        {
            // Un link che non si apre non deve far cadere l'app.
        }
    }

    private async void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/luigiplacidi/ApriP7M"));
        }
        catch
        {
            // Un link che non si apre non deve far cadere l'app.
        }
    }
}
