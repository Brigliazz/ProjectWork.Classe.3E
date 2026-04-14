using BlaisePascal.ProjectWork._3E.Infrastructure.Persistence;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Enums;
using BlaisePascal.ProjectWork._3E.Domain.Services;

// ═══════════════════════════════════════════════════════════════════
//  TEST INTERATTIVO — Distribuzione Classi
// ═══════════════════════════════════════════════════════════════════

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
int numAutomazione = LeggiIntero("Quante sezioni di Automazione vuoi creare? (max 4): ", 0, 4);
int numInformatica = LeggiIntero("Quante sezioni di Informatica vuoi creare? (max 9): ", 0, 9);
int numBio = LeggiIntero("Quante sezioni di Bio vuoi creare? (max 1): ", 0, 1);

int numSezioni = numAutomazione + numInformatica + numBio;
if (numSezioni == 0)
{
    Console.WriteLine("Devi creare almeno una sezione. Uscita.");
    return;
}

int totaleStudenti = LeggiIntero("Quanti studenti vuoi generare? : ", 1, 1000);

// ─── 2. CALCOLO MEDIA E RICHIESTA AUTORIZZAZIONE ──────────────────
double media = (double)totaleStudenti / numSezioni;
Console.WriteLine();
Console.WriteLine($"📊 Media studenti per classe: {media:F1}");

var opzioni = new OpzioniDistribuzione();

if (media > 27)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine();
    Console.WriteLine("⚠️  ATTENZIONE: la media supera il limite legale di 27 studenti per classe!");
    Console.WriteLine($"    Con {totaleStudenti} studenti e {numSezioni} classi, la media è {media:F1}.");
    Console.WriteLine("    È necessaria un'autorizzazione per procedere in deroga (sforo).");
    Console.ResetColor();
    Console.WriteLine();
    bool contr = false;
    while (!contr)
    {
        Console.Write("Autorizzi lo sforo dei limiti? (s/n): ");

        string? risposta = Console.ReadLine()?.Trim().ToLower();
        if (risposta == "s" || risposta == "si" || risposta == "sì")
        {
            opzioni.ConsentiSforo = true;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Sforo autorizzato. L'algoritmo procederà in deroga.");
            Console.ResetColor();
            contr = true;
        }
        else if (risposta == "n" || risposta == "no")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Sforo NON autorizzato. L'algoritmo potrebbe non riuscire ad assegnare tutti gli studenti.");
            Console.ResetColor();
            contr = true;
            return;
        }

        Console.WriteLine();
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ La media rientra nei limiti (≤ 27). Nessuna deroga necessaria.");
    Console.ResetColor();
    Console.WriteLine();
}

// ─── 2b. RICHIESTA PREFERENZE ─────────────────────────────────────
Console.Write("Vuoi attivare il matching delle preferenze compagno? (s/n, default s): ");
string? rispPref = Console.ReadLine()?.Trim().ToLower();
if (rispPref == "n" || rispPref == "no")
{
    opzioni.UsaPreferenze = false;
    Console.WriteLine("ℹ️  Preferenze disattivate.");
}
else
{
    opzioni.UsaPreferenze = true;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ Preferenze attivate — il matcher fuzzy verrà eseguito.");
    Console.ResetColor();
}
Console.WriteLine();

// ─── 3. SETUP REPOSITORY IN-MEMORY ────────────────────────────────
var studenteRepo = new InMemoryStudenteRepository();
var classeRepo = new InMemoryClasseRepository();

// ─── 4. IMPOSTAZIONE GENERAZIONE DINAMICA ───────────────────────────
// Invece di pre-creare le classi le facciamo generare dinamicamente dall'algoritmo
opzioni.SezioniPerIndirizzo["Automazione"] = numAutomazione;
opzioni.SezioniPerIndirizzo["Informatica"] = numInformatica;
opzioni.SezioniPerIndirizzo["Bio"] = numBio;

Console.WriteLine($"✅ Verranno generate dinamicamente {numSezioni} classi dall'algoritmo.");
Console.WriteLine();

// ─── 5. CREAZIONE STUDENTI ────────────────────────────────────────
int cfCounter = 0;
// Scuole di provenienza simulate
string[] scuoleProvenienza = { "MIIS00100A", "MIIS00200B", "MIIS00300C", "MIIS00400D", "MIIS00500E" };

Studente CreaStudente(string nome, string cognome, Sesso sesso,
    bool hasDisabilita = false, bool hasDSA = false, bool isStraniero = false,
    int? votoEsame = null, string? codiceScuola = null, SceltaCompagno? sceltaCompagno = null, string? indirizzoPreferito = null)
{
    cfCounter++;
    var cittadinanza = isStraniero ? Cittadinanza.Crea(100) : Cittadinanza.Italiana;
    var profiloBES = ProfiloBES.Crea(hasDisabilita, hasDSA, hasDisabilita);
    return Studente.Crea(
        nome, cognome, sesso,
        $"TESTCF{cfCounter:D10}",
        new DateOnly(2011, 1, 1),
        isStraniero ? new DateOnly(2020, 6, 1) : null,
        cittadinanza,
        codiceScuola ?? "MIIS00100A",
        profiloBES,
        faReligione: true,
        votoEsame: votoEsame,
        sceltaCompagno: sceltaCompagno,
        indirizzoPreferito: indirizzoPreferito);
}

// Generazione automatica con percentuali realistiche
int numFemmine = (int)(totaleStudenti * 0.13);
int numDisabili = Math.Min((int)(totaleStudenti * 0.03), numSezioni); // max 1 per classe

// ─── 5b. PREFERENZE DI TEST ───────────────────────────────────────
// Prepara le preferenze da assegnare durante la creazione degli studenti.
// Le preferenze fanno riferimento ad altri studenti per nome/cognome.
// Poiché i nomi sono deterministici ("NomeN", "CognomeN"), possiamo prefabbricarle.
int prefAssegnate = 0;
var preferenzePerIndice = new Dictionary<int, SceltaCompagno>();

if (opzioni.UsaPreferenze && totaleStudenti >= 10)
{
    // Studente 0 preferisce Studente 1 (match esatto → Certo)
    preferenzePerIndice[0] = SceltaCompagno.Crea("Nome2 Cognome2");
    // Studente 2 preferisce Studente 3 con errore di battitura (match fuzzy)
    preferenzePerIndice[2] = SceltaCompagno.Crea("Nome4 Cognome4xx");
    // Studente 4 preferisce Studente 5 con nome invertito
    preferenzePerIndice[4] = SceltaCompagno.Crea("Cognome6 Nome6");
    // Studente 6 scrive "nessuno"
    preferenzePerIndice[6] = SceltaCompagno.Crea("nessuno");
    // Studente 8 con testo irriconoscibile
    preferenzePerIndice[8] = SceltaCompagno.Crea("zzzzxxx qqqqqqq");
    prefAssegnate = preferenzePerIndice.Count;
}

var studenti = new List<Studente>();
string[] indirizziDisponibili = { "Informatica", "Automazione", "Bio" };

for (int i = 0; i < totaleStudenti; i++)
{
    bool isFemmina = i < numFemmine;
    bool isDisabile = (i >= numFemmine && i < numFemmine + numDisabili);
    bool isDSA = (i % 15 == 0 && !isDisabile);
    bool isStraniero = (i % 20 == 2);

    preferenzePerIndice.TryGetValue(i, out var sceltaCompagno);

    // Assegnamo un indirizzo preferito al 70% degli studenti
    string? indirizzoPreferito = (i % 10 < 7) ? indirizziDisponibili[i % 3] : null;

    studenti.Add(CreaStudente(
        $"Nome{i + 1}",
        $"Cognome{i + 1}",
        isFemmina ? Sesso.Femmina : Sesso.Maschio,
        hasDisabilita: isDisabile,
        hasDSA: isDSA,
        isStraniero: isStraniero,
        votoEsame: (i % 5) + 6,
        codiceScuola: scuoleProvenienza[i % scuoleProvenienza.Length],
        sceltaCompagno: sceltaCompagno,
        indirizzoPreferito: indirizzoPreferito));
}

foreach (var s in studenti)
    await studenteRepo.AddAsync(s);

Console.WriteLine($"✅ Generati {studenti.Count} studenti:");
Console.WriteLine($"   • {numFemmine} ragazze (13%)");
Console.WriteLine($"   • {numDisabili} con disabilità (3%, max 1/classe)");
Console.WriteLine($"   • {studenti.Count(s => s.ProfiloBES.HasDSA)} con DSA");
Console.WriteLine($"   • {studenti.Count(s => s.IsStraniero)} stranieri");
Console.WriteLine($"   • Da {scuoleProvenienza.Length} scuole di provenienza diverse");
if (prefAssegnate > 0)
    Console.WriteLine($"   • {prefAssegnate} preferenze compagno di test assegnate");
Console.WriteLine();

// ─── 6. ESECUZIONE DISTRIBUZIONE ──────────────────────────────────
Console.WriteLine("⏳ Esecuzione dell'algoritmo di distribuzione...");
Console.WriteLine();

var service = new DistribuzioneClassiService(studenteRepo, classeRepo);

try
{
    List<List<Studente>> matriceClassi = await service.DistribuisciAsync(opzioni);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ Distribuzione completata!");
    Console.ResetColor();
    Console.WriteLine();

    // ─── 6b. REPORT MATCH PREFERENZE ──────────────────────────────
    if (opzioni.UsaPreferenze && service.MatchIncerti.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   MATCH INCERTI — REVISIONE RICHIESTA                                             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        foreach (var m in service.MatchIncerti)
        {
            Console.WriteLine($"  ⚠️  {m.Richiedente.Nome} {m.Richiedente.Cognome}");
            Console.WriteLine($"      Testo: \"{m.TestoOriginale}\"");
            Console.WriteLine($"      Candidato: {m.CandidatoTrovato?.Nome} {m.CandidatoTrovato?.Cognome} (score: {m.Score})");
            Console.WriteLine($"      → {m.Messaggio}");
            Console.WriteLine();
        }
    }
    else if (opzioni.UsaPreferenze)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Nessun match incerto — tutte le preferenze sono state risolte automaticamente.");
        Console.ResetColor();
        Console.WriteLine();
    }

    // ─── 7. REPORT TABELLARE ──────────────────────────────────────
    var classi = await classeRepo.GetAllAsync();
    var tuttiStudenti = await studenteRepo.GetAllAsync();

    Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║   RISULTATI DISTRIBUZIONE                                                            ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    int totaleAssegnati = 0;

    foreach (var studentiClasse in matriceClassi)
    {
        if (studentiClasse.Count == 0) continue;

        var classeId = studentiClasse.First().ClasseId;
        var classeTarget = classi.FirstOrDefault(c => c.Id == classeId);
        if (classeTarget == null) continue;

        int nFemmine = studentiClasse.Count(s => s.Sesso == Sesso.Femmina);
        int nDSA = studentiClasse.Count(s => s.ProfiloBES.HasDSA);
        int nStranieri = studentiClasse.Count(s => s.IsStraniero);
        int nDisabilita = studentiClasse.Count(s => s.ProfiloBES.HasDisabilita);
        totaleAssegnati += studentiClasse.Count;

        string info1 = $"CLASSE 1{classeTarget.Sezione.Valore} ({classeTarget.Indirizzo.Nome})";
        string info2 = $"Totale: {studentiClasse.Count} | Disabilità: {nDisabilita} | DSA: {nDSA} | Stranieri: {nStranieri} | Femmine: {nFemmine}";

        // Evidenzia in giallo se la classe supera il limite standard
        bool inSforo = studentiClasse.Count > (nDisabilita > 0 ? 20 : 27);

        Console.WriteLine($"┌──────────────────────────────────────────────────────────────────────────────────────┐");
        if (inSforo) Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"│ {info1.PadRight(84)} │");
        Console.WriteLine($"│ {info2.PadRight(84)} │");
        if (inSforo)
        {
            Console.WriteLine($"│ {"⚠️  CLASSE IN DEROGA (sforo autorizzato)",-84} │");
            Console.ResetColor();
        }
        Console.WriteLine($"├──────────────────────┬──────────────────────┬─────────┬──────────────────────────────┤");
        Console.WriteLine($"│ {"Cognome",-20} │ {"Nome",-20} │ {"Sesso",-7} │ {"Caratteristiche",-28} │");
        Console.WriteLine($"├──────────────────────┼──────────────────────┼─────────┼──────────────────────────────┤");

        foreach (var s in studentiClasse)
        {
            string tags = string.Join(", ", GetTags(s));
            Console.WriteLine($"│ {s.Cognome,-20} │ {s.Nome,-20} │ {s.Sesso,-7} │ {tags,-28} │");
        }

        Console.WriteLine($"└──────────────────────┴──────────────────────┴─────────┴──────────────────────────────┘");
        Console.WriteLine();
    }

    // ─── 8. VERIFICHE AUTOMATICHE ─────────────────────────────────
    Console.WriteLine("╔══════════════════════════════════════════════════════╗");
    Console.WriteLine("║   VERIFICHE AUTOMATICHE                             ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════╝");
    Console.WriteLine();

    int nonAssegnati = tuttiStudenti.Count(s => s.Stato == StatoAssegnazione.NonAssegnato);
    Console.WriteLine($"Totale: {tuttiStudenti.Count}  |  Assegnati: {totaleAssegnati}  |  Non assegnati: {nonAssegnati}");
    Console.WriteLine();

    bool p1Ok = classi.All(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.ProfiloBES.HasDisabilita) <= 1);
    Console.WriteLine($"[P1]   Max 1 disabile per classe:      {(p1Ok ? "✅ OK" : "❌ FALLITO")}");

    var dsaPerClasse = classi.Select(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.ProfiloBES.HasDSA)).ToList();
    bool p22Ok = (dsaPerClasse.Max() - dsaPerClasse.Min()) <= 1;
    Console.WriteLine($"[P2.2] DSA bilanciati (Δ≤1):           {(p22Ok ? "✅ OK" : "⚠️  Δ=" + (dsaPerClasse.Max() - dsaPerClasse.Min()))}  ({string.Join(", ", dsaPerClasse)})");

    var strPerClasse = classi.Select(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.IsStraniero)).ToList();
    bool p23Ok = (strPerClasse.Max() - strPerClasse.Min()) <= 1;
    Console.WriteLine($"[P2.3] Stranieri bilanciati (Δ≤1):     {(p23Ok ? "✅ OK" : "⚠️  Δ=" + (strPerClasse.Max() - strPerClasse.Min()))}  ({string.Join(", ", strPerClasse)})");

    var numPerClasse = classi.Select(c => c.NumeroStudenti).ToList();
    bool p3Ok = (numPerClasse.Max() - numPerClasse.Min()) <= 2;
    Console.WriteLine($"[P3]   Bilanciamento numerico (Δ≤2):   {(p3Ok ? "✅ OK" : "⚠️  Δ=" + (numPerClasse.Max() - numPerClasse.Min()))}  ({string.Join(", ", numPerClasse)})");

    // Verifica bilanciamento per scuola di provenienza
    Console.WriteLine();
    Console.WriteLine("── Bilanciamento Scuole di Provenienza ──");
    var codiciScuolaPresenti = tuttiStudenti
        .Where(s => s.ClasseId != null)
        .Select(s => s.CodiceScuolaProvenienza)
        .Distinct()
        .OrderBy(c => c)
        .ToList();

    foreach (var codice in codiciScuolaPresenti)
    {
        var perClasse = classi
            .Select(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.CodiceScuolaProvenienza == codice))
            .ToList();
        int delta = perClasse.Max() - perClasse.Min();
        bool ok = delta <= 1;
        Console.WriteLine($"[Scuola {codice}] Δ={delta}: {(ok ? "✅" : "⚠️")}  ({string.Join(", ", perClasse)})");
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Errore durante la distribuzione: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  Test completato.");
Console.WriteLine("═══════════════════════════════════════════════════════");

// ═══════════════════════════════════════════════════════════════════
//  HELPER
// ═══════════════════════════════════════════════════════════════════

static int LeggiIntero(string prompt, int min, int max)
{
    int valore;
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out valore) && valore >= min && valore <= max)
            return valore;
        Console.WriteLine($"  ⚠️  Inserisci un numero tra {min} e {max}.");
    }
}

static IEnumerable<string> GetTags(Studente s)
{
    if (s.ProfiloBES.HasDisabilita) yield return "DISABILITÀ";
    if (s.ProfiloBES.HasDSA) yield return "DSA";
    if (s.IsStraniero) yield return "STRANIERO";
    if (s.IsEccellenza) yield return "ECCELLENZA";
}
