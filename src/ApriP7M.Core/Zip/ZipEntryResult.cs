using ApriP7M.Core.Detection;

namespace ApriP7M.Core.Zip;

/// <summary>
/// Una voce estratta da un archivio ZIP.
/// </summary>
public sealed class ZipEntryResult
{
    /// <summary>Nome dell'entry (solo file, senza percorso di sistema).</summary>
    public required string EntryName { get; init; }

    public required byte[] Content { get; init; }

    public required FileKind Kind { get; init; }
}
