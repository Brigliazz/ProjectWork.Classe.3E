// PREREQUISITO: aggiungere il pacchetto NuGet al progetto prima di compilare
//   dotnet add package Google.OrTools


using Google.OrTools.Sat;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Exceptions;
using BlaisePascal.ProjectWork._3E.Domain.Repositories;
using BlaisePascal.ProjectWork._3E.Domain.Enums;
using static BlaisePascal.ProjectWork._3E.Domain.Services.Categorie;

namespace BlaisePascal.ProjectWork._3E.Domain.Services
{
    public class DistribuzioneClassiService
    {
        private readonly IStudenteRepository _studenteRepository;
        private readonly IClasseRepository   _classeRepository;
        private readonly PreferenzaMatcher   _preferenzaMatcher;

        public IReadOnlyList<RisultatoMatch> MatchIncerti =>
            _matchIncerti.AsReadOnly();

        private readonly List<RisultatoMatch> _matchIncerti = new();

        public DistribuzioneClassiService(
            IStudenteRepository studenteRepository,
            IClasseRepository classeRepository,
            OpzioniMatcher? opzioniMatcher = null)
        {
            _studenteRepository  = studenteRepository;
            _classeRepository    = classeRepository;
            _preferenzaMatcher   = new PreferenzaMatcher(opzioniMatcher);
        }

        //  Entry point pubblico
        

        public async Task<List<List<Studente>>> DistribuisciAsync(OpzioniDistribuzione? opzioni = null)
        {
            opzioni ??= OpzioniDistribuzione.Default;

            var studenti = await _studenteRepository.GetNonAssegnatiAsync();
            var classi   = await _classeRepository.GetAllAsync();

            if (classi.Count == 0)
                classi = await GeneraClassiAsync(opzioni);

            if (studenti.Count == 0)
                return new List<List<Studente>>();

            //  PreferenzaMatcher: analisi fuzzy delle preferenze
            var coppiePreferenze = new List<(int IdxI, int IdxJ)>();

            if (opzioni.UsaPreferenze)
            {
                _matchIncerti.Clear();
                var risultatiMatch = _preferenzaMatcher.Analizza(studenti);

                // Log dei NessunMatch (utile per debug)
                foreach (var r in risultatiMatch.Where(r => r.Categoria == CategoriaMatch.NessunMatch))
                    Console.WriteLine($"[PreferenzaMatcher] {r.Messaggio}");

                // Gli incerti vengono salvati per la UI
                _matchIncerti.AddRange(risultatiMatch.Where(r => r.Categoria == CategoriaMatch.Incerto));

                // Solo i certi entrano nel modello OR-Tools
                coppiePreferenze = risultatiMatch
                    .Where(r => r.Categoria == CategoriaMatch.Certo && r.CandidatoTrovato != null)
                    .Select(r => (IdxI: studenti.IndexOf(r.Richiedente),IdxJ: studenti.IndexOf(r.CandidatoTrovato!)))
                    .Where(p => p.IdxI >= 0 && p.IdxJ >= 0)
                    .ToList();
            }

            // Risolvi il problema di assegnazione con OR-Tools CP-SAT
            var assegnazioni = RisolviConOrTools(studenti, classi, opzioni, coppiePreferenze);

            // Applica la soluzione agli oggetti di dominio attraverso i loro metodi
            ApplicaSoluzione(assegnazioni, studenti, classi, opzioni);

            await _studenteRepository.SaveChangesAsync();
            await _classeRepository.SaveChangesAsync();

            // Costruisce e ritorna la matrice degli studenti per classe
            var tutti = await _studenteRepository.GetAllAsync();
            return classi
                .OrderBy(c => c.Sezione.Valore)
                .Select(c => tutti.Where(s => s.ClasseId == c.Id).ToList())
                .ToList();
        }

        // Variante che restituisce anche i metadati della classe (sezione + indirizzo)
        public async Task<List<(Aggregates.ClassePrima.ClassePrima Classe, List<Studente> Studenti)>> DistribuisciConMetadatiAsync(OpzioniDistribuzione? opzioni = null)
        {
            opzioni ??= OpzioniDistribuzione.Default;

            var studenti = await _studenteRepository.GetNonAssegnatiAsync();
            var classi   = await _classeRepository.GetAllAsync();

            if (classi.Count == 0)
                classi = await GeneraClassiAsync(opzioni);

            if (studenti.Count == 0)
                return new List<(Aggregates.ClassePrima.ClassePrima, List<Studente>)>();

            var coppiePreferenze = new List<(int IdxI, int IdxJ)>();
            if (opzioni.UsaPreferenze)
            {
                _matchIncerti.Clear();
                var risultatiMatch = _preferenzaMatcher.Analizza(studenti);
                _matchIncerti.AddRange(risultatiMatch.Where(r => r.Categoria == CategoriaMatch.Incerto));
                coppiePreferenze = risultatiMatch
                    .Where(r => r.Categoria == CategoriaMatch.Certo && r.CandidatoTrovato != null)
                    .Select(r => (IdxI: studenti.IndexOf(r.Richiedente), IdxJ: studenti.IndexOf(r.CandidatoTrovato!)))
                    .Where(p => p.IdxI >= 0 && p.IdxJ >= 0)
                    .ToList();
            }

            var assegnazioni = RisolviConOrTools(studenti, classi, opzioni, coppiePreferenze);
            ApplicaSoluzione(assegnazioni, studenti, classi, opzioni);

            await _studenteRepository.SaveChangesAsync();
            await _classeRepository.SaveChangesAsync();

            var tutti = await _studenteRepository.GetAllAsync();
            return classi
                .OrderBy(c => c.Sezione.Valore)
                .Select(c => (c, tutti.Where(s => s.ClasseId == c.Id).ToList()))
                .ToList();
        }

        //  Generazione classi (logica invariata rispetto all'originale)
        

        private async Task<List<ClassePrima>> GeneraClassiAsync(OpzioniDistribuzione opzioni)
        {
            if (opzioni.SezioniPerIndirizzo == null || opzioni.SezioniPerIndirizzo.Count == 0)
                throw new DomainException("Non ci sono classi disponibili e non ne è stata richiesta la generazione.");

            var mapSezioni = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Automazione", new[] { "A", "B", "C", "D" } },
                { "Informatica", new[] { "E", "F", "G", "H", "I", "L", "M", "N", "O" } },
                { "Bio",         new[] { "Bio" } }
            };

            var classi = new List<ClassePrima>();

            foreach (var kvp in opzioni.SezioniPerIndirizzo)
            {
                if (kvp.Value <= 0) continue;

                if (!mapSezioni.TryGetValue(kvp.Key, out var ammesse))
                    throw new DomainException($"Indirizzo sconosciuto: {kvp.Key}");

                if (kvp.Value > ammesse.Length)
                    throw new DomainException($"Richieste {kvp.Value} sezioni per {kvp.Key}, disponibili max {ammesse.Length}.");

                var indirizzo = IndirizzoScolastico.Crea(kvp.Key);
                for (int i = 0; i < kvp.Value; i++)
                {
                    var nuova = ClassePrima.Crea(Sezione.Crea(ammesse[i]), indirizzo);
                    await _classeRepository.AddAsync(nuova);
                    classi.Add(nuova);
                }
            }

            if (classi.Count == 0)
                throw new DomainException("Impossibile generare le classi.");

            return classi;
        }

        //  OR-Tools CP-SAT — costruzione e risoluzione del modello
        //
        // STRUTTURA DEL MODELLO:
        //   Variabili : x[i,j] = BoolVar, vale 1 se lo studente i va nella classe j
        //   Vincoli hard (P1):
        //     • Ogni studente in esattamente una classe
        //     • Max 1 disabile per classe
        //     • Capienza condizionale (20 se disabile, 27 altrimenti)
        //     • Max 30% stranieri per classe (approssimato sulla dimensione media)
        //   Funzione obiettivo (P2/P3):
        //     • Minimizza lo sbilanciamento per: ragazze, stranieri, DSA, IRC, eccellenze
        //     • Penalizza l'isolamento di ragazze (< 2 per classe)
        //     • Penalizza coppie di preferenza (SceltaCompagno) non rispettate
        //     • Minimizza lo sbilanciamento per scuola di provenienza
        //
        // Ritorna: dizionario studenteId → classeId
        

        private static Dictionary<Guid, Guid> RisolviConOrTools(List<Studente> studenti, List<ClassePrima> classi, OpzioniDistribuzione opzioni,List<(int IdxI, int IdxJ)> coppiePreferenze)
        {
            // Inizializza il modello CP-SAT (Constraint Programming - Satisfiability)
            // Questo modello conterrà tutte le variabili, i vincoli e la funzione obiettivo.
            var model = new CpModel();
            int n = studenti.Count;
            int m = classi.Count;

            // Matrice delle variabili di decisione binaria
            // x[i, j] è una variabile booleana che vale 1 se lo studente i-esimo 
            // è assegnato alla classe j-esima, 0 altrimenti.
            var x = new BoolVar[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    x[i, j] = model.NewBoolVar($"x_{i}_{j}");


            // VINCOLO FISSO: Ogni studente deve appartenere a una e una sola classe.
            // La somma orizzontale delle x[i, j] per ogni i deve essere esattamente 1.
            for (int i = 0; i < n; i++)
                model.AddExactlyOne(
                    Enumerable.Range(0, m).Select(j => (ILiteral)x[i, j]).ToArray());


            // Identifica gli indici degli studenti che appartengono a categorie specifiche
            // Queste liste verranno usate per definire i vincoli di bilanciamento.
            var disabiliIdx  = IndiciStudenti(studenti, s => s.ProfiloBES.HasDisabilita);
            var ragazzeIdx   = IndiciStudenti(studenti, s => s.Sesso == Sesso.Femmina);
            var stranieriIdx = IndiciStudenti(studenti, s => s.IsStraniero);
            var dsaIdx       = IndiciStudenti(studenti, s => s.ProfiloBES.HasDSA);
            var ircIdx       = IndiciStudenti(studenti, s => s.FaReligione);
            var eccIdx       = IndiciStudenti(studenti, s => s.IsEccellenza);


            // VINCOLI SULLA CAPIENZA E DISABILITÀ (Parametri P1):
            // 1. Ogni classe può avere al massimo 1 disabile (per ottimizzare l'integrazione).
            // 2. Se una classe ha un disabile, la sua capienza massima scende a 20 studenti.
            // 3. Altrimenti, la capienza standard è 27 (a meno di sforo permesso dalle opzioni).
            
            int delta = opzioni.LimiteStandard - opzioni.LimiteDisabili; // Solitamente 27 - 20 = 7

            for (int j = 0; j < m; j++)
            {
                if (disabiliIdx.Count > 0)
                {
                    var disInClass = disabiliIdx.Select(i => (IntVar)x[i, j]).ToArray();

                    // Primo vincolo: max 1 disabile per classe
                    model.Add(LinearExpr.Sum(disInClass) <= 1);

                    if (!opzioni.ConsentiSforo)
                    {
                        // dj è 1 se la classe j contiene almeno un disabile
                        var dj = model.NewBoolVar($"d_{j}");

                        // Se un qualunque disabile i è nella classe j, allora dj deve valere 1
                        foreach (var i in disabiliIdx)
                            model.AddImplication(x[i, j], dj);

                        // Viceversa, se dj=1 deve esserci almeno un disabile
                        model.Add(LinearExpr.Sum(disInClass) >= 1).OnlyEnforceIf(dj);

                        // Capienza logica: Sum(studenti_in_classe) + 7 * dj <= 27
                        // Se dj=0 (no disabili): Sum <= 27
                        // Se dj=1 (1 disabile): Sum + 7 <= 27  =>  Sum <= 20
                        var allVars = Enumerable.Range(0, n)
                            .Select(i => (IntVar)x[i, j])
                            .Append((IntVar)dj)
                            .ToArray();
                        var weights = Enumerable.Repeat(1L, n)
                            .Append((long)delta)
                            .ToArray();
                        model.Add(LinearExpr.WeightedSum(allVars, weights) <= opzioni.LimiteStandard);
                    }
                }
                else if (!opzioni.ConsentiSforo)
                {
                    // Caso semplice senza disabili: capienza fissa per tutte le classi
                    model.Add(LinearExpr.Sum(Enumerable.Range(0, n).Select(i => (IntVar)x[i, j]).ToArray()) <= opzioni.LimiteStandard);
                }
            }


            // VINCOLO SULLA PERCENTUALE DI STRANIERI:
            // Per regolamento, gli stranieri non dovrebbero superare il 30% degli alunni per classe.
            // Poiché CP-SAT lavora con interi, calcoliamo una soglia numerica basata sulla media attesa.
            if (stranieriIdx.Count > 0 && !opzioni.ConsentiSforo)
            {
                int attesoPerClasse = (int)Math.Ceiling((double)n / m);
                int maxStranieri    = Math.Max(1, (int)Math.Floor(0.30 * attesoPerClasse));

                for (int j = 0; j < m; j++)
                    model.Add(LinearExpr.Sum(stranieriIdx.Select(i => (IntVar)x[i, j]).ToArray()) <= maxStranieri);
            }


            // FUNZIONE OBIETTIVO (Ottimizzazione dei criteri soft):
            // Qui definiamo cosa rende una distribuzione "di qualità".
            // Ogni penalità pesa sulla funzione obiettivo: il solver cercherà di minimizzare la somma totale dei pesi.
            
            var objVars    = new List<IntVar>();
            var objWeights = new List<long>();
            
            // Metodo helper per aggiungere un parametro da minimizzare alla funzione obiettivo
            void AddPenalty(IntVar v, int w) { objVars.Add(v); objWeights.Add(w); }

            // BILANCIAMENTO CATEGORIE (Minimizza la differenza tra la classe con più elementi e quella con meno):
            // Più alto è il numero di ragazze, stranieri, DSA, ecc., più importante è bilanciarli equamente.
            if (ragazzeIdx.Count   > 0) AddPenalty(PenalitaBilancio(model, x, n, m, ragazzeIdx,   "girl"), 100);
            if (stranieriIdx.Count > 0) AddPenalty(PenalitaBilancio(model, x, n, m, stranieriIdx, "str"),   80);
            if (dsaIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, dsaIdx,       "dsa"),   80);
            if (ircIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, ircIdx,       "irc"),   30);
            if (eccIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, eccIdx,       "ecc"),   20);


            // CRITERIO "ANTI-ISOLAMENTO" RAGAZZE:
            // Evitiamo che una ragazza sia sola o con una sola compagna in una classe.
            // Se in una classe ci sono meno di 2 ragazze, scatta una penalità molto alta (500).
            if (ragazzeIdx.Count >= 2)
            {
                for (int j = 0; j < m; j++)
                {
                    // Conta le ragazze nella classe j
                    var gc = model.NewIntVar(0, ragazzeIdx.Count, $"gc_{j}");
                    model.Add(gc == LinearExpr.Sum(
                        ragazzeIdx.Select(i => (IntVar)x[i, j]).ToArray()));

                    var atLeast2    = model.NewBoolVar($"al2_{j}");
                    var notAtLeast2 = model.NewBoolVar($"nal2_{j}");

                    // Se gc >= 2, atLeast2 = 1. Se gc <= 1, atLeast2 = 0 (implica notAtLeast2 = 1).
                    model.Add(gc >= 2).OnlyEnforceIf(atLeast2);
                    model.Add(gc <= 1).OnlyEnforceIf(atLeast2.Not());
                    model.AddBoolXor(new ILiteral[] { atLeast2, notAtLeast2 });

                    AddPenalty((IntVar)notAtLeast2, 500);
                }
            }


            // GESTIONE PREFERENZE (SCELTA COMPAGNO):
            // Per ogni coppia di studenti che hanno espresso una preferenza reciproca certa:
            // Il solver cerca di metterli nella stessa classe. Se non ci riesce, aggiunge una penalità (50).
            foreach (var (pi, pj) in coppiePreferenze)
            {
                var togetherK = new BoolVar[m];
                for (int k = 0; k < m; k++)
                {
                    togetherK[k] = model.NewBoolVar($"tog_{pi}_{pj}_{k}");
                    // togetherK[k] vale 1 solo se sia i che j sono nella classe k
                    model.AddBoolAnd(new ILiteral[] { x[pi, k], x[pj, k] })
                         .OnlyEnforceIf(togetherK[k]);
                    model.AddBoolOr(new ILiteral[] { x[pi, k].Not(), x[pj, k].Not() })
                         .OnlyEnforceIf(togetherK[k].Not());
                }

                // inSameClass vale 1 se i e j sono insieme in UNA QUALUNQUE delle classi m
                var inSameClass = model.NewBoolVar($"sc_{pi}_{pj}");
                model.AddBoolOr(togetherK.Cast<ILiteral>().ToArray())
                     .OnlyEnforceIf(inSameClass);
                model.AddBoolAnd(togetherK.Select(t => (ILiteral)t.Not()).ToArray())
                     .OnlyEnforceIf(inSameClass.Not());

                var notTogether = model.NewBoolVar($"nt_{pi}_{pj}");
                model.AddBoolXor(new ILiteral[] { inSameClass, notTogether });
                AddPenalty((IntVar)notTogether, 50);
            }


            // GESTIONE INDIRIZZO PREFERITO:
            // Per ogni studente che ha un IndirizzoPreferito espresso, 
            // penalizziamo fortemente (+1000) l'assegnazione a una classe con indirizzo diverso.
            for (int i = 0; i < n; i++)
            {
                if (studenti[i].IndirizzoPreferito == null) continue;

                for (int j = 0; j < m; j++)
                {
                    if (!string.Equals(classi[j].Indirizzo.Nome, studenti[i].IndirizzoPreferito, StringComparison.OrdinalIgnoreCase))
                    {
                        AddPenalty((IntVar)x[i, j], 1000);
                    }
                }
            }


            // BILANCIAMENTO SCUOLA DI PROVENIENZA:
            // Evitiamo che troppi studenti della stessa scuola elementare/media finiscano tutti nella stessa classe (o nessuno).
            // Questo aiuta l'integrazione con studenti di altre zone.
            foreach (var codice in CodiciScuolaDistinti(studenti))
            {
                var scuolaIdx = IndiciStudenti(studenti, s => s.CodiceScuolaProvenienza == codice);
                if (scuolaIdx.Count < 2) continue;

                string tag = "sc_" + new string(codice.Where(char.IsLetterOrDigit).Take(8).ToArray());

                AddPenalty(PenalitaBilancio(model, x, n, m, scuolaIdx, tag), 60);
            }


            // Assembla e imposta la funzione obiettivo (minimizzare la somma pesata delle penalità)
            model.Minimize(LinearExpr.WeightedSum(
                objVars.ToArray(), objWeights.ToArray()));

            // ESECUZIONE DEL SOLVER:
            var solver = new CpSolver();
            // Impostiamo un timeout di 30 secondi e usiamo 4 core per accelerare la ricerca della soluzione ottima.
            solver.StringParameters = "max_time_in_seconds:30.0 num_search_workers:4";

            var status = solver.Solve(model);

            // Se il solver non trova nemmeno una soluzione fattibile (che rispetti i vincoli P1), lanciamo un'eccezione.
            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
                throw new DomainException($"OR-Tools non ha trovato una soluzione fattibile (status: {status}). " + "Verificare che i vincoli P1 siano soddisfacibili.");


            // ESTRAZIONE DEI RISULTATI:
            // Mappiamo ogni studente alla classe assegnata leggendo il valore delle variabili x[i, j].
            var result = new Dictionary<Guid, Guid>(n);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (solver.Value(x[i, j]) == 1L)
                    {
                        result[studenti[i].Id] = classi[j].Id;
                        break;
                    }

            return result;
        }


        // LOGICA DI CALCOLO DELLO SBILANCIAMENTO:
        // Crea una variabile intera che rappresenta la differenza tra il conteggio massimo di una categoria 
        // in una classe e il conteggio minimo. Il solver cercherà di rendere questa differenza il più piccola possibile.
        private static IntVar PenalitaBilancio(CpModel model, BoolVar[,] x, int n, int m, List<int> groupIdx, string tag)
        {
            var count = new IntVar[m];
            for (int j = 0; j < m; j++)
            {
                // Somma le variabili x[i, j] per tutti gli studenti i che appartengono al gruppo specifico
                count[j] = model.NewIntVar(0, groupIdx.Count, $"{tag}_cnt_{j}");
                model.Add(count[j] == LinearExpr.Sum(groupIdx.Select(i => (IntVar)x[i, j]).ToArray()));
            }

            // Identifica il valore massimo e minimo tra tutte le classi
            var maxV = model.NewIntVar(0, groupIdx.Count, $"{tag}_max");
            var minV = model.NewIntVar(0, groupIdx.Count, $"{tag}_min");
            model.AddMaxEquality(maxV, count);
            model.AddMinEquality(minV, count);

            // Lo sbilanciamento è la differenza tra massimo e minimo
            var imbalance = model.NewIntVar(0, groupIdx.Count, $"{tag}_imb");
            model.Add(imbalance == maxV - minV);

            return imbalance;
        }


        //  IndiciStudenti — ritorna gli indici nella lista dove il predicato è vero
        

        private static List<int> IndiciStudenti(List<Studente> studenti, Func<Studente, bool> pred)
            => studenti
                .Select((s, i) => (s, i))
                .Where(t => pred(t.s))
                .Select(t => t.i)
                .ToList();


        //  PreferenzeCoppie — ora gestita da PreferenzaMatcher
        // (il vecchio metodo statico è stato sostituito)
        


           
        // CodiciScuolaDistinti — codici meccanografici presenti nel pool
        

        private static IEnumerable<string> CodiciScuolaDistinti(List<Studente> studenti)
            => studenti
                .Where(s => !string.IsNullOrWhiteSpace(s.CodiceScuolaProvenienza))
                .Select(s => s.CodiceScuolaProvenienza)
                .Distinct();


        //  ApplicaSoluzione
        //
        // Traduce il dizionario studenteId → classeId in chiamate ai metodiù
        // di dominio AggiungiStudente.
        //
        // I disabili vengono applicati per primi: questo garantisce che
        // ClassePrima.HasStudenteConDisabilita sia già impostato prima che
        // gli studenti normali vengano aggiunti, così il check di capienza
        // (20 vs 27) funziona correttamente.
        

        private static void ApplicaSoluzione(Dictionary<Guid, Guid> assegnazioni,List<Studente> studenti,List<ClassePrima> classi,OpzioniDistribuzione opzioni)
        {
            var classiById = classi.ToDictionary(c => c.Id);

            // OrderByDescending su HasDisabilita: true (1) prima di false (0)
            foreach (var studente in studenti.OrderByDescending(s => s.ProfiloBES.HasDisabilita))
            {
                if (!assegnazioni.TryGetValue(studente.Id, out var classeId))
                    throw new DomainException($"OR-Tools non ha assegnato lo studente " + $"{studente.Nome} {studente.Cognome} a nessuna classe. " + "Questo non dovrebbe accadere: verificare i vincoli hard del modello.");

                classiById[classeId].AggiungiStudente(studente, opzioni);
            }
        }


        //  P4 — Fase di settembre: integrazione bocciati e trasferimenti
        //
        // TODO:
        //   - Aggiungere i bocciati al pool
        //   - Vincolo hard: il bocciato NON torna nella stessa sezione dell'anno prec.
        //   - Estendere il modello OR-Tools con: x[i,j] = 0 se classi[j].Sezione
        //     coincide con la sezione precedente dello studente
        

        public Task IntegraBocciatiAsync(IEnumerable<Studente> bocciati, OpzioniDistribuzione? opzioni = null)
        {
            throw new NotImplementedException("P4 (integrazione bocciati e trasferimenti, settembre) non ancora implementata.");
        }
    }
}