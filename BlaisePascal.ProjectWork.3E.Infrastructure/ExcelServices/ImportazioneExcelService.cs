using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader; 
using System.Data;
using BlaisePascal.ProjectWork._3E.Application.Interfaces;
using BlaisePascal.ProjectWork._3E.Application.ImportModels;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.ExcelServices
{
    public class ImportazioneExcelService : IImportazioneExcelService
    {
        public DatiImportatiDto EstrapolaDati(string percorsoFile)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // Creiamo l'oggetto contenitore che ha già le 5 liste pronte per essere riempite
            DatiImportatiDto risultato = new DatiImportatiDto();

            if (!File.Exists(percorsoFile))
            {
                Console.WriteLine($"ERRORE: File '{percorsoFile}' non trovato.");
                return risultato; // Restituisce le liste vuote per evitare crash
            }
            /*
            per il percorso file bisogna fare in modo che dalla WPF (sfoglia file) venga messo il percorso nella varibaile percorsoFIle
            */

            using (var stream = File.Open(percorsoFile, FileMode.Open, FileAccess.Read))
            {
                IExcelDataReader reader;

                // CONTROLLO INTELLIGENTE DELL'ESTENSIONE
                if (percorsoFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    // Se il file finisce in .csv, usiamo il lettore per i file di testo
                    // (La configurazione serve per dirgli che in Italia usiamo il punto e virgola ';' per separare i dati)
                    var csvConfig = new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = System.Text.Encoding.GetEncoding(1252),
                        AutodetectSeparators = new char[] { ';', ',' }
                    };
                    reader = ExcelReaderFactory.CreateCsvReader(stream, csvConfig);
                }
                else
                {
                    // Se è un .xls o .xlsx, usiamo il lettore Excel normale
                    reader = ExcelReaderFactory.CreateReader(stream);
                }

                using (reader)
                {
                    var result = reader.AsDataSet();
                    var table = result.Tables[0];

                    // 1. TROVIAMO GLI INDICI DELLE COLONNE
                    int idxNome = TrovaIndiceColonna(table, "nome alunno", "nome");
                    int idxCognome = TrovaIndiceColonna(table, "cognome alunno", "cognome");
                    int idxSesso = TrovaIndiceColonna(table, "sesso");
                    int idxCf = TrovaIndiceColonna(table, "fiscale", "cf");
                    int idxCittadinanza = TrovaIndiceColonna(table, "cittadinanza");
                    int idxResidenza = TrovaIndiceColonna(table, "comune", "residenza");
                    int idxIndirizzo = TrovaIndiceColonna(table, "indirizzo alunno", "IndirizzoDomicilio", "via");
                    int idxDisabilita = TrovaIndiceColonna(table, "disabilit", "handicap", "sostegno", "104");
                    int idxDsa = TrovaIndiceColonna(table, "dsa", "disturb", "apprendimento");
                    int idxVotoMedia = TrovaIndiceColonna(table, "voto", "esame", "media");
                    int idxReligione = TrovaIndiceColonna(table, "religione"); // Dal file: "Religione"
                    int idxAssBase = TrovaIndiceColonna(table, "assistenzabase", "disabilitaassbase"); // Dal file: "DisabilitaAssBase"
                    int idxDataNascita = TrovaIndiceColonna(table, "datanascita", "data di nascita"); // Dal file: "DataNascita"                                                              // Per la data di arrivo, l'intestazione nel tuo file è lunghissima, usiamo delle parole chiave sicure:
                    int idxDataArrivo = TrovaIndiceColonna(table, "data di arrivo", "arrivo in italia", "arrivo");

                    int idxIndirizzoScelto = TrovaIndiceColonna(table, "Indirizzo/OpzioneCurricolare1ScuolaPrimaScelta");
                    int idxCodScuola = TrovaIndiceColonna(table, "meccanografico", "codice scuola", "codscuprovenienza");
                    int idxNomeScuola = TrovaIndiceColonna(table, "denominazione scuola", "scuola prov", "denominazione");
                    int idxComuneScuola = TrovaIndiceColonna(table, "comune scuola", "comune provenienza");

                    int idxTelGenitore1 = TrovaIndiceColonna(table, "TelefonoPrimoGen");
                    int idxNomeGenitore1 = TrovaIndiceColonna(table, "NomePrimoGen");
                    int idxCognomeGenitore1 = TrovaIndiceColonna(table, "CognomePrimoGen");
                    int idxMailGenitore1 = TrovaIndiceColonna(table, "EmailPrimoGen");

                    int idxPrefNomeCompagno = TrovaIndiceColonna(table, "Ulteriore dato: Scelta compagno/a", "Scelta compagno/a");


                    // 2. LEGGIAMO I DATI
                    for (int i = 1; i < table.Rows.Count; i++) // Salto riga 0 (intestazioni)
                    {
                        var row = table.Rows[i];
                        string nome = EstraiDato(row, idxNome);
                        string cognome = EstraiDato(row, idxCognome);

                        if (string.IsNullOrWhiteSpace(nome) && string.IsNullOrWhiteSpace(cognome)) continue;

                        bool sesso = EstraiDato(row, idxSesso) == "M";

                        // Riempimento delle 5 liste presenti dentro l'oggetto 'risultato'
                        risultato.Alunni.Add(new StudenteImportDto
                        {
                            Nome = nome,
                            Cognome = cognome,
                            Sesso = sesso,
                            CodiceFiscale = EstraiDato(row, idxCf),
                            Cittadinanza = EstraiDato(row, idxCittadinanza),
                            ComuneResidenza = EstraiDato(row, idxResidenza),
                            Disabilita = EstraiDato(row, idxDisabilita).ToLower().Contains("si"),
                            Dsa = EstraiDato(row, idxDsa).ToLower().Contains("si"),
                            Indirizzo = EstraiDato(row, idxIndirizzo),
                            VotoEsameTerzaMedia = EstraiDato(row, idxVotoMedia),
                            FaReligione = EstraiDato(row, idxReligione).ToLower() == "si" || EstraiDato(row, idxReligione).ToLower() == "sì",
                            DisabilitaAssistenzaBase = EstraiDato(row, idxAssBase).ToLower() == "si" || EstraiDato(row, idxAssBase).ToLower() == "sì",
                            DataDiNascita = EstraiDato(row, idxDataNascita),
                            DataArrivoInItalia = EstraiDato(row, idxDataArrivo)
                        });

                        risultato.Scelte.Add(new SceltaImportDto { 
                            IndirizzoScelto = EstraiDato(row, idxIndirizzoScelto),
                            CodiceFiscaleStudente = EstraiDato(row, idxCf),
                        });
                        risultato.Scuole.Add(new ScuolaProvImportDto { CodiceScuola = EstraiDato(row, idxCodScuola), DenominazioneScuola = EstraiDato(row, idxNomeScuola), ComuneScuola = EstraiDato(row, idxComuneScuola), CodiceFiscaleStudente = EstraiDato(row, idxCf), });
                        risultato.Genitori.Add(new GenitoreImportDTO { Numero = EstraiDato(row, idxTelGenitore1), Nome = EstraiDato(row, idxNomeGenitore1), Cognome = EstraiDato(row, idxCognomeGenitore1), Mail = EstraiDato(row, idxMailGenitore1), CodiceFiscale = EstraiDato(row, idxCf), });
                        risultato.PreferenzeCompagni.Add(new PreferenzaCompagnoImportDto { NomeStudenteScelto = EstraiDato(row, idxPrefNomeCompagno), CodiceFiscaleStudente = EstraiDato(row, idxCf), });
                    }
                }

                // Alla fine restituiamo il contenitore con tutte e 5 le liste popolate!
                return risultato;
            }
        }
        

        // --- FUNZIONI DI SUPPORTO ---
        private static int TrovaIndiceColonna(DataTable table, params string[] paroleChiave)
        {
            var rigaIntestazione = table.Rows[0];
            for (int c = 0; c < table.Columns.Count; c++)
            {
                string nomeColonna = rigaIntestazione[c]?.ToString().Trim().ToLower();
                if (string.IsNullOrWhiteSpace(nomeColonna)) continue;

                foreach (var parola in paroleChiave)
                {
                    if (nomeColonna.Contains(parola.ToLower())) return c;
                }
            }
            return -1;
        }

        private static string EstraiDato(DataRow riga, int indice)
        {
            if (indice >= 0 && indice < riga.ItemArray.Length)
            {
                return riga[indice]?.ToString().Trim() ?? "";
            }
            return "";
        }
    }
}
