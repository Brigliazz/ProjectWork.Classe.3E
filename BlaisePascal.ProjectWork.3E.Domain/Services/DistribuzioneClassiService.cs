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
        
        // Validazione preventiva dei vincoli strutturali P1 prima dell'invio al solver
        private static List<string> ValidaVincoliP1(List<Studente> studenti, List<ClassePrima> classi, OpzioniDistribuzione opzioni)
        {
            var violazioni = new List<string>();
            var indirizziDistinti = classi.Select(c => c.Indirizzo).Distinct().ToList();

            foreach (var indirizzo in indirizziDistinti)
            {
                int classiIndirizzo = classi.Count(c => c.Indirizzo == indirizzo);
                if (classiIndirizzo == 0) continue;

                var studentiIndirizzo = studenti.Where(s => s.IndirizzoScolastico == indirizzo).ToList();
                int totStudenti = studentiIndirizzo.Count;

                // 1. Overflow capienza
                if (totStudenti > classiIndirizzo * opzioni.LimiteStandard)
                {
                    violazioni.Add($"[{indirizzo.Nome}] Capienza: {totStudenti} studenti su {classiIndirizzo} classi disponibili (max {opzioni.LimiteStandard} per classe, totale {classiIndirizzo * opzioni.LimiteStandard}).");
                }

                // 2. Overflow disabili
                int totDisabili = studentiIndirizzo.Count(s => s.ProfiloBES.HasDisabilita);
                if (totDisabili > classiIndirizzo)
                {
                    violazioni.Add($"[{indirizzo.Nome}] Disabili: {totDisabili} su {classiIndirizzo} classi disponibili (max 1 per classe).");
                }

                // 3. Overflow stranieri
                int totStranieri = studentiIndirizzo.Count(s => s.IsStraniero);
                int attesoPerClasse = (int)Math.Ceiling((double)totStudenti / classiIndirizzo);
                int maxStranieriPerClasse = Math.Max(1, (int)Math.Floor(0.30 * attesoPerClasse));
                int maxStranieriTotale = classiIndirizzo * maxStranieriPerClasse;
                
                if (totStranieri > maxStranieriTotale)
                {
                    violazioni.Add($"[{indirizzo.Nome}] Stranieri: {totStranieri} su limite calcolato di {maxStranieriTotale} (max {maxStranieriPerClasse} per classe).");
                }
            }

            return violazioni;
        }

        private static Dictionary<Guid, Guid> RisolviConOrTools(List<Studente> studenti, List<ClassePrima> classi, OpzioniDistribuzione opzioni,List<(int IdxI, int IdxJ)> coppiePreferenze)
        {
            var violazioniStrutturali = ValidaVincoliP1(studenti, classi, opzioni);
            if (violazioniStrutturali.Count > 0)
            {
                foreach (var v in violazioniStrutturali)
                    Console.WriteLine($"[P1-WARN] {v}");
            }

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


            // VINCOLO P1: ogni studente può essere assegnato solo a classi del suo indirizzo.
            // Se l'indirizzo dello studente non corrisponde a quello della classe, x[i,j] = 0.
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (studenti[i].IndirizzoScolastico != classi[j].Indirizzo)
                        model.Add(x[i, j] == 0);


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
                    else
                    {
                        // In modalità sforo, imponiamo quantomeno il limite fisico/assoluto della classe
                        var allVarsInClass = Enumerable.Range(0, n).Select(i => (IntVar)x[i, j]).ToArray();
                        model.Add(LinearExpr.Sum(allVarsInClass) <= opzioni.LimiteMassimoAssoluto);
                    }
                }
                else
                {
                    // Nessun disabile nell'istituto (o in questa iterazione)
                    var allVarsInClass = Enumerable.Range(0, n).Select(i => (IntVar)x[i, j]).ToArray();
                    if (!opzioni.ConsentiSforo)
                    {
                        model.Add(LinearExpr.Sum(allVarsInClass) <= opzioni.LimiteStandard);
                    }
                    else
                    {
                        model.Add(LinearExpr.Sum(allVarsInClass) <= opzioni.LimiteMassimoAssoluto);
                    }
                }
            }


            // VINCOLO SULLA PERCENTUALE DI STRANIERI:
            // Per regolamento, gli stranieri non dovrebbero superare il 30% degli alunni per classe.
            // Calcoliamo la soglia per indirizzo, non globale.
            if (stranieriIdx.Count > 0 && !opzioni.ConsentiSforo)
            {
                // Pre-calcolo: conteggi per indirizzo (evita O(n·m) nel loop)
                var studentiPerIndirizzo = studenti
                    .GroupBy(s => s.IndirizzoScolastico)
                    .ToDictionary(g => g.Key, g => g.Count());

                var classiPerIndirizzo = classi
                    .GroupBy(c => c.Indirizzo)
                    .ToDictionary(g => g.Key, g => g.Count());

                var stranieriPerIndirizzo = stranieriIdx
                    .GroupBy(i => studenti[i].IndirizzoScolastico)
                    .ToDictionary(g => g.Key, g => g.ToList());

                for (int j = 0; j < m; j++)
                {
                    var indirizzo = classi[j].Indirizzo;
                    int totStudentiIndirizzo = studentiPerIndirizzo.GetValueOrDefault(indirizzo, 0);
                    int totClassiIndirizzo   = classiPerIndirizzo.GetValueOrDefault(indirizzo, 0);

                    if (totClassiIndirizzo > 0)
                    {
                        int attesoPerClasse = (int)Math.Ceiling((double)totStudentiIndirizzo / totClassiIndirizzo);
                        int maxStranieri = Math.Max(1, (int)Math.Floor(0.30 * attesoPerClasse));
                        
                        // Applica il vincolo maxStranieri solo agli stranieri del medesimo indirizzo della classe
                        if (stranieriPerIndirizzo.TryGetValue(indirizzo, out var stranieriStessoIndirizzo)
                            && stranieriStessoIndirizzo.Count > 0)
                        {
                             model.Add(LinearExpr.Sum(stranieriStessoIndirizzo.Select(i => (IntVar)x[i, j]).ToArray()) <= maxStranieri);
                        }
                    }
                }
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
            // Il calcolo avviene rigorosamente per singolo Indirizzo, isolando minimi e massimi per impedire
            // sbilanciamenti strutturali o penalità impossibili e insormontabili che affliggerebbero OR-Tools.
            if (ragazzeIdx.Count   > 0) AddPenalty(PenalitaBilancio(model, x, n, m, ragazzeIdx,   "girl", studenti, classi), 100);
            if (stranieriIdx.Count > 0) AddPenalty(PenalitaBilancio(model, x, n, m, stranieriIdx, "str",  studenti, classi), 80);
            if (dsaIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, dsaIdx,       "dsa",  studenti, classi), 80);
            if (ircIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, ircIdx,       "irc",  studenti, classi), 30);
            if (eccIdx.Count       > 0) AddPenalty(PenalitaBilancio(model, x, n, m, eccIdx,       "ecc",  studenti, classi), 20);


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


            // BILANCIAMENTO SCUOLA DI PROVENIENZA:
            // Evitiamo che troppi studenti della stessa scuola elementare/media finiscano tutti nella stessa classe (o nessuno).
            // Questo aiuta l'integrazione con studenti di altre zone.
            foreach (var codice in CodiciScuolaDistinti(studenti))
            {
                var scuolaIdx = IndiciStudenti(studenti, s => s.CodiceScuolaProvenienza == codice);
                if (scuolaIdx.Count < 2) continue;

                string tag = "sc_" + new string(codice.Where(char.IsLetterOrDigit).Take(8).ToArray());

                AddPenalty(PenalitaBilancio(model, x, n, m, scuolaIdx, tag, studenti, classi), 60);
            }


            // Assembla e imposta la funzione obiettivo (minimizzare la somma pesata delle penalità)
            model.Minimize(LinearExpr.WeightedSum(
                objVars.ToArray(), objWeights.ToArray()));

            // ESECUZIONE DEL SOLVER:
            var solver = new CpSolver();
            // Impostiamo un timeout di 30 secondi e usiamo 4 core per accelerare la ricerca della soluzione ottima.
            solver.StringParameters = "max_time_in_seconds:30.0 num_search_workers:4";

            var status = solver.Solve(model);

            // Se il solver non trova nemmeno una soluzione fattibile (che rispetti i vincoli P1)
            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
            {
                if (!opzioni.ConsentiSforo)
                {
                    // Fallback automatico: consente lo sforo dei limiti (ammorbidisce la capienza) e riprova.
                    // Cloniamo le opzioni per evitare side-effect indesiderati sull'istanza originale passata dal chiamante.
                    var fallbackOpzioni = opzioni.Clone();
                    fallbackOpzioni.ConsentiSforo = true;
                    return RisolviConOrTools(studenti, classi, fallbackOpzioni, coppiePreferenze);
                }
                else
                {
                    // Se fallisce anche col ConsentiSforo attivo, lancerà un messaggio specifico
                    throw new DomainException($"OR-Tools non ha trovato una soluzione fattibile nemmeno dopo aver consentito lo sforo dei limiti (Fallback attempt: status {status}). " + 
                                              "Questo indica un conflitto strutturale nei vincoli hard P1 (es. troppi studenti/disabili/stranieri rispetto alle classi dell'indirizzo scelti). Controllare i [P1-WARN] nei log.");
                }
            }


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


        // LOGICA DI CALCOLO DELLO SBILANCIAMENTO (PER INDIRIZZO):
        // Calcola lo sbilanciamento (max - min) in modo perfettamente isolato rispetto ad ogni singolo IndirizzoScolastico.
        // Lo sbilanciamento finale restituito è la somma delle penalità individuali di tutti gli Indirizzi.
        private static IntVar PenalitaBilancio(CpModel model, BoolVar[,] x, int n, int m, List<int> groupIdx, string tag, List<Studente> studenti, List<ClassePrima> classi)
        {
            var indirizziDistinti = classi.Select(c => c.Indirizzo.Nome).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var imbalancePerIndirizzo = new List<IntVar>();

            foreach(var strIndirizzo in indirizziDistinti)
            {
                // Trova gli indici delle classi che appartengono a questo indirizzo
                var classiSottoinsieme = classi.Select((c, index) => new { Class = c, Index = index })
                                               .Where(ci => string.Equals(ci.Class.Indirizzo.Nome, strIndirizzo, StringComparison.OrdinalIgnoreCase))
                                               .Select(ci => ci.Index)
                                               .ToList();

                if(classiSottoinsieme.Count <= 1) continue; // Sbilanciamento assente/irragionevole su 0 o 1 classe

                // Identifica gli studenti interessati da questa categoria E appartenti a questo indirizzo
                var studentiSottoinsieme = groupIdx
                                           .Where(i => string.Equals(studenti[i].IndirizzoScolastico.Nome, strIndirizzo, StringComparison.OrdinalIgnoreCase))
                                           .ToList();
                
                // Se non c'è nessuno di questa categoria per questo indirizzo, lo sbilanciamento è nullo.
                if(studentiSottoinsieme.Count == 0) continue;

                var count = new IntVar[classiSottoinsieme.Count];
                for(int idx = 0; idx < classiSottoinsieme.Count; idx++)
                {
                    int j = classiSottoinsieme[idx];
                    count[idx] = model.NewIntVar(0, studentiSottoinsieme.Count, $"{tag}_{strIndirizzo}_cnt_{j}");
                    model.Add(count[idx] == LinearExpr.Sum(studentiSottoinsieme.Select(i => (IntVar)x[i, j]).ToArray()));
                }

                // Max e min conteggi circoscritti a questo indirizzo
                var maxV = model.NewIntVar(0, studentiSottoinsieme.Count, $"{tag}_{strIndirizzo}_max");
                var minV = model.NewIntVar(0, studentiSottoinsieme.Count, $"{tag}_{strIndirizzo}_min");
                model.AddMaxEquality(maxV, count);
                model.AddMinEquality(minV, count);

                // Calcola sbilanciamento e memorizzalo
                var imbalance = model.NewIntVar(0, studentiSottoinsieme.Count, $"{tag}_{strIndirizzo}_imb");
                model.Add(imbalance == maxV - minV);
                
                imbalancePerIndirizzo.Add(imbalance);
            }

            if(imbalancePerIndirizzo.Count == 0) return model.NewConstant(0);

            // La penalità globale della categoria "tag" è la somma lineare degli sbilanciamenti dei singoli indirizzi.
            var totalImbalance = model.NewIntVar(0, groupIdx.Count, $"{tag}_total_imb");
            model.Add(totalImbalance == LinearExpr.Sum(imbalancePerIndirizzo.ToArray()));
            
            return totalImbalance;
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