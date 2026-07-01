using System.Text;
using ApriP7M.App.ViewModels;
using ApriP7M.Core.Detection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ApriP7M.App.Views;

public sealed partial class DocumentPreviewPage : Page
{
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

    private void ShowPreview(ResultItem item)
    {
        var pdfBytes = item.Document.ReadablePdf;
        if (pdfBytes is null && item.Kind == FileKind.Pdf)
        {
            pdfBytes = item.Document.OriginalContent;
        }

        if (pdfBytes is { Length: > 0 })
        {
            _previewPath = Path.Combine(Path.GetTempPath(), "ApriP7M", $"{Guid.NewGuid():N}.pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(_previewPath)!);
            File.WriteAllBytes(_previewPath, pdfBytes);
            PdfPreview.Source = new Uri(_previewPath);
            PdfPreview.Visibility = Visibility.Visible;
            return;
        }

        if (item.Kind is FileKind.Xml or FileKind.InvoiceXml)
        {
            TextPreview.Text = DecodeText(item.Document.OriginalContent);
            TextPreview.Visibility = Visibility.Visible;
            return;
        }

        NoPreview.Visibility = Visibility.Visible;
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
