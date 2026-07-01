using ApriP7M.App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ApriP7M.App;

public sealed partial class MainWindow : Window
{
    private bool _updatingSelection;

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
