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
Console.WriteLine("║   TEST INTERATTIVO DISTRIBUZIONE CLASSI             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

// ─── 1. INPUT UTENTE ───────────────────────────────────────────────
// Sezioni ammesse dal dominio (Automazione: A-D, Informatica: E-O)
string[] sezioniDisponibili = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "L", "M", "N", "O" };

int numSezioni = LeggiIntero("Quante sezioni (classi) vuoi creare? (max 13): ", 1, sezioniDisponibili.Length);
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

// ─── 3. SETUP REPOSITORY IN-MEMORY ────────────────────────────────
var studenteRepo = new InMemoryStudenteRepository();
var classeRepo = new InMemoryClasseRepository();

// ─── 4. CREAZIONE CLASSI ──────────────────────────────────────────
for (int idx = 0; idx < numSezioni; idx++)
{
    string sez = sezioniDisponibili[idx];
    string indirizzo = (sez == "A" || sez == "B" || sez == "C" || sez == "D") ? "Automazione" : "Informatica";
    var classe = ClassePrima.Crea(
        Sezione.Crea(sez),
        IndirizzoScolastico.Crea(indirizzo));
    await classeRepo.AddAsync(classe);
}

Console.WriteLine($"✅ Create {numSezioni} classi (1{sezioniDisponibili[0]} - 1{sezioniDisponibili[numSezioni - 1]})");
Console.WriteLine();

// ─── 5. CREAZIONE STUDENTI ────────────────────────────────────────
int cfCounter = 0;
// Scuole di provenienza simulate
string[] scuoleProvenienza = { "MIIS00100A", "MIIS00200B", "MIIS00300C", "MIIS00400D", "MIIS00500E" };

Studente CreaStudente(string nome, string cognome, Sesso sesso,
    bool hasDisabilita = false, bool hasDSA = false, bool isStraniero = false,
    int? votoEsame = null, string? codiceScuola = null)
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
        votoEsame: votoEsame);
}

// Generazione automatica con percentuali realistiche
int numFemmine = (int)(totaleStudenti * 0.13);
int numDisabili = Math.Min((int)(totaleStudenti * 0.03), numSezioni); // max 1 per classe

var studenti = new List<Studente>();
for (int i = 0; i < totaleStudenti; i++)
{
    bool isFemmina = i < numFemmine;
    bool isDisabile = (i >= numFemmine && i < numFemmine + numDisabili);
    bool isDSA = (i % 15 == 0 && !isDisabile);
    bool isStraniero = (i % 20 == 2);

    studenti.Add(CreaStudente(
        $"Nome{i + 1}",
        $"Cognome{i + 1}",
        isFemmina ? Sesso.Femmina : Sesso.Maschio,
        hasDisabilita: isDisabile,
        hasDSA: isDSA,
        isStraniero: isStraniero,
        votoEsame: (i % 5) + 6,
        codiceScuola: scuoleProvenienza[i % scuoleProvenienza.Length]));
}

foreach (var s in studenti)
    await studenteRepo.AddAsync(s);

Console.WriteLine($"✅ Generati {studenti.Count} studenti:");
Console.WriteLine($"   • {numFemmine} ragazze (13%)");
Console.WriteLine($"   • {numDisabili} con disabilità (3%, max 1/classe)");
Console.WriteLine($"   • {studenti.Count(s => s.ProfiloBES.HasDSA)} con DSA");
Console.WriteLine($"   • {studenti.Count(s => s.IsStraniero)} stranieri");
Console.WriteLine($"   • Da {scuoleProvenienza.Length} scuole di provenienza diverse");
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

    // ─── 7. REPORT TABELLARE ──────────────────────────────────────
    var classi = await classeRepo.GetAllAsync();
    var tuttiStudenti = await studenteRepo.GetAllAsync();

    Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║   RISULTATI DISTRIBUZIONE                                                          ║");
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
