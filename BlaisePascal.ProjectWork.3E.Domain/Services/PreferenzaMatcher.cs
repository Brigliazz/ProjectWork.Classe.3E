
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

                // Step 1 — Normalizza e splitta in segmenti
                var segmenti = NormalizzaESplita(testoOriginale);

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


        /// <summary>
        /// Normalizza il testo grezzo di preferenza ed estrae i singoli segmenti-nome.
        /// Pipeline di pulizia applicata in ordine:
        ///   2a. Troncamento a parole sentinella (nato il, se possibile, @, ecc.)
        ///   2b. Rimozione contenuto tra parentesi (), [], {}, &lt;&gt;
        ///   2c. Rimozione estesa di parole/frasi rumore (50+ voci, case-insensitive)
        ///   2d. Rimozione date (dd/mm/yyyy, dd-mm-yyyy, dd.mm.yyyy, dd/mm/yy, anni isolati)
        ///   2e. Rimozione numeri di telefono (7+ cifre consecutive)
        ///   2f. Rimozione prefissi ordinali (1), 2., a), ecc.)
        ///   2g. Rimozione punteggiatura residua (preserva trattino tra lettere)
        ///   2h. Normalizzazione apostrofi (varianti → ')
        ///   2i. Normalizzazione spazi bianchi
        /// Dopo la pulizia, lo split avviene su: \n, " e ", " - ", " / ", virgola, punto e virgola, spazi multipli.
        /// Post-filtro: scarta segmenti &lt;3 car., puramente numerici, parola singola ≤3 car., deduplicazione.
        /// </summary>
        private static List<string> NormalizzaESplita(string testo)
        {
            string pulito = testo;

            // ── 2a. Troncamento a parole sentinella ──────────────────────────
            string[] sentinelle =
            {
                "nato il", "nata il", "nato a", "nata a",
                "se possibile", "tutrice", "tutore",
                "sono unica", "sono il", "sono la",
                "genitore di", "madre di", "padre di",
                "residente a", "residente in",
                "scuola media", "scuola di provenienza",
                "classe", "sezione",
                "tel", "cell", "email", "e-mail", "@",
                "nota:", "note:", "ps:", "p.s."
            };
            foreach (var s in sentinelle)
            {
                int idx = pulito.IndexOf(s, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    pulito = pulito[..idx];
                }
            }

            // ── 2b. Rimozione contenuto tra parentesi ────────────────────────
            pulito = Regex.Replace(pulito, @"\(.*?\)", " ");
            pulito = Regex.Replace(pulito, @"\[.*?\]", " ");
            pulito = Regex.Replace(pulito, @"\{.*?\}", " ");
            pulito = Regex.Replace(pulito, @"<.*?>",   " ");

            // ── 2c. Rimozione parole/frasi rumore (più lunghe prima) ─────────
            string[] paroleRumore =
            {
                // Frasi lunghe prima
                "attuale compagno di classe", "attuale compagna di classe",
                "compagno di classe", "compagna di classe",
                "scuola di provenienza", "media frequentata", "scuola media",
                "mio figlio vorrebbe stare con", "vorrebbe stare con",
                "si richiede di stare con", "si chiede di stare con",
                "preferisce stare con", "chiede di stare con",
                "amico del cuore", "migliore amico", "migliore amica",
                // Parole singole / frasi brevi
                "amico", "amica", "amici", "amiche",
                "cugino", "cugina", "fratello", "sorella", "fratellino", "sorellina",
                "compagno", "compagna",
                "perché", "perche", "perchè",
                "nato a", "nata a", "nato il", "nata il",
                "residente", "via", "viale", "piazza", "corso",
                "cell", "cellulare", "telefono", "tel",
                "e-mail", "email", "mail",
                "madre", "padre", "mamma", "papà", "papa", "genitore",
                "tutrice", "tutore", "legale",
                "del", "della", "dello", "dei", "delle",
                "con", "insieme a", "insieme con",
                "si chiama", "di nome",
                "nr", "n°", "numero"
            };
            foreach (var parola in paroleRumore)
            {
                // Whole-word / whole-phrase match (word-boundary)
                string pattern = @"(?<!\w)" + Regex.Escape(parola) + @"(?!\w)";
                pulito = Regex.Replace(pulito, pattern, " ", RegexOptions.IgnoreCase);
            }

            // ── 2d. Rimozione date ──────────────────────────────────────────
            // dd/mm/yyyy, dd-mm-yyyy, dd.mm.yyyy
            pulito = Regex.Replace(pulito, @"\b\d{1,2}[/\-\.]\d{1,2}[/\-\.]\d{4}\b", " ");
            // dd/mm/yy
            pulito = Regex.Replace(pulito, @"\b\d{1,2}[/\-\.]\d{1,2}[/\-\.]\d{2}\b", " ");
            // Anno isolato (4 cifre tra 1900 e 2099)
            pulito = Regex.Replace(pulito, @"\b(19|20)\d{2}\b", " ");

            // ── 2e. Rimozione numeri di telefono (7+ cifre) ──────────────────
            pulito = Regex.Replace(pulito, @"\b\d{7,}\b", " ");

            // ── 2f. Rimozione prefissi ordinali ──────────────────────────────
            // Patterns: "1)", "2.", "a)", "b)" all'inizio di un segmento
            pulito = Regex.Replace(pulito, @"(?:^|\n)\s*(?:\d+|[a-zA-Z])[.)]\s*", " ");

            // ══ Split su separatori di preferenze multiple ═══════════════════
            // Ordine: dal più specifico al meno specifico
            string[][] separatoriGruppi =
            {
                new[] { "\n", "\r\n" },
                new[] { " e " },
                new[] { " - " },
                new[] { " / " },
                new[] { "," },
                new[] { ";" }
            };

            var segmentiPreliminari = new List<string> { pulito };

            foreach (var gruppo in separatoriGruppi)
            {
                var nuovi = new List<string>();
                foreach (var seg in segmentiPreliminari)
                    nuovi.AddRange(seg.Split(gruppo, StringSplitOptions.RemoveEmptyEntries));
                segmentiPreliminari = nuovi;
            }

            var finali = new List<string>();

            foreach (var elaborando in segmentiPreliminari)
            {
                string seg = elaborando;

                // ── 2g. Rimozione punteggiatura residua ──────────────────────────
                // Rimuovi tutto tranne lettere, cifre, spazi, trattino e apostrofo
                seg = Regex.Replace(seg, @"[.,;:!?/\\|_#^~`]", " ");

                // ── 2h. Normalizzazione apostrofi ────────────────────────────────
                // Normalizza varianti → apostrofo standard
                seg = Regex.Replace(seg, @"[\u2018\u2019\u0060\u00B4]", "'");
                // Rimuovi apostrofo NON tra due lettere
                seg = Regex.Replace(seg, @"(?<![a-zA-ZÀ-ÿ])'|'(?![a-zA-ZÀ-ÿ])", " ");

                // ── 2i. Normalizzazione spazi bianchi ────────────────────────────
                seg = Regex.Replace(seg, @"[\t\r\n]+", " ");
                seg = Regex.Replace(seg, @"\s{2,}", " ").Trim();

                if (seg.Length == 0) continue;

                // Split su 2+ spazi consecutivi (nomi separati solo da spazi extra)
                var parti = Regex.Split(seg, @"\s{2,}");
                finali.AddRange(parti);
            }

            // ══ Post-filtro ═════════════════════════════════════════════════
            return finali
                .Select(s => s.Trim())
                .Where(s => s.Length >= 3)                               // < 3 char
                .Where(s => !Regex.IsMatch(s, @"^\d+$"))                 // solo cifre
                .Where(s => s.Contains(' ') || s.Length > 3)             // parola singola ≤ 3 char
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        //  Step 3 — Riconosci valori non-preferenza 

        private static readonly HashSet<string> ValoriDaScartare =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Risposte negative
                "nessuno", "nessuna", "non", "niente", "-", "/", ".",
                "nessuno in particolare", "nessuna preferenza",
                "non ho preferenze", "non saprei", "indifferente",
                "non so", "boh", "n/a", "na", "nd", "//",
                // Parole rumore sopravvissute alla pulizia
                "madre", "padre", "mamma", "papa", "papà",
                "genitore", "tutrice", "tutore",
                "amico", "amica", "compagno", "compagna",
                "fratello", "sorella", "cugino", "cugina",
                // Punteggiatura residua e risposte minime
                "x", "ok", "si", "no"
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
                int    score        = Fuzz.TokenSortRatio(segmento.ToLowerInvariant(), nomeCompleto.ToLowerInvariant());

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
