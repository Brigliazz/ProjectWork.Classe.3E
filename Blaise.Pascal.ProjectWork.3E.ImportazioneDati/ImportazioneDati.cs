using Blaise.Pascal.ProjectWork._3E.ImportazioneDati;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;  

namespace Importazione_Dati_Excel
{
    class ImportazioneDati
    {
        public string percorsoFile;
        public void EstrapolaDati()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            List<Alunno> listaAlunni = new List<Alunno>();
            List<Scelta> listaScelte = new List<Scelta>();
            List<ScuolaProv> listaScuole = new List<ScuolaProv>();
            List<Genitore> listaGenitori = new List<Genitore>();
            List<PreferenzaCompagno> listaPreferenzeCompagni = new List<PreferenzaCompagno>();

            /*
            aggiungi nella importazione dati i seguenti dati dal file excell: 
            QUESTI SONO PER STUDENTE
            voto esame terza media 
            fa religione 
            disabilita assistenza base 
            data arrivo in italia 
            data di nascita 
            
            QUESTI SONO PER PREFERENZE COMPAGNO
            nome compagno
            cognome compagno
            
            QUESTI SONO PER SCUOLA PROV
            comune scuola
            
            GENITORE  OK
            IndirizzoScelto OK


            in piu' bisogna implementare la clean architecture
            
            
            */
            // per il percorso file bisogna fare in modo che dalla WPF (sfoglia file) venga messo il percorso nella varibaile percorsoFIle

            if (!File.Exists(percorsoFile))
            {
                Console.WriteLine($"ERRORE: File '{percorsoFile}' non trovato.");
                Console.ReadLine();
                return;
            }

            using (var stream = File.Open(percorsoFile, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet();
                var table = result.Tables[0];

                // 1. TROVIAMO GLI INDICI DELLE COLONNE TRAMITE PAROLE CHIAVE
                int idxNome = TrovaIndiceColonna(table, "nome alunno", "nome");
                int idxCognome = TrovaIndiceColonna(table, "cognome alunno", "cognome");
                int idxSesso = TrovaIndiceColonna(table, "sesso");
                int idxCf = TrovaIndiceColonna(table, "fiscale", "cf");
                int idxCittadinanza = TrovaIndiceColonna(table, "cittadinanza");
                int idxResidenza = TrovaIndiceColonna(table, "comune", "residenza");
                int idxIndirizzo = TrovaIndiceColonna(table, "indirizzo alunno", "IndirizzoDomicilio", "via");
                int idxDisabilita = TrovaIndiceColonna(table, "disabilit", "handicap", "sostegno", "104");
                int idxDsa = TrovaIndiceColonna(table, "dsa", "disturb", "apprendimento");

                int idxIndirizzoScelto = TrovaIndiceColonna(table, "Indirizzo/OpzioneCurricolare1ScuolaPrimaScelta");
                // MODIFICA EFFETTUATA QUI: Aggiunte le parole chiave "codscuprovenienza" e "denominazione"
                int idxCodScuola = TrovaIndiceColonna(table, "meccanografico", "codice scuola", "codscuprovenienza");
                int idxNomeScuola = TrovaIndiceColonna(table, "denominazione scuola", "scuola prov", "denominazione");

                int idxTelGenitore2 = TrovaIndiceColonna(table, "TelefonoSecondoGen");
                int idxNomeGenitore2 = TrovaIndiceColonna(table, "NomeSecondoGen");
                int idxCognomeGenitore2 = TrovaIndiceColonna(table, "CognomeSecondoGen");
                int idxMailGenitore2 = TrovaIndiceColonna(table, "EmailSecondoGen");

                int idxPrefCompagno = TrovaIndiceColonna(table, "Ulteriore dato: Scelta compagno/a", "Scelta compagno/a");


                // 2. LEGGIAMO I DATI
                for (int i = 1; i < table.Rows.Count; i++) // Salto riga 0 (intestazioni)
                {
                    var row = table.Rows[i];
                    bool sesso;
                    string nome = EstraiDato(row, idxNome);
                    string cognome = EstraiDato(row, idxCognome);

                    // Se non c'è il nome o il cognome, probabilmente è una riga vuota
                    if (string.IsNullOrWhiteSpace(nome) && string.IsNullOrWhiteSpace(cognome)) continue;

                    if (EstraiDato(row, idxSesso) == "M")
                    {
                        sesso = true;
                    }
                    else
                    {
                        sesso = false;
                    }
                    listaAlunni.Add(new Alunno
                    {
                        Nome = nome,
                        Cognome = cognome,
                        Sesso = sesso,
                        CodiceFiscale = EstraiDato(row, idxCf),
                        Cittadinanza = EstraiDato(row, idxCittadinanza),
                        ComuneResidenza = EstraiDato(row, idxResidenza),
                        Disabilita = EstraiDato(row, idxDisabilita).ToLower().Contains("si"),
                        Dsa = EstraiDato(row, idxDsa).ToLower().Contains("si"),
                        Indirizzo = EstraiDato(row, idxIndirizzo)
                    });

                    listaScelte.Add(new Scelta { IndirizzoScelto = EstraiDato(row, idxIndirizzoScelto) });
                    listaScuole.Add(new ScuolaProv { CodiceScuola = EstraiDato(row, idxCodScuola), DenominazioneScuola = EstraiDato(row, idxNomeScuola) });
                    listaGenitori.Add(new Genitore { Numero = EstraiDato(row, idxTelGenitore2), Nome = EstraiDato(row, idxNomeGenitore2), Mail = EstraiDato(row, idxMailGenitore2) });
                    listaPreferenzeCompagni.Add(new PreferenzaCompagno { NomeStudenteScelto = EstraiDato(row, idxPrefCompagno) });
                }
            }

            // --- STAMPA DI VERIFICA ---
            /*
            Console.WriteLine($"Estrazione completata! Trovati {listaAlunni.Count} record.\n");

            for (int i = 0; i < listaAlunni.Count; i++)
            {

                var a = listaAlunni[i];
                var sc = listaScelte[i];
                var sp = listaScuole[i];
                var g = listaGenitori[i];
                string sesso;
                Console.WriteLine($"=== RECORD {i + 1} ===");
                if (a.Sesso == true)
                {
                    sesso = "Maschio";
                }
                else
                {
                    sesso = "Femmina";
                }
                // Alunno
                Console.WriteLine($"[ALUNNO]      Nome: {VerificaDato(a.Nome)} {VerificaDato(a.Cognome)}");
                Console.WriteLine($"              Sesso: {sesso} | CF: {VerificaDato(a.CodiceFiscale)}");
                Console.WriteLine($"              Cittadinanza: {VerificaDato(a.Cittadinanza)}");
                Console.WriteLine($"              Residenza: {VerificaDato(a.ComuneResidenza)} | Indirizzo: {VerificaDato(a.Indirizzo)}");
                Console.WriteLine($"              Disabilità: {(a.Disabilita ? "SÌ" : "No")} | DSA: {(a.Dsa ? "SÌ" : "No")}");

                // Scelta e Scuola
                Console.WriteLine($"[SCELTA]      Indirizzo: {VerificaDato(sc.IndirizzoScelto)}");
                Console.WriteLine($"[SCUOLA PROV] Codice: {VerificaDato(sp.CodiceScuola)} | Nome: {VerificaDato(sp.DenominazioneScuola)}");

                // Genitori (Caso specifico telefono)
                Console.WriteLine($"[GENITORI]    Referente: {VerificaDato(g.Nome)}");
                Console.WriteLine($"              Telefono: {VerificaDato(g.Numero, "Nessun numero fornito")}");
                Console.WriteLine($"              Mail: {VerificaDato(g.Mail, "Email mancante")}");

                Console.WriteLine("--------------------------------------------------\n"); /
            }
            */

            // Aggiungi questa funzione in fondo alla classe Program insieme alle altre funzioni di supporto
            static string VerificaDato(string valore, string segnaposto = "N/D")
            {
                return string.IsNullOrWhiteSpace(valore) ? segnaposto : valore;
            }



            // --- FUNZIONI DI SUPPORTO ---

            // Cerca la colonna controllando se l'intestazione contiene una delle parole chiave
            static int TrovaIndiceColonna(DataTable table, params string[] paroleChiave)
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
                return -1; // Ritorna -1 se non trova nulla
            }

            // Estrae il dato in modo sicuro, evitando errori se la colonna non esiste
            static string EstraiDato(DataRow riga, int indice)
            {
                if (indice >= 0 && indice < riga.ItemArray.Length)
                {
                    return riga[indice]?.ToString().Trim() ?? "";
                }
                return "";
            }
        }
    }
}

