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
            var model = new CpModel();
            int n = studenti.Count;
            int m = classi.Count;

            //  Variabili di decisione 
            // x[i,j] = 1  ⟺  studente[i] è assegnato alla classe[j]
            var x = new BoolVar[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    x[i, j] = model.NewBoolVar($"x_{i}_{j}");


            //  VINCOLO HARD: ogni studente in esattamente una classe 
            for (int i = 0; i < n; i++)
                model.AddExactlyOne(
                    Enumerable.Range(0, m).Select(j => (ILiteral)x[i, j]).ToArray());


            //  Precalcolo degli indici per categoria 
            var disabiliIdx  = IndiciStudenti(studenti, s => s.ProfiloBES.HasDisabilita);
            var ragazzeIdx   = IndiciStudenti(studenti, s => s.Sesso == Sesso.Femmina);
            var stranieriIdx = IndiciStudenti(studenti, s => s.IsStraniero);
            var dsaIdx       = IndiciStudenti(studenti, s => s.ProfiloBES.HasDSA);
            var ircIdx       = IndiciStudenti(studenti, s => s.FaReligione);
            var eccIdx       = IndiciStudenti(studenti, s => s.IsEccellenza);


            //  VINCOLI HARD P1: capienza e limite disabili 
            //
            // Il delta è la differenza tra il limite standard e quello con disabili (27-20=7).
            // Modellazione della capienza condizionale:
            //   Se d[j]=0 (nessun disabile):  Σx[i,j]          ≤ 27
            //   Se d[j]=1 (un disabile):      Σx[i,j] + 7*d[j] ≤ 27  →  Σx[i,j] ≤ 20
            // Questa singola disuguaglianza copre entrambi i casi.

            int delta = opzioni.LimiteStandard - opzioni.LimiteDisabili; // 7

            for (int j = 0; j < m; j++)
            {
                if (disabiliIdx.Count > 0)
                {
                    var disInClass = disabiliIdx.Select(i => (IntVar)x[i, j]).ToArray();

                    // Max 1 disabile per classe
                    model.Add(LinearExpr.Sum(disInClass) <= 1);

                    if (!opzioni.ConsentiSforo)
                    {
                        // d[j] = 1 sse la classe j contiene uno studente disabile
                        var dj = model.NewBoolVar($"d_{j}");

                        // x[i,j]=1 implica d[j]=1 per ogni studente disabile i
                        foreach (var i in disabiliIdx)
                            model.AddImplication(x[i, j], dj);

                        // d[j]=1 implica che almeno un disabile è nella classe
                        model.Add(LinearExpr.Sum(disInClass) >= 1).OnlyEnforceIf(dj);

                        // Capienza condizionale: Σ(tutti) + delta*d[j] ≤ LimiteStandard
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
                    // Nessun disabile nell'intero pool: capienza piatta
                    model.Add(LinearExpr.Sum(Enumerable.Range(0, n).Select(i => (IntVar)x[i, j]).ToArray()) <= opzioni.LimiteStandard);
                }
            }


            //  VINCOLO HARD: max 30% stranieri per classe 
            //
            // NOTA: il vincolo esatto sarebbe Σ_stranieri / Σ_tutti ≤ 0.30,
            // ma è nonlineare perché anche il denominatore è variabile.
            // Approssimazione: si usa la dimensione media attesa come denominatore fisso.
            // L'errore è trascurabile perché le classi vengono bilanciate anche in obiettivo.

            if (stranieriIdx.Count > 0 && !opzioni.ConsentiSforo)
            {
                int attesoPerClasse = (int)Math.Ceiling((double)n / m);
                int maxStranieri    = Math.Max(1, (int)Math.Floor(0.30 * attesoPerClasse));

                for (int j = 0; j < m; j++)
                    model.Add(LinearExpr.Sum(stranieriIdx.Select(i => (IntVar)x[i, j]).ToArray()) <= maxStranieri);
            }


            //  FUNZIONE OBIETTIVO 
            //
            // Ogni termine ha la forma (IntVar, peso).
            // Il solver minimizza Σ(var * peso).
            // Le variabili rappresentano misure di "cattiveria" della distribuzione:
            // più sono alte, peggiore è la soluzione.

            var objVars    = new List<IntVar>();
            var objWeights = new List<long>();
            void AddPenalty(IntVar v, int w) { objVars.Add(v); objWeights.Add(w); }

            // Sbilanciamento per categoria — P2 (peso alto) e P3 (peso basso)
            // La funzione PenalitàBilancio crea una variabile che vale max(count) - min(count)
            // tra tutte le classi per la categoria specificata.
            if (ragazzeIdx.Count   > 0) AddPenalty(PenalitaBilancio(model, x, n, m, ragazzeIdx,   "girl"), 100);
            if (stranieriIdx.Count > 0) AddPenalty(PenalitaBilancio(model, x, n, m, stranieriIdx, "str"),   80);
            if (dsaIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, dsaIdx,       "dsa"),   80);
            if (ircIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, ircIdx,       "irc"),   30);
            if (eccIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, eccIdx,       "ecc"),   20);


            // Anti-isolamento ragazze (P2): penalizza ogni classe con meno di 2 ragazze.
            // Peso molto alto perché è un requisito esplicito del dominio.
            //
            // Modello:
            //   atLeast2[j] = 1  sse  girlCount[j] >= 2
            //   notAtLeast2[j]   = NOT(atLeast2[j])  → penalizzato in obiettivo
            //
            // OnlyEnforceIf garantisce che i vincoli sui conteggi siano attivi
            // solo quando la rispettiva variabile booleana è vera.

            if (ragazzeIdx.Count >= 2)
            {
                for (int j = 0; j < m; j++)
                {
                    var gc = model.NewIntVar(0, ragazzeIdx.Count, $"gc_{j}");
                    model.Add(gc == LinearExpr.Sum(
                        ragazzeIdx.Select(i => (IntVar)x[i, j]).ToArray()));

                    var atLeast2    = model.NewBoolVar($"al2_{j}");
                    var notAtLeast2 = model.NewBoolVar($"nal2_{j}");

                    model.Add(gc >= 2).OnlyEnforceIf(atLeast2);
                    model.Add(gc <= 1).OnlyEnforceIf(atLeast2.Not());
                    // Le due variabili booleane sono complementari (esattamente una vale 1)
                    model.AddBoolXor(new ILiteral[] { atLeast2, notAtLeast2 });

                    AddPenalty((IntVar)notAtLeast2, 500);
                }
            }


            // Preferenze SceltaCompagno (P2): matching fuzzy su testo libero.
            //
            // SceltaCompagno ha una proprietà .Testo di tipo string.
            //
            // Per ogni coppia (i,j) trovata:
            //   together[k] = 1  sse  entrambi i e j sono nella classe k
            //   inSameClass  = OR(together[k])  →  1 se nella stessa classe
            //   notTogether  = NOT(inSameClass)  →  penalizzato in obiettivo

            foreach (var (pi, pj) in coppiePreferenze)
            {
                var togetherK = new BoolVar[m];
                for (int k = 0; k < m; k++)
                {
                    togetherK[k] = model.NewBoolVar($"tog_{pi}_{pj}_{k}");
                    // together[k] = 1  ⟺  x[i,k]=1 AND x[j,k]=1
                    model.AddBoolAnd(new ILiteral[] { x[pi, k], x[pj, k] })
                         .OnlyEnforceIf(togetherK[k]);
                    model.AddBoolOr(new ILiteral[] { x[pi, k].Not(), x[pj, k].Not() })
                         .OnlyEnforceIf(togetherK[k].Not());
                }

                var inSameClass = model.NewBoolVar($"sc_{pi}_{pj}");
                // inSameClass = 1  ⟺  almeno un together[k] = 1
                model.AddBoolOr(togetherK.Cast<ILiteral>().ToArray())
                     .OnlyEnforceIf(inSameClass);
                model.AddBoolAnd(togetherK.Select(t => (ILiteral)t.Not()).ToArray())
                     .OnlyEnforceIf(inSameClass.Not());

                var notTogether = model.NewBoolVar($"nt_{pi}_{pj}");
                model.AddBoolXor(new ILiteral[] { inSameClass, notTogether });
                AddPenalty((IntVar)notTogether, 50);
            }


            // Bilanciamento per scuola di provenienza (P2).
            // Per ogni scuola con almeno 2 studenti, minimizza lo squilibrio
            // del numero di suoi studenti tra le classi.

            foreach (var codice in CodiciScuolaDistinti(studenti))
            {
                var scuolaIdx = IndiciStudenti(studenti, s => s.CodiceScuolaProvenienza == codice);
                if (scuolaIdx.Count < 2) continue;

                // Sanifica il codice per usarlo come parte del nome della variabile CP-SAT
                string tag = "sc_" + new string(codice.Where(char.IsLetterOrDigit).Take(8).ToArray());

                AddPenalty(PenalitaBilancio(model, x, n, m, scuolaIdx, tag), 60);
            }


            // Assembla e imposta la funzione obiettivo
            model.Minimize(LinearExpr.WeightedSum(
                objVars.ToArray(), objWeights.ToArray()));


            //  Risoluzione 
            var solver = new CpSolver();
            // 30 secondi di timeout; 4 thread paralleli per convergenza più rapida
            solver.StringParameters = "max_time_in_seconds:30.0 num_search_workers:4";

            var status = solver.Solve(model);

            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
                throw new DomainException($"OR-Tools non ha trovato una soluzione fattibile (status: {status}). " + "Verificare che i vincoli P1 siano soddisfacibili con il numero di classi " + "disponibili (es. abbastanza classi per i disabili, stranieri < 30%).");


            //  Estrazione soluzione 
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


        //  PenalitaBilancio
        //
        // Crea nel modello una variabile intera che rappresenta
        // max(count_j) − min(count_j),  dove count_j = numero di studenti
        // del gruppo 'groupIdx' nella classe j.
        //
        // Questa variabile viene poi aggiunta all'obiettivo con un peso:
        // il solver la spinge verso 0, cioè verso distribuzione uniforme.
        

        private static IntVar PenalitaBilancio(CpModel model, BoolVar[,] x, int n, int m, List<int> groupIdx, string tag)
        {
            var count = new IntVar[m];
            for (int j = 0; j < m; j++)
            {
                count[j] = model.NewIntVar(0, groupIdx.Count, $"{tag}_cnt_{j}");
                model.Add(count[j] == LinearExpr.Sum(groupIdx.Select(i => (IntVar)x[i, j]).ToArray()));
            }

            var maxV = model.NewIntVar(0, groupIdx.Count, $"{tag}_max");
            var minV = model.NewIntVar(0, groupIdx.Count, $"{tag}_min");
            model.AddMaxEquality(maxV, count);
            model.AddMinEquality(minV, count);

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
        // Traduce il dizionario studenteId → classeId in chiamate ai metodi
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