namespace ApriP7M.App.Services;

/// <summary>
/// Link per il sostegno al progetto, in un unico punto. Per aggiungere
/// Satispay basta incollarne l'URL qui: il pulsante compare da solo dove
/// serve (nell'app e nell'invito discreto dopo l'apertura di un documento).
/// </summary>
public static class DonationLinks
{
    public const string PayPal =
        "https://www.paypal.com/donate/?hosted_button_id=7ZTNNLPSGE2BU&locale.x=it_IT";

    // Incolla qui l'URL Satispay quando è pronto (es. https://tag.satispay.com/...).
    public const string Satispay = "";

    public static bool HasSatispay => !string.IsNullOrWhiteSpace(Satispay);
}
