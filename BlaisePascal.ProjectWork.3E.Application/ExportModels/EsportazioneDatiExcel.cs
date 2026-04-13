using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Application.ExportModels
{
    public class EsportazioneDatiExcel
    {
        public void Esporta(List<List<BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.Studente>> matriceClassi, List<ClassePrima> classi)
        {
            // Licenza gratuita EPPlus
            ExcelPackage.License.SetNonCommercialOrganization("PCTO School Project");

            if (matriceClassi.Count == 0)
            {
                Console.WriteLine("Nessuno studente da distribuire.");
                return;
            }

            // Dizionario per cercare la classe tramite Id
            var classiById = classi.ToDictionary(c => c.Id);

            using (var package = new ExcelPackage())
            {
                foreach (var studenti in matriceClassi)
                {
                    if (studenti.Count == 0) continue;

                    // Recupera la classe reale dal ClasseId del primo studente
                    string nomeSezione;
                    string titoloClasse;
                    var classeId = studenti.First().ClasseId;
                    if (classeId.HasValue && classiById.TryGetValue(classeId.Value, out var classeTarget))
                    {
                        nomeSezione = $"1{classeTarget.Sezione.Valore}";
                        titoloClasse = $"Classe 1{classeTarget.Sezione.Valore} - {classeTarget.Indirizzo.Nome}";
                    }
                    else
                    {
                        nomeSezione = "Classe";
                        titoloClasse = "Classe";
                    }

                    // Trunca il nome a 31 caratteri se troppo lungo (limite Excel)
                    if (nomeSezione.Length > 31)
                        nomeSezione = nomeSezione.Substring(0, 31);

                    // Creazione foglio Excel
                    var worksheet = package.Workbook.Worksheets.Add(nomeSezione);

                    // Titolo classe
                    worksheet.Cells[1, 1, 1, 3].Merge = true;
                    worksheet.Cells[1, 1].Value = titoloClasse;

                    // Intestazioni
                    worksheet.Cells[3, 1].Value = "n";
                    worksheet.Cells[3, 2].Value = "Cognome";
                    worksheet.Cells[3, 3].Value = "Nome";

                    // Allineamento celle
                    worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    // Ordinamento alfabetico
                    var studentiOrdinati = studenti
                        .Select(s => (Cognome: s.Cognome, Nome: s.Nome))
                        .OrderBy(s => s.Cognome)
                        .ThenBy(s => s.Nome)
                        .ToList();

                    // Font in grassetto
                    using (var range = worksheet.Cells[1, 1, 3, 3])
                        range.Style.Font.Bold = true;

                    // Inserimento dati
                    int row = 4;
                    int num = 1;
                    foreach (var studente in studentiOrdinati)
                    {
                        worksheet.Cells[row, 1].Value = num;
                        worksheet.Cells[row, 2].Value = studente.Cognome;
                        worksheet.Cells[row, 3].Value = studente.Nome;
                        row++;
                        num++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    // Formattazione bordi
                    using (var range = worksheet.Cells[3, 1, row - 1, 3])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }


                }

                // Salvataggio su desktop del file con solo le informazioni base(elenco con nome e cognome)
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string percorso = Path.Combine(desktop, "Classibase.xlsx");
                package.SaveAs(new FileInfo(percorso));

                // Apri il file
                Process.Start(new ProcessStartInfo
                {
                    FileName = percorso,
                    UseShellExecute = true
                });

                //Console.WriteLine("File Excel salvato sul desktop");
            }
        }
    }
}
