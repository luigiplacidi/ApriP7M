using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ApriP7M.App.Services;

namespace ApriP7M.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();

        DiagnosticsToggle.IsOn = App.Settings.DiagnosticsEnabled;
        StartupCheckToggle.IsOn = App.Settings.CheckUpdatesOnStartup;
        FileAssociationService.RemoveObsoleteAssociations();
        P7mAssociationToggle.IsOn = FileAssociationService.IsAssociated(FileAssociationService.P7m);
        XmlAssociationToggle.IsOn = FileAssociationService.IsAssociated(FileAssociationService.Xml);
        ThemeCombo.SelectedIndex = App.Settings.Theme switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };

        DiagnosticsToggle.Toggled += (_, _) =>
        {
            App.Settings.DiagnosticsEnabled = DiagnosticsToggle.IsOn;
            App.SaveSettings();
        };

        StartupCheckToggle.Toggled += (_, _) =>
        {
            App.Settings.CheckUpdatesOnStartup = StartupCheckToggle.IsOn;
            App.SaveSettings();
        };

        P7mAssociationToggle.Toggled += (_, _) =>
            UpdateAssociation(FileAssociationService.P7m, P7mAssociationToggle.IsOn);
        XmlAssociationToggle.Toggled += (_, _) =>
            UpdateAssociation(FileAssociationService.Xml, XmlAssociationToggle.IsOn);
    }

    private void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is ComboBoxItem item)
        {
            App.ApplyTheme(item.Tag as string ?? "system");
            App.SaveSettings();
        }
    }

    private void UpdateAssociation(FileAssociationOption option, bool enabled)
    {
        try
        {
            FileAssociationService.SetAssociation(option, enabled);
            AssociationStatusBar.Severity = InfoBarSeverity.Success;
            AssociationStatusBar.Title = enabled
                ? $"{option.Extension} associato ad Apri P7M"
                : $"{option.Extension} non è più associato";
            AssociationStatusBar.Message = "La modifica è stata applicata a Esplora file.";
            AssociationStatusBar.IsOpen = true;
        }
        catch (Exception ex)
        {
            AssociationStatusBar.Severity = InfoBarSeverity.Error;
            AssociationStatusBar.Title = "Non riesco a modificare l'associazione";
            AssociationStatusBar.Message = ex.Message;
            AssociationStatusBar.IsOpen = true;
        }
    }
}
