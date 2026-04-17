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

        public async Task EsportaAsync(OpzioniDistribuzione opzioni)
        {
            // Licenza gratuita EPPlus
            ExcelPackage.License.SetNonCommercialOrganization("<Your Noncommercial Organization>");

            // Recupero matrice di studenti per classe con metadati (sezione + indirizzo)
            var risultati = await _distribuzioneService.DistribuisciConMetadatiAsync(opzioni);

            if (risultati.Count == 0)
            {
                Console.WriteLine("Nessuno studente da distribuire.");
                return;
            }

            using (var package = new ExcelPackage())
            {
                foreach (var (classe, studenti) in risultati)
                {
                    if (studenti.Count == 0) continue;

                    // Nome del foglio = "1" + sezione (es. "1E", "1F", "1Bio")
                    string sezione = classe.Sezione.Valore;
                    string indirizzo = classe.Indirizzo.Nome;
                    string nomeFoglio = $"1{sezione}";
                    // Trunca il nome a 31 caratteri se troppo lungo (limite Excel)
                    if (nomeFoglio.Length > 31)
                        nomeFoglio = nomeFoglio.Substring(0, 31);

                    // Creazione foglio Excel
                    var worksheet = package.Workbook.Worksheets.Add(nomeFoglio);

                    // Titolo: "Classe 1E" su 6 colonne
                    worksheet.Cells[1, 1, 1, 6].Merge = true;
                    worksheet.Cells[1, 1].Value = $"Classe 1{sezione}";
                    worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Indirizzo su riga 2
                    worksheet.Cells[2, 1, 2, 6].Merge = true;
                    worksheet.Cells[2, 1].Value = $"Indirizzo: {indirizzo}";
                    worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Intestazioni colonne su riga 4
                    worksheet.Cells[4, 1].Value = "n";
                    worksheet.Cells[4, 2].Value = "Cognome";
                    worksheet.Cells[4, 3].Value = "Nome";
                    worksheet.Cells[4, 4].Value = "Scelta Compagno";
                    worksheet.Cells[4, 5].Value = "DSA";
                    worksheet.Cells[4, 6].Value = "Disabilità";

                    // Allineamento numero
                    worksheet.Cells[4, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    // Ordinamento alfabetico — manteniamo i dati extra per le nuove colonne
                    var studentiOrdinati = studenti
                        .Select(s => (
                            Cognome: s.Cognome,
                            Nome: s.Nome,
                            SceltaCompagno: s.SceltaCompagno?.Testo,
                            DSA: s.ProfiloBES.HasDSA,
                            Disabilita: s.ProfiloBES.HasDisabilita
                        ))
                        .OrderBy(s => s.Cognome)
                        .ThenBy(s => s.Nome)
                        .ToList();

                    // Font in grassetto per titolo, indirizzo e intestazioni
                    using (var range = worksheet.Cells[1, 1, 4, 6])
                        range.Style.Font.Bold = true;

                    // Inserimento dati a partire da riga 5
                    int row = 5;
                    int num = 1;
                    foreach (var studente in studentiOrdinati)
                    {
                        worksheet.Cells[row, 1].Value = num;
                        worksheet.Cells[row, 2].Value = studente.Cognome;
                        worksheet.Cells[row, 3].Value = studente.Nome;

                        // Colonna Scelta Compagno
                        worksheet.Cells[row, 4].Value = !string.IsNullOrWhiteSpace(studente.SceltaCompagno)
                            ? studente.SceltaCompagno
                            : "-";

                        // Colonna DSA
                        worksheet.Cells[row, 5].Value = studente.DSA ? "Sì" : "No";

                        // Colonna Disabilità
                        worksheet.Cells[row, 6].Value = studente.Disabilita ? "Sì" : "No";

                        row++;
                        num++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    // Formattazione bordi (solo dalla riga delle intestazioni in poi)
                    using (var range = worksheet.Cells[4, 1, row - 1, 6])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }
                }

                // Salvataggio sul desktop
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string percorso = Path.Combine(desktop, "Classibase.xlsx");
                package.SaveAs(new FileInfo(percorso));

                // Apri il file
                Process.Start(new ProcessStartInfo
                {
                    FileName = percorso,
                    UseShellExecute = true
                });
            }
        }
    }
}
