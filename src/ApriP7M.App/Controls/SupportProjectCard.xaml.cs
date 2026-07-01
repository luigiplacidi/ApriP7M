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
    }

    private async void Donate_Click(object sender, RoutedEventArgs e)
        => await Launcher.LaunchUriAsync(new Uri("https://www.paypal.com/donate/?hosted_button_id=7ZTNNLPSGE2BU"));

    private async void Review_Click(object sender, RoutedEventArgs e)
        => await _store.RequestReviewAsync();
}
