using System.Text;
using ApriP7M.App.ViewModels;
using ApriP7M.Core.Detection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ApriP7M.App.Views;

public sealed partial class DocumentPreviewPage : Page
{
    // Host virtuale usato per servire il PDF a WebView2: nella versione Store
    // (pacchettizzata) i file:/// dalla cartella temporanea non vengono caricati.
    private const string PreviewHost = "anteprima.aprip7m.local";

    private ResultItem? _item;
    private string? _previewPath;

    public DocumentPreviewPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not ResultItem item)
        {
            ClosePreview();
            return;
        }

        _item = item;
        ConfigureHeader(item);
        SavePdfButton.Visibility = CanSavePdf(item) ? Visibility.Visible : Visibility.Collapsed;
        ShowPreview(item);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        DeletePreviewFile();
    }

    private void ConfigureHeader(ResultItem item)
    {
        ResultTitle.Text = item.Kind switch
        {
            FileKind.InvoiceXml => "Fattura elettronica rilevata",
            FileKind.Pdf => "Documento PDF estratto correttamente",
            FileKind.Xml => "Documento XML aperto correttamente",
            _ => "Documento estratto correttamente"
        };

        ResultSubtitle.Text = item.Kind switch
        {
            FileKind.InvoiceXml => "Il PDF generato è una copia leggibile di cortesia. Il documento fiscale resta l'XML originale.",
            _ => $"Tipo rilevato: {item.KindLabel}. Puoi visualizzarlo o salvarlo sul PC."
        };
    }

    private async void ShowPreview(ResultItem item)
    {
        var pdfBytes = item.Document.ReadablePdf;
        if (pdfBytes is null && item.Kind == FileKind.Pdf)
        {
            pdfBytes = item.Document.OriginalContent;
        }

        if (pdfBytes is { Length: > 0 } && await TryShowPdfAsync(pdfBytes))
        {
            return;
        }

        if (item.Kind is FileKind.Xml or FileKind.InvoiceXml)
        {
            TextPreview.Text = DecodeText(item.Document.OriginalContent);
            TextPreview.Visibility = Visibility.Visible;
            return;
        }

        // Nessuna anteprima possibile (formato non visualizzabile o viewer non
        // disponibile): il documento resta estratto e salvabile sul PC.
        NoPreview.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Tenta l'anteprima PDF nel viewer integrato (WebView2). Se il runtime non
    /// è disponibile o l'inizializzazione fallisce, ritorna false e lascia
    /// mostrare il fallback "Anteprima non disponibile" (con salvataggio).
    /// </summary>
    private async Task<bool> TryShowPdfAsync(byte[] pdfBytes)
    {
        try
        {
            var folder = GetPreviewFolder();
            var fileName = $"{Guid.NewGuid():N}.pdf";
            _previewPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(_previewPath, pdfBytes);

            // Inizializza esplicitamente il motore: senza, un runtime WebView2
            // assente lascerebbe l'anteprima bianca senza spiegazione.
            await PdfPreview.EnsureCoreWebView2Async();

            // Serviamo la cartella tramite un host virtuale invece di un file:///.
            // Nella versione Store (pacchettizzata) WebView2 non carica i file
            // locali per percorso; con l'host virtuale funziona in entrambe.
            PdfPreview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                PreviewHost, folder, CoreWebView2HostResourceAccessKind.Allow);
            PdfPreview.Source = new Uri($"https://{PreviewHost}/{fileName}");
            PdfPreview.Visibility = Visibility.Visible;
            return true;
        }
        catch
        {
            PdfPreview.Visibility = Visibility.Collapsed;
            DeletePreviewFile();
            _previewPath = null;
            return false;
        }
    }

    /// <summary>
    /// Cartella scrivibile per l'anteprima, valida sia nella versione Store
    /// (pacchettizzata) sia in quella con installer (non pacchettizzata).
    /// </summary>
    private static string GetPreviewFolder()
    {
        string folder;
        try
        {
            // App pacchettizzata: cartella temporanea dedicata dell'app.
            folder = ApplicationData.Current.TemporaryFolder.Path;
        }
        catch
        {
            // App non pacchettizzata: cartella temporanea di sistema.
            folder = Path.Combine(Path.GetTempPath(), "ApriP7M");
        }

        Directory.CreateDirectory(folder);
        return folder;
    }

    private async void SavePdf_Click(object sender, RoutedEventArgs e)
    {
        if (_item is null)
        {
            return;
        }

        var bytes = _item.Document.ReadablePdf;
        if (bytes is null && _item.Kind == FileKind.Pdf)
        {
            bytes = _item.Document.OriginalContent;
        }

        if (bytes is { Length: > 0 })
        {
            await SaveBytesAsync(bytes, _item.DisplayName, "pdf", "PDF");
        }
    }

    private async void SaveOriginal_Click(object sender, RoutedEventArgs e)
    {
        if (_item is null)
        {
            return;
        }

        await SaveBytesAsync(
            _item.Document.OriginalContent,
            _item.DisplayName,
            _item.Document.OriginalExtension,
            "Documento originale");
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ClosePreview();
    }

    private void ClosePreview()
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
            return;
        }

        if (App.MainWindow is MainWindow window)
        {
            window.NavigateTo("open");
        }
    }

    private async Task SaveBytesAsync(byte[] bytes, string displayName, string extension, string label)
    {
        if (bytes.Length == 0)
        {
            return;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = Path.GetFileNameWithoutExtension(displayName)
        };
        picker.FileTypeChoices.Add(label, new List<string> { NormalizeExtension(extension) });

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile? file = await picker.PickSaveFileAsync();
        if (file is not null)
        {
            await FileIO.WriteBytesAsync(file, bytes);
        }
    }

    private void DeletePreviewFile()
    {
        if (_previewPath is null)
        {
            return;
        }

        try
        {
            File.Delete(_previewPath);
        }
        catch
        {
            // Best effort: il viewer PDF potrebbe tenere ancora il file aperto.
        }
    }

    private static bool CanSavePdf(ResultItem item)
        => item.HasReadablePdf || item.Kind == FileKind.Pdf;

    private static string DecodeText(byte[] bytes)
    {
        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "Il testo non può essere visualizzato, ma il file può essere salvato.";
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".bin";
        }

        return extension.StartsWith('.') ? extension : $".{extension}";
    }
}
