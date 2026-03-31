
// PREREQUISITO: aggiungere il pacchetto NuGet al progetto prima di compilare
//   dotnet add package FuzzySharp


using System.Text.RegularExpressions;
using FuzzySharp;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using static BlaisePascal.ProjectWork._3E.Domain.Services.Categorie;

namespace BlaisePascal.ProjectWork._3E.Domain.Services
{
    // PreferenzaMatcher
    //
    // Responsabilità unica: dato il pool di studenti, analizza il campo
    // SceltaCompagno di ciascuno e produce una lista di RisultatoMatch
    // classificati nelle tre categorie.
    //
    // NON decide cosa fare con i match — quella responsabilità rimane in
    // DistribuzioneClassiService.PreferenzeCoppie(), che ora consumerà
    // solo i RisultatoMatch con Categoria == Certo.
    // I match Incerti vengono esposti all'interfaccia per revisione umana.
    public class PreferenzaMatcher
    {
        private readonly OpzioniMatcher _opzioni;

        public PreferenzaMatcher(OpzioniMatcher? opzioni = null)
        {
            _opzioni = opzioni ?? OpzioniMatcher.Default;
        }
        //  Entry point 
        // Processa tutti gli studenti del pool e restituisce la lista
        // completa dei risultati, una voce per ogni segmento di preferenza
        // estratto (uno studente può avere più preferenze → più voci).

        public List<RisultatoMatch> Analizza(List<Studente> pool)
        {
            var risultati = new List<RisultatoMatch>();

            foreach (var richiedente in pool)
            {
                if (richiedente.SceltaCompagno is not { } sc) continue;

                string testoOriginale = sc.Testo;
                if (string.IsNullOrWhiteSpace(testoOriginale)) continue;

                // Step 1 — Fix encoding (UTF-8 salvato come Latin-1)
                string testoFixato = FixEncoding(testoOriginale);

                // Step 2 — Normalizza e splitta in segmenti
                var segmenti = NormalizzaESplita(testoFixato);

                foreach (var segmento in segmenti)
                {
                    // Step 3 — Scarta valori non-preferenza
                    if (IsNonPreferenza(segmento))
                    {
                        risultati.Add(new RisultatoMatch
                        {
                            Richiedente          = richiedente,
                            TestoOriginale       = testoOriginale,
                            SegmentoNormalizzato = segmento,
                            CandidatoTrovato     = null,
                            Score                = 0,
                            Categoria            = CategoriaMatch.NessunMatch,
                            Messaggio            = $"Scartato: valore non-preferenza riconosciuto (\"{segmento}\")"
                        });
                        continue;
                    }

                    // Step 4 — Fuzzy match contro il pool
                    var (candidato, score) = TrovaMatchMigliore(segmento, pool, richiedente);

                    var categoria = ClassificaScore(score);
                    var messaggio = CostruisciMessaggio(categoria, segmento, candidato, score);

                    risultati.Add(new RisultatoMatch
                    {
                        Richiedente          = richiedente,
                        TestoOriginale       = testoOriginale,
                        SegmentoNormalizzato = segmento,
                        CandidatoTrovato     = candidato,
                        Score                = score,
                        Categoria            = categoria,
                        Messaggio            = messaggio
                    });
                }
            }

            return risultati;
        }

        //  Step 1 — Fix encoding 
        // Tenta di reinterpretare il testo come UTF-8 mal decodificato come Latin-1.
        // Esempio: "NiccolÃ²" → "Niccolò"

        private static string FixEncoding(string testo)
        {
            try
            {
                var latin1 = System.Text.Encoding.GetEncoding("iso-8859-1");
                var bytes  = latin1.GetBytes(testo);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return testo; // Se fallisce, usa il testo originale
            }
        }


        //  Step 2 — Normalizza e splitta 
        // Restituisce uno o più segmenti puliti pronti per il matching.

        private static List<string> NormalizzaESplita(string testo)
        {
            // Rimozione di rumore contestuale prima dello split
            string pulito = testo;

            // Date (es. "13/04/2010", "26/11/2010")
            pulito = Regex.Replace(pulito, @"\b\d{2}/\d{2}/\d{4}\b", " ");

            // Numeri di telefono (es. "3883969178")
            pulito = Regex.Replace(pulito, @"\b\d{7,}\b", " ");

            // Parole rumore — note descrittive aggiunte dai genitori
            var paroleRumore = new[]
            {
                "attuale compagno di classe", "attuale compagna di classe",
                "scuola di provenienza", "media frequentata",
                "amico", "amica", "cugino", "cugina", "fratello", "sorella",
                "perché", "perche", "nato a", "nata a", "residente",
                "via", "cell", "e-mail", "email"
            };
            foreach (var parola in paroleRumore)
                pulito = Regex.Replace(pulito, Regex.Escape(parola), " ",
                    RegexOptions.IgnoreCase);

            // Parentesi e contenuto tra parentesi (es. "(cugino)")
            pulito = Regex.Replace(pulito, @"\(.*?\)", " ");

            // Numerazione tipo "1)" "2)" "3)"
            pulito = Regex.Replace(pulito, @"\b\d+\)", " ");

            // Punteggiatura residua
            pulito = Regex.Replace(pulito, @"[.,;:!?]", " ");

            // Normalizza spazi multipli
            pulito = Regex.Replace(pulito, @"\s{2,}", " ").Trim();

            //  Split su separatori di preferenze multiple 
            // Ordine importante: prima i separatori più specifici
            var separatori = new[] { " e ", "  ", "\n" };
            var segmenti   = new List<string> { pulito };

            foreach (var sep in separatori)
            {
                var nuovi = new List<string>();
                foreach (var s in segmenti)
                    nuovi.AddRange(s.Split(new[] { sep },
                        StringSplitOptions.RemoveEmptyEntries));
                segmenti = nuovi;
            }

            // Filtra segmenti troppo corti per essere un nome (< 3 caratteri)
            return segmenti
                .Select(s => s.Trim())
                .Where(s => s.Length >= 3)
                .Distinct()
                .ToList();
        }


        //  Step 3 — Riconosci valori non-preferenza 

        private static readonly HashSet<string> ValoriDaScartare =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "nessuno", "non", "-", "nessuno in particolare"
            };

        private static bool IsNonPreferenza(string segmento)
        {
            if (ValoriDaScartare.Contains(segmento))
                return true;

            // Solo cifre (numeri di telefono residui)
            if (Regex.IsMatch(segmento, @"^\d+$"))
                return true;

            // Solo una parola di 3 caratteri o meno → troppo ambiguo
            if (!segmento.Contains(' ') && segmento.Length <= 3)
                return true;

            return false;
        }


        //  Step 4 — Fuzzy match 
        // Usa FuzzySharp.Fuzz.TokenSortRatio che:
        //   • ignora il casing
        //   • gestisce l'inversione nome/cognome
        //   • è tollerante a piccoli errori ortografici
        //
        // Confronta il segmento contro "{Nome} {Cognome}" di ogni studente
        // del pool, escludendo il richiedente stesso.

        private (Studente? candidato, int score) TrovaMatchMigliore(
            string segmento, List<Studente> pool, Studente richiedente)
        {
            Studente? migliore = null;
            int       maxScore = 0;

            foreach (var candidato in pool)
            {
                if (candidato.Id == richiedente.Id) continue;

                string nomeCompleto = $"{candidato.Nome} {candidato.Cognome}";
                int    score        = Fuzz.TokenSortRatio(segmento, nomeCompleto);

                if (score > maxScore)
                {
                    maxScore = score;
                    migliore = candidato;
                }
            }

            return (migliore, maxScore);
        }


        //  Classificazione score → categoria 

        private CategoriaMatch ClassificaScore(int score)
        {
            if (score >= _opzioni.SogliaMatchCerto)   return CategoriaMatch.Certo;
            if (score >= _opzioni.SogliaMatchIncerto) return CategoriaMatch.Incerto;
            return CategoriaMatch.NessunMatch;
        }


        //  Costruzione messaggio leggibile per log / UI 

        private static string CostruisciMessaggio(
            CategoriaMatch categoria, string segmento,
            Studente? candidato, int score)
        {
            return categoria switch
            {
                CategoriaMatch.Certo =>
                    $"Match certo (score {score}): \"{segmento}\" → " +
                    $"{candidato!.Nome} {candidato.Cognome}",

                CategoriaMatch.Incerto =>
                    $"Match incerto (score {score}): \"{segmento}\" → " +
                    $"{candidato!.Nome} {candidato.Cognome} — richiede revisione",

                CategoriaMatch.NessunMatch =>
                    $"Nessun match (score {score}): \"{segmento}\" — scartato",

                _ => string.Empty
            };
        }
    }
}
