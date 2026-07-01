namespace ApriP7M.Core.Diagnostics;

/// <summary>
/// Converte la dimensione di un file in una fascia generica, così che la
/// diagnostica non riveli la dimensione esatta del documento.
/// </summary>
public static class SizeBucket
{
    public static string For(long bytes) => bytes switch
    {
        < 0 => "sconosciuta",
        < 100 * 1024 => "<100KB",
        < 1024 * 1024 => "100KB-1MB",
        < 5L * 1024 * 1024 => "1-5MB",
        < 20L * 1024 * 1024 => "5-20MB",
        < 100L * 1024 * 1024 => "20-100MB",
        _ => ">100MB"
    };
}
