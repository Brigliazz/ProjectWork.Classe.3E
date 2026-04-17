using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BlaisePascal.ProjectWork._3E.Application.ExportModels
{
    public class EsportazioneDatiPDF
    {
        /// <summary>
        /// Genera un PDF a partire dai risultati già distribuiti.
        /// Riceve direttamente la lista di (Classe, Studenti) già pronta.
        /// </summary>
        public void Esporta(List<(ClassePrima Classe, List<Studente> Studenti)> risultati)
        {
            // Licenza community QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            if (risultati.Count == 0)
                return;

            // Salvataggio sul desktop
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string percorso = Path.Combine(desktop, "Classibase.pdf");

            Document.Create(container =>
            {
                foreach (var (classe, studenti) in risultati)
                {
                    if (studenti.Count == 0) continue;

                    string sezione = classe.Sezione.Valore;
                    string indirizzo = classe.Indirizzo.Nome;

                    // Ordinamento alfabetico
                    var studentiOrdinati = studenti
                        .OrderBy(s => s.Cognome)
                        .ThenBy(s => s.Nome)
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
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Anno Scolastico 2025/2026").FontSize(9).FontColor(Colors.Grey.Medium);
                                });
                                row.RelativeItem().AlignRight().Text($"CLASSE 1{sezione}").FontSize(22).ExtraBold().FontColor(Colors.Blue.Medium);
                            });

                            // Indirizzo sotto il titolo
                            col.Item().AlignRight().Text($"Indirizzo: {indirizzo}").FontSize(12).FontColor(Colors.Grey.Darken1);

                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
                        });

                        // Tabella
                        page.Content().PaddingVertical(0.5f, Unit.Centimetre).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);   // N°
                                columns.RelativeColumn(3);    // Cognome
                                columns.RelativeColumn(3);    // Nome
                                columns.RelativeColumn(2);    // Firma
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
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Stampato il: ").FontSize(9);
                                t.Span(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(9);
                            });

                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Pagina ");
                                x.CurrentPageNumber();
                                x.Span(" di ");
                                x.TotalPages();
                            });
                        });
                    });
                }
            })
            .GeneratePdf(percorso);

            // Apertura automatica del PDF
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = percorso,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}