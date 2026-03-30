using BlaisePascal.ProjectWork._3E.Domain.Services;
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
        private readonly DistribuzioneClassiService _distribuzioneService;

        public EsportazioneDatiExcel(DistribuzioneClassiService distribuzioneService)
        {
            _distribuzioneService = distribuzioneService;
        }

        public async Task EsportaAsync()
        {
            // Licenza gratuita EPPlus
            ExcelPackage.License.SetNonCommercialOrganization("<Your Noncommercial Organization>");

            // Recupero matrice di studenti per classe
            var matriceClassi = await _distribuzioneService.DistribuisciAsync();

            if (matriceClassi.Count == 0)
            {
                Console.WriteLine("Nessuno studente da distribuire.");
                return;
            }

            using (var package = new ExcelPackage())
            {
                int classeNum = 1;

                foreach (var studenti in matriceClassi)
                {
                    if (studenti.Count == 0) continue;

                    // Nome del foglio
                    string nomeClasse = $"Classe {classeNum}";
                    // Trunca il nome a 31 caratteri se troppo lungo (limite Excel)
                    if (nomeClasse.Length > 31)
                        nomeClasse = nomeClasse.Substring(0, 31);

                    // Creazione foglio Excel
                    var worksheet = package.Workbook.Worksheets.Add(nomeClasse);

                    // Titolo classe
                    worksheet.Cells[1, 1, 1, 3].Merge = true;
                    worksheet.Cells[1, 1].Value = "Classe: " + nomeClasse;

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

                    classeNum++;
                }

                // Salvataggio su desktop del file con solo le informazioni base(elenco co nome e cognome)
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
