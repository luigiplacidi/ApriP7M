using System.Collections.ObjectModel;
using System.Globalization;
using ApriP7M.Core;
using ApriP7M.Core.Diagnostics;
using ApriP7M.Core.Detection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApriP7M.App.ViewModels;

/// <summary>Una riga di risultato mostrata nella UI.</summary>
public sealed partial class ResultItem : ObservableObject
{
    public required string DisplayName { get; init; }
    public required FileKind Kind { get; init; }
    public required OpenedDocument Document { get; init; }

    public string KindLabel => Kind switch
    {
        FileKind.Pdf => "PDF",
        FileKind.InvoiceXml => "Fattura XML",
        FileKind.Xml => "XML",
        FileKind.Zip => "Archivio",
        FileKind.P7m => "P7M",
        _ => "Documento"
    };

    public bool HasReadablePdf => Document.ReadablePdf is { Length: > 0 };
    public bool HasAttachments => Document.Attachments.Count > 0;
}

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly DocumentService _service = new();

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool HasResults { get; set; }

    [ObservableProperty]
    public partial string? DiagnosticPreview { get; set; }

    public ObservableCollection<ResultItem> Results { get; } = new();
    public ObservableCollection<ResultItem> RecentResults { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasRecentResults => RecentResults.Count > 0;
    public bool HasDiagnosticPreview => !string.IsNullOrWhiteSpace(DiagnosticPreview);

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnDiagnosticPreviewChanged(string? value)
    {
        OnPropertyChanged(nameof(HasDiagnosticPreview));
    }

    /// <summary>
    /// Apre un file dal percorso indicato. L'elaborazione gira fuori dal thread UI.
    /// Tutto in locale: nessun upload.
    /// </summary>
    public async Task OpenFileAsync(string filePath)
    {
        ErrorMessage = null;
        DiagnosticPreview = null;
        Results.Clear();
        HasResults = false;
        IsBusy = true;
        try
        {
            var docs = await Task.Run(() => _service.Open(filePath));

            foreach (var doc in docs)
            {
                var item = new ResultItem
                {
                    DisplayName = doc.DisplayName,
                    Kind = doc.Kind,
                    Document = doc
                };

                Results.Add(item);
                AddRecent(item);
            }
            HasResults = Results.Count > 0;
        }
        catch (ApriP7MException ex)
        {
            // Messaggio utente chiaro; nessun contenuto del documento.
            ErrorMessage = ex.Message;
            DiagnosticPreview = BuildDiagnosticPreview(filePath, ex);
        }
        catch
        {
            ErrorMessage = "Si è verificato un errore imprevisto durante l'apertura del file.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string? BuildDiagnosticPreview(string filePath, ApriP7MException error)
    {
        try
        {
            var kind = FileTypeDetector.Detect(filePath);
            var size = File.Exists(filePath) ? new FileInfo(filePath).Length : -1;
            var builder = new DiagnosticBuilder(
                "1.0.1",
                Environment.OSVersion.VersionString,
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            return builder.FromError(error, kind, size).ToPreviewJson();
        }
        catch
        {
            return null;
        }
    }

    private void AddRecent(ResultItem item)
    {
        RecentResults.Insert(0, item);
        while (RecentResults.Count > 8)
        {
            RecentResults.RemoveAt(RecentResults.Count - 1);
        }

        OnPropertyChanged(nameof(HasRecentResults));
    }

    public void RemoveRecent(ResultItem item)
    {
        if (RecentResults.Remove(item))
        {
            OnPropertyChanged(nameof(HasRecentResults));
        }
    }

    public void ClearRecent()
    {
        if (RecentResults.Count == 0)
        {
            return;
        }

        RecentResults.Clear();
        OnPropertyChanged(nameof(HasRecentResults));
    }
}
