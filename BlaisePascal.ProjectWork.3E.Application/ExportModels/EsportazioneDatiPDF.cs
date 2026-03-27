/*
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;

// 1. Configurazione licenza
QuestPDF.Settings.License = LicenseType.Community;
namespace Blaise.Pascal.ProjectWork._3E.EsportazioneDati
{
    public class EsportazioneDatiPDF
    {
        public void CreaPDF()
        {
            // Implementazione della logica per creare un PDF
            var path = "Registro_Classi_Prime_Aggiornato.pdf";

            // 3. Generazione documento
            Document.Create(container =>
            {
                // Raggruppamento per classe e ordinamento alfabetico (Classe -> Cognome -> Nome)
                var classiRaggruppate = listaStudenti
                    .GroupBy(s => s.Classe)
                    .OrderBy(g => g.Key);

                foreach (var gruppo in classiRaggruppate)
                {
                    var nomeClasse = gruppo.Key;
                    var studentiOrdinati = gruppo
                        .OrderBy(s => s.Cognome)
                        .ThenBy(s => s.Nome) // Questo gestisce i due "Verdi" in 1M
                        .ToList();

                    container.Page(page =>
                    {
                        page.Margin(1.5f, Unit.Centimetre);
                        page.Size(PageSizes.A4);

                        // Header
                        page.Header().Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c => {
                                    c.Item().Text("Anno Scolastico 2026/2027").FontSize(9).FontColor(Colors.Grey.Medium);
                                });
                                row.RelativeItem().AlignRight().Text($"CLASSE {nomeClasse}").FontSize(22).ExtraBold().FontColor(Colors.Blue.Medium);
                            });
                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
                        });

                        // Tabella
                        page.Content().PaddingVertical(0.5f, Unit.Centimetre).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("N°");
                                header.Cell().Element(HeaderStyle).Text("Cognome");
                                header.Cell().Element(HeaderStyle).Text("Nome");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Firma");

                                static IContainer HeaderStyle(IContainer c) =>
                                    c.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1);
                            });

                            for (int i = 0; i < studentiOrdinati.Count; i++)
                            {
                                var studente = studentiOrdinati[i];

                                table.Cell().Element(RowStyle).Text($"{i + 1}");
                                table.Cell().Element(RowStyle).Text(studente.Cognome);
                                table.Cell().Element(RowStyle).Text(studente.Nome);
                                table.Cell().Element(RowStyle).Text("");

                                static IContainer RowStyle(IContainer c) =>
                                    c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(10);
                            }
                        });

                        // Footer
                        page.Footer().Row(row =>
                        {
                            row.RelativeItem().Text(t => {
                                t.Span("Stampato il: ").FontSize(9);
                                t.Span(System.DateTime.Now.ToString("dd/MM/yyyy")).FontSize(9);
                            });

                            row.RelativeItem().AlignRight().Text(x => {
                                x.Span("Pagina ");
                                x.CurrentPageNumber();
                                x.Span(" di ");
                                x.TotalPages();
                            });
                        });
                    });
                }
            })
            .GeneratePdf(path);

            // 4. Apertura automatica
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch { }
        }
    }
}
*/