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
            // Recupero matrice di studenti per classe con metadati (sezione + indirizzo)
            var risultati = await _distribuzioneService.DistribuisciConMetadatiAsync(opzioni);
            Esporta(risultati);
        }

        /// <summary>
        /// Esporta da risultati già distribuiti (senza richiamare la distribuzione).
        /// </summary>
        public void Esporta(List<(BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima.ClassePrima Classe, List<BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.Studente> Studenti)> risultati)
        {
            // Licenza gratuita EPPlus
            ExcelPackage.License.SetNonCommercialOrganization("<Your Noncommercial Organization>");

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

                    int totalColumns = 8; // n, Cognome, Nome, Sesso, Scelta Compagno, DSA, Disabilità, Indirizzo Preferito

                    // Titolo: "Classe 1E" su tutte le colonne
                    worksheet.Cells[1, 1, 1, totalColumns].Merge = true;
                    worksheet.Cells[1, 1].Value = $"Classe 1{sezione}";
                    worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Indirizzo su riga 2
                    worksheet.Cells[2, 1, 2, totalColumns].Merge = true;
                    worksheet.Cells[2, 1].Value = $"Indirizzo: {indirizzo}";
                    worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Intestazioni colonne su riga 4
                    worksheet.Cells[4, 1].Value = "n";
                    worksheet.Cells[4, 2].Value = "Cognome";
                    worksheet.Cells[4, 3].Value = "Nome";
                    worksheet.Cells[4, 4].Value = "Sesso";
                    worksheet.Cells[4, 5].Value = "Scelta Compagno";
                    worksheet.Cells[4, 6].Value = "DSA";
                    worksheet.Cells[4, 7].Value = "Disabilità";
                    worksheet.Cells[4, 8].Value = "Indirizzo Preferito";

                    // Allineamento numero
                    worksheet.Cells[4, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    // Ordinamento alfabetico — manteniamo i dati extra per le nuove colonne
                    var studentiOrdinati = studenti
                        .Select(s => (
                            Cognome: s.Cognome,
                            Nome: s.Nome,
                            Sesso: s.Sesso,
                            SceltaCompagno: s.SceltaCompagno?.Testo,
                            DSA: s.ProfiloBES.HasDSA,
                            Disabilita: s.ProfiloBES.HasDisabilita,
                            IndirizzoPreferito: s.IndirizzoPreferito
                        ))
                        .OrderBy(s => s.Cognome)
                        .ThenBy(s => s.Nome)
                        .ToList();

                    // Font in grassetto per titolo, indirizzo e intestazioni
                    using (var range = worksheet.Cells[1, 1, 4, totalColumns])
                        range.Style.Font.Bold = true;

                    // Inserimento dati a partire da riga 5
                    int row = 5;
                    int num = 1;
                    foreach (var studente in studentiOrdinati)
                    {
                        worksheet.Cells[row, 1].Value = num;
                        worksheet.Cells[row, 2].Value = studente.Cognome;
                        worksheet.Cells[row, 3].Value = studente.Nome;

                        // Colonna Sesso
                        worksheet.Cells[row, 4].Value = studente.Sesso == BlaisePascal.ProjectWork._3E.Domain.Enums.Sesso.Maschio ? "M" : "F";

                        // Colonna Scelta Compagno
                        worksheet.Cells[row, 5].Value = !string.IsNullOrWhiteSpace(studente.SceltaCompagno)
                            ? studente.SceltaCompagno
                            : "-";

                        // Colonna DSA
                        worksheet.Cells[row, 6].Value = studente.DSA ? "Sì" : "No";

                        // Colonna Disabilità
                        worksheet.Cells[row, 7].Value = studente.Disabilita ? "Sì" : "No";

                        // Colonna Indirizzo Preferito
                        worksheet.Cells[row, 8].Value = !string.IsNullOrWhiteSpace(studente.IndirizzoPreferito)
                            ? studente.IndirizzoPreferito
                            : "-";

                        // COLORAZIONE RIGHE (priorità: Disabilità > DSA > Femmina)
                        // Giallo per disabili
                        if (studente.Disabilita)
                        {
                            using (var rowRange = worksheet.Cells[row, 1, row, totalColumns])
                            {
                                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 224)); // Giallo chiaro
                            }
                        }
                        // Verde per DSA
                        else if (studente.DSA)
                        {
                            using (var rowRange = worksheet.Cells[row, 1, row, totalColumns])
                            {
                                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(200, 255, 200)); // Verde chiaro
                            }
                        }
                        // Rosa chiaro per ragazze
                        else if (studente.Sesso == BlaisePascal.ProjectWork._3E.Domain.Enums.Sesso.Femmina)
                        {
                            using (var rowRange = worksheet.Cells[row, 1, row, totalColumns])
                            {
                                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 228, 225)); // Rosa chiaro (MistyRose)
                            }
                        }

                        row++;
                        num++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    // Formattazione bordi (solo dalla riga delle intestazioni in poi)
                    using (var range = worksheet.Cells[4, 1, row - 1, totalColumns])
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
