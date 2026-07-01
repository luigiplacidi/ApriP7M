using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

const string PayloadResourceName = "ApriP7M-payload.zip";
const string AppExeName = "ApriP7M.App.exe";
const string AppDisplayName = "Apri P7M";
const string AppPublisher = "Luigi Placidi";
const string AppVersion = "1.0.2";
const string UninstallerExeName = "ApriP7M.Uninstall.exe";
const string UninstallerDirectoryName = "Apri P7M Uninstaller";
const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\LuigiPlacidi.ApriP7M";

ApplicationConfiguration.Initialize();

try
{
    if (HasArg(args, "--uninstall") || HasArg(args, "/uninstall"))
    {
        var quiet = HasArg(args, "--quiet") || HasArg(args, "/quiet");
        if (quiet)
        {
            Uninstall(quiet: true);
            return;
        }

        using var uninstallWizard = new UninstallWizard(() => Uninstall(quiet: true));
        uninstallWizard.ShowDialog();
        return;
    }

    var options = InstallOptions.CreateDefault();
    if (HasArg(args, "--quiet") || HasArg(args, "/quiet"))
    {
        options.Quiet = true;
        options.LaunchAfterInstall = false;
        Install(options);
        return;
    }

    using var wizard = new InstallerWizard(options);
    if (wizard.ShowDialog() == DialogResult.OK)
    {
        Install(wizard.Options);
    }
}
catch (Exception ex)
{
    ShowMessage("Installazione non riuscita", ex.Message, MessageBoxIcon.Error);
}

static bool HasArg(string[] args, string value)
    => args.Any(arg => string.Equals(arg, value, StringComparison.OrdinalIgnoreCase));

static void Install(InstallOptions options)
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var programsRoot = Path.Combine(localAppData, "Programs");
    var defaultInstallRoot = Path.Combine(programsRoot, AppDisplayName);
    var installRoot = string.IsNullOrWhiteSpace(options.InstallRoot)
        ? defaultInstallRoot
        : Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.InstallRoot));
    var legacyInstallRoot = Path.Combine(programsRoot, "ApriP7M");
    var tempRoot = Path.Combine(Path.GetTempPath(), "ApriP7M-install-" + Guid.NewGuid());
    var payloadPath = Path.Combine(tempRoot, PayloadResourceName);

    AssertSafeInstallPath(installRoot);
    StopAppProcesses(installRoot, legacyInstallRoot);

    Directory.CreateDirectory(tempRoot);
    try
    {
        ExtractPayload(payloadPath);
        ZipFile.ExtractToDirectory(payloadPath, tempRoot, overwriteFiles: true);

        if (Directory.Exists(installRoot))
        {
            DeleteDirectoryWithRetry(installRoot);
        }

        if (!string.Equals(legacyInstallRoot, installRoot, StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(legacyInstallRoot))
        {
            DeleteDirectoryWithRetry(legacyInstallRoot);
        }

        Directory.CreateDirectory(installRoot);
        foreach (var entry in Directory.EnumerateFileSystemEntries(tempRoot))
        {
            if (string.Equals(entry, payloadPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destination = Path.Combine(installRoot, Path.GetFileName(entry));
            if (Directory.Exists(entry))
            {
                CopyDirectory(entry, destination);
            }
            else
            {
                File.Copy(entry, destination, overwrite: true);
            }
        }

        var appExe = Path.Combine(installRoot, AppExeName);
        if (!File.Exists(appExe))
        {
            throw new FileNotFoundException("Eseguibile Apri P7M non trovato dopo l'installazione.", appExe);
        }

        CreateShortcuts(appExe, options.CreateDesktopShortcut);
        RegisterFileAssociations(appExe, options.FileAssociations);
        var uninstallerPath = InstallUninstaller(programsRoot);
        RegisterUninstallEntry(installRoot, uninstallerPath);

        if (options.LaunchAfterInstall)
        {
            Process.Start(new ProcessStartInfo(appExe)
            {
                UseShellExecute = true,
                WorkingDirectory = installRoot
            });
        }

        if (!options.Quiet)
        {
            ShowMessage(AppDisplayName, "Installazione completata.", MessageBoxIcon.Information);
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            DeleteDirectoryWithRetry(tempRoot);
        }
    }
}

static void Uninstall(bool quiet)
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var programsRoot = Path.Combine(localAppData, "Programs");
    var installRoot = Path.Combine(programsRoot, AppDisplayName);
    var legacyInstallRoot = Path.Combine(programsRoot, "ApriP7M");
    var uninstallerRoot = Path.Combine(programsRoot, UninstallerDirectoryName);

    StopAppProcesses(installRoot, legacyInstallRoot);
    DeleteShortcuts();
    UnregisterFileAssociations(AssociationDefinition.All);
    Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, throwOnMissingSubKey: false);

    if (Directory.Exists(legacyInstallRoot) &&
        !string.Equals(legacyInstallRoot, installRoot, StringComparison.OrdinalIgnoreCase))
    {
        DeleteDirectoryWithRetry(legacyInstallRoot);
    }

    if (Directory.Exists(installRoot))
    {
        DeleteDirectoryWithRetry(installRoot);
    }

    var currentExe = Environment.ProcessPath;
    if (!string.IsNullOrWhiteSpace(currentExe) &&
        IsChildPath(uninstallerRoot, currentExe) &&
        Directory.Exists(uninstallerRoot))
    {
        ScheduleDirectoryRemoval(uninstallerRoot);
    }

    if (!quiet)
    {
        ShowMessage(AppDisplayName, "Disinstallazione completata.", MessageBoxIcon.Information);
    }
}

static void ExtractPayload(string destinationPath)
{
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream(PayloadResourceName)
        ?? throw new InvalidOperationException("Payload dell'app non incorporato nell'installer.");
    using var file = File.Create(destinationPath);
    stream.CopyTo(file);
}

static void CopyDirectory(string sourceDir, string destinationDir)
{
    Directory.CreateDirectory(destinationDir);

    foreach (var file in Directory.EnumerateFiles(sourceDir))
    {
        File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), overwrite: true);
    }

    foreach (var directory in Directory.EnumerateDirectories(sourceDir))
    {
        CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
    }
}

static void StopAppProcesses(params string[] installRoots)
{
    var roots = installRoots
        .Where(root => !string.IsNullOrWhiteSpace(root))
        .Select(root => Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar)
        .ToArray();

    foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(AppExeName)))
    {
        try
        {
            var processPath = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(processPath))
            {
                continue;
            }

            var processFullPath = Path.GetFullPath(processPath);
            if (!roots.Any(root => processFullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            process.CloseMainWindow();
            if (!process.WaitForExit(3000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch
        {
            // Se il processo resta bloccato, il retry sulla cartella produrra un errore chiaro.
        }
        finally
        {
            process.Dispose();
        }
    }
}

static void DeleteDirectoryWithRetry(string path)
{
    const int attempts = 8;
    for (var attempt = 1; attempt <= attempts; attempt++)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            return;
        }
        catch (IOException) when (attempt < attempts)
        {
            Thread.Sleep(500);
        }
        catch (UnauthorizedAccessException) when (attempt < attempts)
        {
            Thread.Sleep(500);
        }
    }

    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
}

static void CreateShortcuts(string appExe, bool createDesktopShortcut)
{
    DeleteShortcuts();

    var startMenuDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Programs),
        AppDisplayName);
    Directory.CreateDirectory(startMenuDir);

    CreateShortcut(Path.Combine(startMenuDir, AppDisplayName + ".lnk"), appExe);
    if (createDesktopShortcut)
    {
        CreateShortcut(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppDisplayName + ".lnk"),
            appExe);
    }
}

static void DeleteShortcuts()
{
    DeleteFileIfExists(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Programs),
        "ApriP7M",
        "ApriP7M.lnk"));
    DeleteFileIfExists(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "ApriP7M.lnk"));
    DeleteFileIfExists(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        AppDisplayName + ".lnk"));

    var startMenuDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Programs),
        AppDisplayName);
    DeleteFileIfExists(Path.Combine(startMenuDir, AppDisplayName + ".lnk"));
    DeleteDirectoryIfEmpty(startMenuDir);

    var legacyStartMenuDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Programs),
        "ApriP7M");
    DeleteDirectoryIfEmpty(legacyStartMenuDir);
}

static void DeleteFileIfExists(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}

static void DeleteDirectoryIfEmpty(string path)
{
    if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
    {
        Directory.Delete(path);
    }
}

static void CreateShortcut(string shortcutPath, string targetPath)
{
    var shellType = Type.GetTypeFromProgID("WScript.Shell")
        ?? throw new InvalidOperationException("WScript.Shell non disponibile.");
    dynamic shell = Activator.CreateInstance(shellType)
        ?? throw new InvalidOperationException("Impossibile creare lo shortcut.");
    dynamic shortcut = shell.CreateShortcut(shortcutPath);
    shortcut.TargetPath = targetPath;
    shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
    shortcut.IconLocation = targetPath + ",0";
    shortcut.Save();
}

static void RegisterFileAssociations(string appExe, IReadOnlySet<string> extensions)
{
    foreach (var association in AssociationDefinition.All)
    {
        if (extensions.Contains(association.Extension, StringComparer.OrdinalIgnoreCase))
        {
            RegisterAssociation(appExe, association);
        }
        else
        {
            UnregisterAssociation(association);
        }
    }

    NotifyShellAssociationChanged();
}

static void RegisterAssociation(string appExe, AssociationDefinition association)
{
    using (var extension = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + association.Extension))
    {
        extension?.SetValue("", association.ProgId);
    }

    using (var prog = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + association.ProgId))
    {
        prog?.SetValue("", association.DisplayName);
    }

    using (var icon = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + association.ProgId + @"\DefaultIcon"))
    {
        icon?.SetValue("", "\"" + appExe + "\",0");
    }

    using (var command = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + association.ProgId + @"\shell\open\command"))
    {
        command?.SetValue("", "\"" + appExe + "\" \"%1\"");
    }
}

static void UnregisterFileAssociations(IEnumerable<AssociationDefinition> associations)
{
    foreach (var association in associations)
    {
        UnregisterAssociation(association);
    }

    NotifyShellAssociationChanged();
}

static void UnregisterAssociation(AssociationDefinition association)
{
    using (var extension = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + association.Extension, writable: true))
    {
        if (extension is not null &&
            string.Equals(extension.GetValue("") as string, association.ProgId, StringComparison.OrdinalIgnoreCase))
        {
            extension.DeleteValue("", throwOnMissingValue: false);
        }
    }

    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + association.ProgId, throwOnMissingSubKey: false);
}

static string InstallUninstaller(string programsRoot)
{
    var currentExe = Environment.ProcessPath;
    if (string.IsNullOrWhiteSpace(currentExe) || !File.Exists(currentExe))
    {
        throw new InvalidOperationException("Impossibile preparare l'uninstaller.");
    }

    var uninstallerRoot = Path.Combine(programsRoot, UninstallerDirectoryName);
    Directory.CreateDirectory(uninstallerRoot);

    var uninstallerPath = Path.Combine(uninstallerRoot, UninstallerExeName);
    if (!string.Equals(currentExe, uninstallerPath, StringComparison.OrdinalIgnoreCase))
    {
        File.Copy(currentExe, uninstallerPath, overwrite: true);
    }

    return uninstallerPath;
}

static void RegisterUninstallEntry(string installRoot, string uninstallerPath)
{
    var appExe = Path.Combine(installRoot, AppExeName);
    using var key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath)
        ?? throw new InvalidOperationException("Impossibile registrare la disinstallazione.");

    key.SetValue("DisplayName", AppDisplayName);
    key.SetValue("DisplayVersion", AppVersion);
    key.SetValue("Publisher", AppPublisher);
    key.SetValue("InstallLocation", installRoot);
    key.SetValue("DisplayIcon", "\"" + appExe + "\",0");
    key.SetValue("UninstallString", "\"" + uninstallerPath + "\" --uninstall");
    key.SetValue("QuietUninstallString", "\"" + uninstallerPath + "\" --uninstall --quiet");
    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

    var estimatedSizeKb = Directory.EnumerateFiles(installRoot, "*", SearchOption.AllDirectories)
        .Sum(file => new FileInfo(file).Length / 1024);
    key.SetValue("EstimatedSize", Math.Min(estimatedSizeKb, int.MaxValue), RegistryValueKind.DWord);
}

static void ScheduleDirectoryRemoval(string installRoot)
{
    var command = "/c timeout /t 2 /nobreak >nul & rmdir /s /q \"" + installRoot + "\"";
    Process.Start(new ProcessStartInfo("cmd.exe", command)
    {
        CreateNoWindow = true,
        UseShellExecute = false,
        WindowStyle = ProcessWindowStyle.Hidden
    });
}

static bool IsChildPath(string parent, string child)
{
    var parentFull = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    var childFull = Path.GetFullPath(child);
    return childFull.StartsWith(parentFull, StringComparison.OrdinalIgnoreCase);
}

static void AssertSafeInstallPath(string installRoot)
{
    var fullPath = Path.GetFullPath(installRoot);
    var root = Path.GetPathRoot(fullPath);
    if (string.IsNullOrWhiteSpace(root) ||
        string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Scegli una cartella di installazione, non la radice del disco.");
    }

    var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    if (!string.IsNullOrWhiteSpace(windowsDir) &&
        fullPath.StartsWith(Path.GetFullPath(windowsDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Non installare Apri P7M dentro la cartella di Windows.");
    }

    var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    if (!string.IsNullOrWhiteSpace(programFiles) &&
        fullPath.StartsWith(Path.GetFullPath(programFiles).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
        !IsAdministrator())
    {
        throw new InvalidOperationException("Per installare in Programmi avvia il setup come amministratore, oppure usa la cartella predefinita.");
    }

    if (Directory.Exists(fullPath) &&
        Directory.EnumerateFileSystemEntries(fullPath).Any() &&
        !File.Exists(Path.Combine(fullPath, AppExeName)))
    {
        throw new InvalidOperationException("La cartella scelta contiene altri file. Scegli una cartella vuota o una cartella dove Apri P7M è già installato.");
    }
}

static bool IsAdministrator()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}

static void ShowMessage(string title, string message, MessageBoxIcon icon)
    => MessageBox.Show(message, title, MessageBoxButtons.OK, icon);

static void NotifyShellAssociationChanged()
    => SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

[DllImport("shell32.dll")]
static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

sealed class UninstallWizard : Form
{
    private readonly Action _uninstallAction;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 18) };
    private readonly Button _primaryButton = new() { Text = "Disinstalla", Width = 112 };
    private readonly Button _cancelButton = new() { Text = "Annulla", Width = 96 };
    private bool _completed;
    private bool _running;

    public UninstallWizard(Action uninstallAction)
    {
        _uninstallAction = uninstallAction;
        Text = "Disinstallazione Apri P7M";
        Width = 640;
        Height = 380;
        MinimumSize = new Size(600, 340);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 58,
            ColumnCount = 4,
            Padding = new Padding(18, 12, 18, 12)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        footer.Controls.Add(_cancelButton, 1, 0);
        footer.Controls.Add(_primaryButton, 3, 0);
        _cancelButton.Dock = DockStyle.Fill;
        _primaryButton.Dock = DockStyle.Fill;

        _primaryButton.Click += (_, _) =>
        {
            if (_completed)
            {
                Close();
                return;
            }

            if (!_running)
            {
                RunUninstall();
            }
        };
        _cancelButton.Click += (_, _) => Close();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.Controls.Add(_content, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
        RenderConfirm();
    }

    private void RenderConfirm()
    {
        _content.Controls.Clear();
        _primaryButton.Text = "Disinstalla";
        _primaryButton.Enabled = true;
        _cancelButton.Text = "Annulla";
        _cancelButton.Enabled = true;

        _content.Controls.Add(Stack(
            Title("Disinstallare Apri P7M?"),
            Body("La procedura rimuoverà Apri P7M dal PC, i collegamenti dal desktop e dal menu Start, la voce in App installate e le associazioni file create dall'installer.\r\n\r\nI tuoi documenti non vengono toccati.")
        ));
    }

    private void RunUninstall()
    {
        _running = true;
        _primaryButton.Enabled = false;
        _cancelButton.Enabled = false;
        _content.Controls.Clear();
        _content.Controls.Add(Stack(
            Title("Disinstallazione in corso"),
            Body("Sto rimuovendo Apri P7M e ripulendo i collegamenti di Windows."),
            new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 35,
                Height = 18,
                Dock = DockStyle.Top
            }
        ));
        Refresh();

        try
        {
            _uninstallAction();
            RenderComplete();
        }
        catch (Exception ex)
        {
            RenderError(ex.Message);
        }
    }

    private void RenderComplete()
    {
        _running = false;
        _completed = true;
        _content.Controls.Clear();
        _primaryButton.Text = "Chiudi";
        _primaryButton.Enabled = true;
        _cancelButton.Visible = false;

        _content.Controls.Add(Stack(
            Title("Disinstallazione completata"),
            Body("Apri P7M è stato rimosso. Se qualche file era ancora in uso, Windows completerà la pulizia tra pochi secondi.")
        ));
    }

    private void RenderError(string message)
    {
        _running = false;
        _content.Controls.Clear();
        _primaryButton.Text = "Riprova";
        _primaryButton.Enabled = true;
        _cancelButton.Text = "Chiudi";
        _cancelButton.Enabled = true;
        _cancelButton.Visible = true;

        _content.Controls.Add(Stack(
            Title("Non sono riuscito a completare la disinstallazione"),
            Body(message + "\r\n\r\nChiudi Apri P7M se è ancora aperto e riprova.")
        ));
    }

    private static Label Title(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 42,
        Font = new Font("Segoe UI", 16, FontStyle.Bold)
    };

    private static Label Body(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 150,
        Font = new Font("Segoe UI", 10.5f)
    };

    private static Control Stack(params Control[] controls)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = false
        };

        foreach (var control in controls)
        {
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(0, 0, 0, 14);
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(control, 0, panel.RowCount++);
        }

        return panel;
    }
}

sealed class InstallerWizard : Form
{
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 18) };
    private readonly Button _backButton = new() { Text = "Indietro", Width = 96, Enabled = false };
    private readonly Button _nextButton = new() { Text = "Avanti", Width = 96 };
    private readonly Button _cancelButton = new() { Text = "Annulla", Width = 96 };
    private readonly Label _title = new() { Dock = DockStyle.Top, Height = 42, Font = new Font("Segoe UI", 16, FontStyle.Bold) };
    private readonly List<Func<Control>> _pages;
    private int _pageIndex;

    public InstallOptions Options { get; }

    public InstallerWizard(InstallOptions options)
    {
        Options = options;
        Text = "Installazione Apri P7M";
        Width = 760;
        Height = 540;
        MinimumSize = new Size(720, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty);

        _pages = new List<Func<Control>>
        {
            BuildWelcomePage,
            BuildDestinationPage,
            BuildOptionsPage,
            BuildSummaryPage
        };

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 58,
            ColumnCount = 6,
            Padding = new Padding(18, 12, 18, 12)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        footer.Controls.Add(_backButton, 1, 0);
        footer.Controls.Add(_nextButton, 3, 0);
        footer.Controls.Add(_cancelButton, 5, 0);
        _backButton.Dock = DockStyle.Fill;
        _nextButton.Dock = DockStyle.Fill;
        _cancelButton.Dock = DockStyle.Fill;

        _backButton.Click += (_, _) => MovePage(-1);
        _nextButton.Click += (_, _) =>
        {
            if (_pageIndex == _pages.Count - 1)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            MovePage(1);
        };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.Controls.Add(_content, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
        RenderPage();
    }

    private void MovePage(int direction)
    {
        _pageIndex = Math.Clamp(_pageIndex + direction, 0, _pages.Count - 1);
        RenderPage();
    }

    private void RenderPage()
    {
        _content.Controls.Clear();
        _content.Controls.Add(_pages[_pageIndex]());
        _backButton.Enabled = _pageIndex > 0;
        _nextButton.Text = _pageIndex == _pages.Count - 1 ? "Installa" : "Avanti";
    }

    private Control BuildWelcomePage()
    {
        _title.Text = "Benvenuto in Apri P7M";
        var body = BodyText(
            "Questa procedura installerà Apri P7M sul PC.\r\n\r\n" +
            "Apri P7M apre file P7M, XML e fatture elettroniche senza caricare documenti online.\r\n\r\n" +
            "Durante l'installazione puoi scegliere la cartella, creare il collegamento sul desktop e decidere se associare .p7m e .xml.");

        return Stack(_title, body);
    }

    private Control BuildDestinationPage()
    {
        _title.Text = "Cartella di installazione";
        var pathBox = new TextBox { Text = Options.InstallRoot, Dock = DockStyle.Fill };
        var browseButton = new Button { Text = "Sfoglia...", Width = 96 };
        browseButton.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Scegli dove installare Apri P7M",
                SelectedPath = pathBox.Text,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                pathBox.Text = dialog.SelectedPath;
                Options.InstallRoot = dialog.SelectedPath;
            }
        };
        pathBox.TextChanged += (_, _) => Options.InstallRoot = pathBox.Text;

        var row = new TableLayoutPanel { Dock = DockStyle.Top, Height = 36, ColumnCount = 2 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        row.Controls.Add(pathBox, 0, 0);
        row.Controls.Add(browseButton, 1, 0);
        browseButton.Dock = DockStyle.Fill;

        var help = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 80,
            Font = new Font("Segoe UI", 9.5f),
            Text = "La cartella predefinita non richiede privilegi amministrativi. Scegli una cartella vuota o una cartella dove Apri P7M è già installato. Per Programmi può servire avviare il setup come amministratore."
        };

        return Stack(_title, row, help);
    }

    private Control BuildOptionsPage()
    {
        _title.Text = "Opzioni";
        var desktop = Check("Crea collegamento sul desktop", Options.CreateDesktopShortcut, value => Options.CreateDesktopShortcut = value);
        var p7m = Check("Apri sempre i file .p7m con Apri P7M", Options.FileAssociations.Contains(".p7m"), value => SetAssociation(".p7m", value));
        var xml = Check("Apri sempre i file .xml con Apri P7M", Options.FileAssociations.Contains(".xml"), value => SetAssociation(".xml", value));
        var launch = Check("Avvia Apri P7M al termine dell'installazione", Options.LaunchAfterInstall, value => Options.LaunchAfterInstall = value);

        var help = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 58,
            Font = new Font("Segoe UI", 9.5f),
            Text = "Consiglio: associa .p7m e .xml. Gli archivi .zip restano apribili scegliendoli dall'app, senza prendere il posto del gestore ZIP di Windows."
        };

        return Stack(_title, desktop, p7m, xml, launch, help);
    }

    private Control BuildSummaryPage()
    {
        _title.Text = "Pronto per installare";
        var associations = Options.FileAssociations.Count == 0
            ? "Nessuna"
            : string.Join(", ", Options.FileAssociations.OrderBy(x => x));

        var summary = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 10.5f),
            Height = 230,
            Text = "Riepilogo\r\n\r\n" +
                   "Cartella di installazione:\r\n" + Options.InstallRoot + "\r\n\r\n" +
                   "Collegamento sul desktop: " + (Options.CreateDesktopShortcut ? "Sì" : "No") + "\r\n" +
                   "Estensioni associate: " + associations + "\r\n" +
                   "Avvio dopo installazione: " + (Options.LaunchAfterInstall ? "Sì" : "No") + "\r\n\r\n" +
                   "Clicca Installa per continuare."
        };

        return Stack(_title, summary);
    }

    private static TextBox BodyText(string text) => new()
    {
        Multiline = true,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = SystemColors.Control,
        Font = new Font("Segoe UI", 10.5f),
        Height = 190,
        TabStop = false,
        Text = text
    };

    private static CheckBox Check(string text, bool isChecked, Action<bool> onChanged)
    {
        var checkbox = new CheckBox
        {
            Text = text,
            Checked = isChecked,
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 10)
        };
        checkbox.CheckedChanged += (_, _) => onChanged(checkbox.Checked);
        return checkbox;
    }

    private void SetAssociation(string extension, bool enabled)
    {
        if (enabled)
        {
            Options.FileAssociations.Add(extension);
        }
        else
        {
            Options.FileAssociations.Remove(extension);
        }
    }

    private static Control Stack(params Control[] controls)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = false
        };

        foreach (var control in controls)
        {
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(0, 0, 0, 14);
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(control, 0, panel.RowCount++);
        }

        return panel;
    }
}

sealed class InstallOptions
{
    public string InstallRoot { get; set; } = "";
    public bool CreateDesktopShortcut { get; set; } = true;
    public bool LaunchAfterInstall { get; set; } = true;
    public bool Quiet { get; set; }
    public HashSet<string> FileAssociations { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static InstallOptions CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var options = new InstallOptions
        {
            InstallRoot = Path.Combine(localAppData, "Programs", "Apri P7M")
        };
        options.FileAssociations.Add(".p7m");
        options.FileAssociations.Add(".xml");
        return options;
    }
}

sealed record AssociationDefinition(string Extension, string ProgId, string DisplayName)
{
    public static readonly AssociationDefinition[] All =
    [
        new(".p7m", "ApriP7M.p7m", "File firmato P7M"),
        new(".xml", "ApriP7M.xml", "Documento XML"),
        new(".zip", "ApriP7M.zip", "Archivio ZIP")
    ];
}
