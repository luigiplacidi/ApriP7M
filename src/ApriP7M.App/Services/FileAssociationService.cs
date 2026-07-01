using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ApriP7M.App.Services;

public sealed record FileAssociationOption(string Extension, string ProgId, string DisplayName);

public static class FileAssociationService
{
    public static readonly FileAssociationOption P7m = new(".p7m", "ApriP7M.p7m", "File firmato P7M");
    public static readonly FileAssociationOption Xml = new(".xml", "ApriP7M.xml", "Documento XML");
    public static readonly FileAssociationOption Zip = new(".zip", "ApriP7M.zip", "Archivio ZIP");

    public static void RemoveObsoleteAssociations()
    {
        if (IsAssociated(Zip))
        {
            Unregister(Zip);
            NotifyShellAssociationChanged();
        }
    }

    public static bool IsAssociated(FileAssociationOption option)
    {
        using var extension = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + option.Extension);
        return string.Equals(extension?.GetValue("") as string, option.ProgId, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetAssociation(FileAssociationOption option, bool enabled)
    {
        if (enabled)
        {
            Register(option);
        }
        else
        {
            Unregister(option);
        }

        NotifyShellAssociationChanged();
    }

    private static void Register(FileAssociationOption option)
    {
        var appExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(appExe) || !File.Exists(appExe))
        {
            throw new InvalidOperationException("Impossibile trovare l'eseguibile di Apri P7M.");
        }

        using (var extension = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + option.Extension))
        {
            extension?.SetValue("", option.ProgId);
        }

        using (var prog = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + option.ProgId))
        {
            prog?.SetValue("", option.DisplayName);
        }

        using (var icon = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + option.ProgId + @"\DefaultIcon"))
        {
            icon?.SetValue("", "\"" + appExe + "\",0");
        }

        using (var command = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + option.ProgId + @"\shell\open\command"))
        {
            command?.SetValue("", "\"" + appExe + "\" \"%1\"");
        }
    }

    private static void Unregister(FileAssociationOption option)
    {
        using (var extension = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + option.Extension, writable: true))
        {
            if (extension is not null &&
                string.Equals(extension.GetValue("") as string, option.ProgId, StringComparison.OrdinalIgnoreCase))
            {
                extension.DeleteValue("", throwOnMissingValue: false);
            }
        }

        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + option.ProgId, throwOnMissingSubKey: false);
    }

    private static void NotifyShellAssociationChanged()
        => SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
