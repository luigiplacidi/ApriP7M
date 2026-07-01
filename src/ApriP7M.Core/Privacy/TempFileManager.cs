namespace ApriP7M.Core.Privacy;

/// <summary>
/// Gestisce i file temporanei creati durante l'elaborazione, garantendone la
/// pulizia. I file restano sul PC dell'utente, in una cartella dedicata, e
/// vengono rimossi alla Dispose o esplicitamente.
/// </summary>
public sealed class TempFileManager : IDisposable
{
    private readonly string _root;
    private readonly List<string> _files = new();
    private bool _disposed;

    public TempFileManager(string? baseDirectory = null)
    {
        var root = baseDirectory
            ?? Path.Combine(Path.GetTempPath(), "ApriP7M", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _root = root;
    }

    public string Root => _root;

    /// <summary>Crea un percorso temporaneo (non scrive nulla).</summary>
    public string NewTempPath(string extension)
    {
        var ext = extension.TrimStart('.');
        var path = Path.Combine(_root, $"{Guid.NewGuid():N}.{ext}");
        _files.Add(path);
        return path;
    }

    /// <summary>Scrive un buffer su un file temporaneo e ne restituisce il percorso.</summary>
    public string WriteTemp(byte[] content, string extension)
    {
        var path = NewTempPath(extension);
        File.WriteAllBytes(path, content);
        return path;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var file in _files)
        {
            TryDelete(file);
        }

        try
        {
            if (Directory.Exists(_root) && Directory.GetFileSystemEntries(_root).Length == 0)
            {
                Directory.Delete(_root);
            }
        }
        catch
        {
            // Pulizia best-effort: non deve mai far fallire l'app.
        }
    }

    private static void TryDelete(string file)
    {
        try
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
