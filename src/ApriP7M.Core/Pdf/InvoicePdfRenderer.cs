using ApriP7M.Core.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApriP7M.Core.Pdf;

/// <summary>
/// Genera una copia leggibile di cortesia (PDF) a partire dal modello di una
/// fattura elettronica.
///
/// Il PDF prodotto NON è il documento fiscalmente rilevante: lo è il file XML
/// originale. Questo avviso compare anche nel PDF stesso.
/// </summary>
public static class InvoicePdfRenderer
{
    private const string Stage = "Pdf.InvoicePdfRenderer";

    public const string CourtesyNotice =
        "Il PDF generato è una copia leggibile di cortesia. " +
        "Il documento fiscalmente rilevante resta il file XML originale.";

    static InvoicePdfRenderer()
    {
        // QuestPDF Community License (gratuita). Vedi NOTICE / README per i termini.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Render(InvoiceModel invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        try
        {
            var document = Document.Create(container =>
            {
                foreach (var doc in invoice.Documents)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Segoe UI", "Arial"));

                        page.Header().Element(c => ComposeHeader(c, invoice, doc));
                        page.Content().Element(c => ComposeContent(c, invoice, doc));
                        page.Footer().Element(ComposeFooter);
                    });
                }
            });

            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            throw new ApriP7MException(ErrorCode.PdfRenderFailed,
                "Non è stato possibile generare il PDF leggibile della fattura.", Stage, ex);
        }
    }

    private static void ComposeHeader(IContainer container, InvoiceModel invoice, InvoiceDocument doc)
    {
        container.Column(col =>
        {
            col.Item().Text("Fattura elettronica — copia di cortesia")
                .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

            col.Item().PaddingBottom(4).Text(t =>
            {
                t.Span("Documento ").FontColor(Colors.Grey.Darken1);
                t.Span($"{doc.DocumentType} n. {doc.Number}").SemiBold();
                t.Span($"  del {doc.Date}").FontColor(Colors.Grey.Darken1);
            });

            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeContent(IContainer container, InvoiceModel invoice, InvoiceDocument doc)
    {
        container.PaddingVertical(8).Column(col =>
        {
            col.Spacing(10);

            // Cedente / Cessionario
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => PartyBox(c, "Cedente / Prestatore", invoice.Supplier));
                row.ConstantItem(12);
                row.RelativeItem().Element(c => PartyBox(c, "Cessionario / Committente", invoice.Customer));
            });

            // Righe
            if (doc.Lines.Count > 0)
            {
                col.Item().Text("Dettaglio").SemiBold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(28);   // n.
                        c.RelativeColumn(5);    // descrizione
                        c.RelativeColumn();     // q.tà
                        c.RelativeColumn();     // prezzo
                        c.RelativeColumn();     // IVA
                        c.RelativeColumn();     // totale
                    });

                    table.Header(h =>
                    {
                        HeaderCell(h, "#");
                        HeaderCell(h, "Descrizione");
                        HeaderCell(h, "Q.tà");
                        HeaderCell(h, "Prezzo");
                        HeaderCell(h, "IVA %");
                        HeaderCell(h, "Totale");
                    });

                    foreach (var line in doc.Lines)
                    {
                        BodyCell(table, line.LineNumber);
                        BodyCell(table, line.Description);
                        BodyCell(table, line.Quantity);
                        BodyCell(table, line.UnitPrice);
                        BodyCell(table, line.VatRate);
                        BodyCell(table, line.TotalPrice);
                    }
                });
            }

            // Riepilogo IVA + totale
            if (doc.VatSummaries.Count > 0)
            {
                col.Item().PaddingTop(6).Text("Riepilogo IVA").SemiBold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });
                    table.Header(h =>
                    {
                        HeaderCell(h, "Aliquota %");
                        HeaderCell(h, "Imponibile");
                        HeaderCell(h, "Imposta");
                    });
                    foreach (var v in doc.VatSummaries)
                    {
                        BodyCell(table, v.VatRate);
                        BodyCell(table, v.TaxableAmount);
                        BodyCell(table, v.Tax);
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(doc.TotalAmount))
            {
                col.Item().AlignRight().Text(t =>
                {
                    t.Span("Totale documento: ").SemiBold();
                    t.Span($"{doc.TotalAmount} {doc.Currency}").Bold().FontSize(11);
                });
            }

            if (invoice.Attachments.Count > 0)
            {
                col.Item().PaddingTop(6).Text(
                    $"Allegati presenti nella fattura: {invoice.Attachments.Count}. " +
                    "Possono essere salvati separatamente da Apri P7M.")
                    .FontColor(Colors.Grey.Darken1).Italic();
            }
        });
    }

    private static void PartyBox(IContainer container, string title, Party party)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(col =>
        {
            col.Item().Text(title).FontSize(8).FontColor(Colors.Grey.Darken1).SemiBold();
            col.Item().Text(party.Name).SemiBold();
            if (!string.IsNullOrWhiteSpace(party.VatNumber))
            {
                col.Item().Text($"P.IVA {party.VatNumber}");
            }
            if (!string.IsNullOrWhiteSpace(party.FiscalCode))
            {
                col.Item().Text($"C.F. {party.FiscalCode}");
            }
            if (!string.IsNullOrWhiteSpace(party.Address))
            {
                col.Item().Text(party.Address).FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(6).Column(col =>
        {
            col.Item().Text(CourtesyNotice)
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1).Italic();
            col.Item().AlignRight().Text(t =>
            {
                t.Span("Generato con Apri P7M — ").FontSize(7).FontColor(Colors.Grey.Medium);
                t.CurrentPageNumber().FontSize(7);
                t.Span(" / ").FontSize(7);
                t.TotalPages().FontSize(7);
            });
        });
    }

    private static void HeaderCell(QuestPDF.Fluent.TableCellDescriptor h, string text)
        => h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).SemiBold().FontSize(8);

    private static void BodyCell(QuestPDF.Fluent.TableDescriptor table, string text)
        => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
            .Text(text ?? "").FontSize(8);
}
