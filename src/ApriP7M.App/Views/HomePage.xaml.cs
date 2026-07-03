using ApriP7M.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ApriP7M.App.Views;

public sealed partial class HomePage : Page
{
    private enum PageMode
    {
        Home,
        Open,
        Convert,
        History
    }

    private static readonly HomeViewModel SharedViewModel = new();
    private PageMode _mode = PageMode.Home;

    public HomeViewModel ViewModel { get; } = SharedViewModel;

    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is HomeNavigationRequest request)
        {
            SetMode(request.Mode);
            if (!string.IsNullOrWhiteSpace(request.FilePath))
            {
                await OpenFileAndShowAsync(request.FilePath);
            }

            return;
        }

        SetMode(e.Parameter as string);
    }

    private async void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        if (_mode == PageMode.Convert)
        {
            picker.FileTypeFilter.Add(".xml");
            picker.FileTypeFilter.Add(".p7m");
        }
        else
        {
            picker.FileTypeFilter.Add(".p7m");
            picker.FileTypeFilter.Add(".xml");
            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add(".zip");
        }

        // WinUI 3: il picker va agganciato alla finestra corrente.
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await OpenFileAndShowAsync(file.Path);
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Apri con Apri P7M";
        }
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        if (items.Count > 0 && items[0] is StorageFile file)
        {
            await OpenFileAndShowAsync(file.Path);
        }
    }

    private async Task OpenFileAndShowAsync(string filePath)
    {
        await ViewModel.OpenFileAsync(filePath);
        UpdateSectionVisibility();
        ShowFirstResult();
    }

    private async void PreviewResult_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ResultItem item })
        {
            ShowResult(item);
        }
    }

    private void RemoveHistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ResultItem item })
        {
            ViewModel.RemoveRecent(item);
            UpdateHistoryVisibility();
        }
    }

    private async void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Pulire la cronologia?",
            Content = "Verrà svuotato solo questo elenco. I tuoi documenti sul PC non vengono toccati.",
            PrimaryButtonText = "Pulisci",
            CloseButtonText = "Annulla",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.ClearRecent();
            UpdateHistoryVisibility();
        }
    }

    private void ShowFirstResult()
    {
        if (ViewModel.Results.Count > 0)
        {
            ShowResult(ViewModel.Results[0]);
        }
    }

    private void ShowResult(ResultItem item)
    {
        Frame.Navigate(typeof(DocumentPreviewPage), item);
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        NavigateShell("settings");
    }

    private void OpenPrivacy_Click(object sender, RoutedEventArgs e)
    {
        NavigateShell("privacy");
    }

    private void OpenSection_Click(object sender, RoutedEventArgs e)
    {
        NavigateShell("open");
    }

    private void ConvertSection_Click(object sender, RoutedEventArgs e)
    {
        NavigateShell("convert");
    }

    private void HistorySection_Click(object sender, RoutedEventArgs e)
    {
        NavigateShell("history");
    }

    private async void ShowDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.DiagnosticPreview))
        {
            return;
        }

        if (!App.Settings.DiagnosticsEnabled)
        {
            var disabledDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Diagnostica anonima disattivata",
                Content = "La diagnostica è facoltativa ed è spenta finché non la attivi tu da Impostazioni. Non vengono mai condivisi documenti, contenuti, nomi di file o cartelle.",
                PrimaryButtonText = "Apri Impostazioni",
                CloseButtonText = "Chiudi"
            };

            var result = await disabledDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                NavigateShell("settings");
            }

            return;
        }

        var previewBox = new TextBox
        {
            Text = ViewModel.DiagnosticPreview,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono, Consolas"),
            MinWidth = 560,
            MinHeight = 280
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Diagnostica anonima",
            Content = previewBox,
            PrimaryButtonText = "Copia diagnostica",
            CloseButtonText = "Chiudi"
        };

        var dialogResult = await dialog.ShowAsync();
        if (dialogResult == ContentDialogResult.Primary)
        {
            var package = new DataPackage();
            package.SetText(ViewModel.DiagnosticPreview);
            Clipboard.SetContent(package);
        }
    }

    private static void NavigateShell(string tag)
    {
        if (App.MainWindow is MainWindow window)
        {
            window.NavigateTo(tag);
        }
    }

    private void SetMode(string? tag)
    {
        _mode = tag switch
        {
            "open" => PageMode.Open,
            "convert" => PageMode.Convert,
            "history" => PageMode.History,
            _ => PageMode.Home
        };

        UpdateSectionVisibility();

        HeaderTitle.Text = _mode switch
        {
            PageMode.Open => "Apri file",
            PageMode.Convert => "Converti XML",
            PageMode.History => "Cronologia",
            _ => "Benvenuto in Apri P7M"
        };

        HeaderSubtitle.Text = _mode switch
        {
            PageMode.Open => "Apri la busta firmata e leggi il documento contenuto dentro, senza caricarlo online.",
            PageMode.Convert => "Leggi una fattura elettronica XML in un PDF di cortesia, comodo da controllare e stampare.",
            PageMode.History => "Qui ritrovi i documenti aperti da quando hai avviato l'app. Alla chiusura la cronologia si svuota da sola: niente resta salvato su disco.",
            _ => "Una piccola app gratuita per Windows per leggere P7M, XML e fatture elettroniche direttamente dal tuo PC."
        };

        PageScrollViewer.ChangeView(null, 0, null, disableAnimation: true);
    }

    private void UpdateSectionVisibility()
    {
        DashboardSection.Visibility = _mode == PageMode.Home ? Visibility.Visible : Visibility.Collapsed;
        HomeSupportCard.Visibility = _mode == PageMode.Home ? Visibility.Visible : Visibility.Collapsed;
        OpenSection.Visibility = _mode == PageMode.Open ? Visibility.Visible : Visibility.Collapsed;
        ConvertSection.Visibility = _mode == PageMode.Convert ? Visibility.Visible : Visibility.Collapsed;
        HistorySection.Visibility = _mode == PageMode.History ? Visibility.Visible : Visibility.Collapsed;
        ResultsSection.Visibility = _mode is PageMode.Open or PageMode.Convert && ViewModel.HasResults
            ? Visibility.Visible
            : Visibility.Collapsed;
        UpdateHistoryVisibility();
    }

    private void UpdateHistoryVisibility()
    {
        EmptyHistoryNotice.Visibility = ViewModel.HasRecentResults ? Visibility.Collapsed : Visibility.Visible;
        HistoryList.Visibility = ViewModel.HasRecentResults ? Visibility.Visible : Visibility.Collapsed;
    }
}
