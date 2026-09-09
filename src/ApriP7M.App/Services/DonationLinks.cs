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

    // Colletta Satispay "Apri P7M".
    public const string Satispay =
        "https://web.satispay.com/download/qrcode/S6Y-SVN--732D9329-C0E2-432B-B4D7-7E7577DC5BB9?locale=it_IT";

    public static bool HasSatispay => !string.IsNullOrWhiteSpace(Satispay);
}
