using System.Reflection;
using System.Xml;
using System.Xml.Xsl;

namespace ApriP7M.Core.Invoice;

/// <summary>
/// Trasforma l'XML di una fattura in HTML leggibile, tramite XSLT, per l'anteprima
/// in WebView2. Lettura XML anti-XXE; nessun accesso di rete.
/// Alternativa "HTML" al PDF generato con QuestPDF.
/// </summary>
public static class XsltInvoiceTransformer
{
    private const string Stage = "Invoice.XsltInvoiceTransformer";
    private static readonly Lazy<XslCompiledTransform> Transform = new(LoadStylesheet);

    public static string ToHtml(byte[] xmlBytes)
    {
        if (xmlBytes is null || xmlBytes.Length == 0)
        {
            throw new ApriP7MException(ErrorCode.EmptyFile, "Il file XML è vuoto.", Stage);
        }

        try
        {
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 0
            };

            using var input = new MemoryStream(xmlBytes);
            using var xmlReader = XmlReader.Create(input, readerSettings);

            using var output = new StringWriter();
            using var htmlWriter = XmlWriter.Create(output, Transform.Value.OutputSettings);
            Transform.Value.Transform(xmlReader, htmlWriter);
            return output.ToString();
        }
        catch (ApriP7MException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApriP7MException(ErrorCode.InvoiceParseFailed,
                "Non è stato possibile generare l'anteprima HTML della fattura.", Stage, ex);
        }
    }

    private static XslCompiledTransform LoadStylesheet()
    {
        var transform = new XslCompiledTransform();
        var path = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
            "Invoice", "Resources", "fattura-ordinaria.xsl");

        var settings = new XsltSettings(enableDocumentFunction: false, enableScript: false);
        using var reader = XmlReader.Create(path, new XmlReaderSettings { XmlResolver = null });
        transform.Load(reader, settings, new XmlUrlResolver());
        return transform;
    }
}
