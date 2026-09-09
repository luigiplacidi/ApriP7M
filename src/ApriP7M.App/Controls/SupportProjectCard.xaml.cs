using ApriP7M.App.Services;
using ApriP7M.Store;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace ApriP7M.App.Controls;

public sealed partial class SupportProjectCard : UserControl
{
    private readonly IStoreService _store = new MicrosoftStoreService();

    public SupportProjectCard()
    {
        InitializeComponent();
        if (DonationLinks.HasSatispay)
        {
            SatispayButton.Visibility = Visibility.Visible;
        }
    }

    private async void Donate_Click(object sender, RoutedEventArgs e)
        => await OpenLinkAsync(DonationLinks.PayPal);

    private async void Satispay_Click(object sender, RoutedEventArgs e)
        => await OpenLinkAsync(DonationLinks.Satispay);

    private static async Task OpenLinkAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            await Launcher.LaunchUriAsync(new Uri(url));
        }
        catch
        {
            // Un link che non si apre non deve far cadere l'app.
        }
    }

    private async void Review_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _store.RequestReviewAsync();
        }
        catch
        {
            // Un link che non si apre non deve far cadere l'app.
        }
    }
}
