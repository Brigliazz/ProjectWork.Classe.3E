using BlaisePascal.ProjectWork._3E.Infrastructure.Persistence;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Enums;
using BlaisePascal.ProjectWork._3E.Domain.Services;

// ═══════════════════════════════════════════════════════════════════
//  TEST DISTRIBUZIONE CLASSI — verifica il corretto funzionamento
//  dell'algoritmo DistribuzioneClassiService
// ═══════════════════════════════════════════════════════════════════

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("");
Console.WriteLine("   TEST ALGORITMO DI DISTRIBUZIONE CLASSI            ");
Console.WriteLine("");
Console.WriteLine();

// ─── 1. SETUP REPOSITORY IN-MEMORY ────────────────────────────────
var studenteRepo = new InMemoryStudenteRepository();
var classeRepo = new InMemoryClasseRepository();

// ─── 2. CREAZIONE CLASSI (4 sezioni Automazione: A, B, C, D) ──────
string[] sezioni = { "A", "B", "C", "D" };
foreach (var sez in sezioni)
{
    var classe = ClassePrima.Crea(
        Sezione.Crea(sez),
        IndirizzoScolastico.Crea("Automazione"));
    await classeRepo.AddAsync(classe);
}

Console.WriteLine("✅ Create 4 classi: 1A, 1B, 1C, 1D (Automazione)");
Console.WriteLine();

// ─── 3. CREAZIONE STUDENTI ────────────────────────────────────────
// Helper per velocizzare la creazione
int cfCounter = 0;
Studente CreaStudente(string nome, string cognome, Sesso sesso,
    bool hasDisabilita = false, bool hasDSA = false, bool isStraniero = false,
    int? votoEsame = null)
{
    cfCounter++;
    var cittadinanza = isStraniero ? Cittadinanza.Crea(100) : Cittadinanza.Italiana;
    var profiloBES = ProfiloBES.Crea(hasDisabilita, hasDSA, hasDisabilita); // disabilità → assBase = true
    return Studente.Crea(
        nome, cognome, sesso,
        $"TESTCF{cfCounter:D10}",
        new DateOnly(2011, 1, 1),
        isStraniero ? new DateOnly(2020, 6, 1) : null,
        cittadinanza,
        "MIIS00100A",
        profiloBES,
        faReligione: true,
        votoEsame: votoEsame);
}

// ── P1: Studenti con DISABILITÀ (2 studenti) ──
var studenti = new List<Studente>
{
    CreaStudente("Marco",    "Rossi",     Sesso.Maschio, hasDisabilita: true),
    CreaStudente("Giulia",   "Bianchi",   Sesso.Femmina, hasDisabilita: true),

    // ── P2.1: RAGAZZE (8 ragazze, dovrebbero raggrupparsi in gruppi di 3) ──
    CreaStudente("Sara",     "Verdi",     Sesso.Femmina),
    CreaStudente("Elena",    "Neri",      Sesso.Femmina),
    CreaStudente("Anna",     "Ferrari",   Sesso.Femmina),
    CreaStudente("Chiara",   "Colombo",   Sesso.Femmina),
    CreaStudente("Martina",  "Ricci",     Sesso.Femmina),
    CreaStudente("Laura",    "Galli",     Sesso.Femmina),
    CreaStudente("Federica", "Conti",     Sesso.Femmina),
    CreaStudente("Valentina","Moretti",   Sesso.Femmina),

    // ── P2.2: Studenti con DSA (4 studenti) ──
    CreaStudente("Luca",     "Esposito",  Sesso.Maschio, hasDSA: true),
    CreaStudente("Andrea",   "Romano",    Sesso.Maschio, hasDSA: true),
    CreaStudente("Davide",   "Greco",     Sesso.Maschio, hasDSA: true),
    CreaStudente("Simone",   "Bruno",     Sesso.Maschio, hasDSA: true),

    // ── P2.3: STRANIERI (4 studenti) ──
    CreaStudente("Ahmed",    "Hassan",    Sesso.Maschio, isStraniero: true),
    CreaStudente("Wei",      "Chen",      Sesso.Maschio, isStraniero: true),
    CreaStudente("Olga",     "Petrov",    Sesso.Femmina, isStraniero: true),
    CreaStudente("Carlos",   "Mendez",    Sesso.Maschio, isStraniero: true),

    // ── P3: STUDENTI REGOLARI (12 ragazzi italiani) ──
    CreaStudente("Francesco","Russo",     Sesso.Maschio, votoEsame: 8),
    CreaStudente("Alessandro","Marino",   Sesso.Maschio, votoEsame: 7),
    CreaStudente("Matteo",   "Giordano",  Sesso.Maschio, votoEsame: 9),
    CreaStudente("Lorenzo",  "Lombardi",  Sesso.Maschio, votoEsame: 10),
    CreaStudente("Gabriele", "Morandi",   Sesso.Maschio, votoEsame: 6),
    CreaStudente("Riccardo", "Fontana",   Sesso.Maschio, votoEsame: 8),
    CreaStudente("Filippo",  "Barbieri",  Sesso.Maschio, votoEsame: 7),
    CreaStudente("Tommaso",  "Marchetti", Sesso.Maschio, votoEsame: 9),
    CreaStudente("Giovanni", "Rinaldi",   Sesso.Maschio, votoEsame: 6),
    CreaStudente("Giacomo",  "Gatto",     Sesso.Maschio, votoEsame: 8),
    CreaStudente("Emanuele", "Caruso",    Sesso.Maschio, votoEsame: 7),
    CreaStudente("Pietro",   "De Luca",   Sesso.Maschio, votoEsame: 9),
};

foreach (var s in studenti)
    await studenteRepo.AddAsync(s);

Console.WriteLine($"✅ Creati {studenti.Count} studenti:");
Console.WriteLine($"   • 2 con disabilità (P1)");
Console.WriteLine($"   • 8 ragazze — di cui 1 anche con disabilità (P2.1)");
Console.WriteLine($"   • 4 con DSA (P2.2)");
Console.WriteLine($"   • 4 stranieri — di cui 1 ragazza (P2.3)");
Console.WriteLine($"   • 12 ragazzi regolari (P3)");
Console.WriteLine();

// ─── 4. ESECUZIONE DISTRIBUZIONE ──────────────────────────────────
Console.WriteLine("⏳ Esecuzione dell'algoritmo di distribuzione...");
Console.WriteLine();

var service = new DistribuzioneClassiService(studenteRepo, classeRepo);
await service.DistribuisciAsync();

Console.WriteLine("✅ Distribuzione completata!");
Console.WriteLine();

// ─── 5. REPORT DETTAGLIATO ────────────────────────────────────────
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║   RISULTATI DISTRIBUZIONE                           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

var classi = await classeRepo.GetAllAsync();
var tuttiStudenti = await studenteRepo.GetAllAsync();

int totaleAssegnati = 0;

foreach (var classe in classi.OrderBy(c => c.Sezione.Valore))
{
    var studentiClasse = tuttiStudenti
        .Where(s => s.ClasseId == classe.Id)
        .ToList();

    int nFemmine = studentiClasse.Count(s => s.Sesso == Sesso.Femmina);
    int nDSA = studentiClasse.Count(s => s.ProfiloBES.HasDSA);
    int nStranieri = studentiClasse.Count(s => s.IsStraniero);
    int nDisabilita = studentiClasse.Count(s => s.ProfiloBES.HasDisabilita);
    totaleAssegnati += studentiClasse.Count;

    Console.WriteLine($"┌─ CLASSE 1{classe.Sezione.Valore} ({classe.Indirizzo.Nome}) ─────────────────────");
    Console.WriteLine($"│  Totale studenti: {studentiClasse.Count}");
    Console.WriteLine($"│  Disabilità: {nDisabilita}  │  DSA: {nDSA}  │  Stranieri: {nStranieri}  │  Femmine: {nFemmine}");
    Console.WriteLine($"│  HasStudenteConDisabilita: {classe.HasStudenteConDisabilita}");
    Console.WriteLine("│");

    foreach (var s in studentiClasse)
    {
        string tags = string.Join(", ", GetTags(s));
        string tagStr = tags.Length > 0 ? $" [{tags}]" : "";
        Console.WriteLine($"│  • {s.Cognome} {s.Nome} ({s.Sesso}){tagStr}");
    }

    Console.WriteLine("└──────────────────────────────────────────────────");
    Console.WriteLine();
}

// ─── 6. RIEPILOGO E VERIFICHE ─────────────────────────────────────
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║   VERIFICHE AUTOMATICHE                             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

int nonAssegnati = tuttiStudenti.Count(s => s.Stato == StatoAssegnazione.NonAssegnato);
Console.WriteLine($"Totale studenti: {tuttiStudenti.Count}  |  Assegnati: {totaleAssegnati}  |  Non assegnati: {nonAssegnati}");
Console.WriteLine();

// Verifica P1: max 1 studente disabile per classe
bool p1Ok = classi.All(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.ProfiloBES.HasDisabilita) <= 1);
Console.WriteLine($"[P1] Max 1 studente con disabilità per classe:  {(p1Ok ? "✅ OK" : "❌ FALLITO")}");

// Verifica P1: classi con disabilità devono avere max 20 studenti
bool p1LimiteOk = classi
    .Where(c => c.HasStudenteConDisabilita)
    .All(c => c.NumeroStudenti <= 20);
Console.WriteLine($"[P1] Classi con disabilità ≤ 20 studenti:       {(p1LimiteOk ? "✅ OK" : "❌ FALLITO")}");

// Verifica P2.1: ragazze non isolate (almeno 2 per classe, se possibile)
var classiConFemmine = classi
    .Select(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.Sesso == Sesso.Femmina))
    .Where(n => n > 0)
    .ToList();
bool p21Ok = classiConFemmine.All(n => n >= 2);
Console.WriteLine($"[P2.1] Ragazze non isolate (≥ 2 per classe):    {(p21Ok ? "✅ OK" : "⚠️  PARZIALE")}");

// Verifica P2.2: distribuzione DSA
var dsaPerClasse = classi
    .Select(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.ProfiloBES.HasDSA))
    .ToList();
int dsaMax = dsaPerClasse.Max();
int dsaMin = dsaPerClasse.Min();
bool p22Ok = (dsaMax - dsaMin) <= 1;
Console.WriteLine($"[P2.2] DSA distribuiti uniformemente (Δ≤1):      {(p22Ok ? "✅ OK" : "⚠️  Δ=" + (dsaMax - dsaMin))}  ({string.Join(", ", dsaPerClasse)})");

// Verifica P2.3: distribuzione stranieri
var stranieriPerClasse = classi
    .Select(c => tuttiStudenti.Count(s => s.ClasseId == c.Id && s.IsStraniero))
    .ToList();
int strMax = stranieriPerClasse.Max();
int strMin = stranieriPerClasse.Min();
bool p23Ok = (strMax - strMin) <= 1;
Console.WriteLine($"[P2.3] Stranieri distribuiti uniformemente (Δ≤1):{(p23Ok ? "✅ OK" : "⚠️  Δ=" + (strMax - strMin))}  ({string.Join(", ", stranieriPerClasse)})");

// Verifica P3: bilanciamento numerico
var numStudPerClasse = classi.Select(c => c.NumeroStudenti).ToList();
int maxNum = numStudPerClasse.Max();
int minNum = numStudPerClasse.Min();
bool p3Ok = (maxNum - minNum) <= 2;
Console.WriteLine($"[P3]  Bilanciamento numerico (Δ≤2):              {(p3Ok ? "✅ OK" : "⚠️  Δ=" + (maxNum - minNum))}  ({string.Join(", ", numStudPerClasse)})");

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  Test completato.");
Console.WriteLine("═══════════════════════════════════════════════════════");

// Helper
static IEnumerable<string> GetTags(Studente s)
{
    if (s.ProfiloBES.HasDisabilita) yield return "DISABILITÀ";
    if (s.ProfiloBES.HasDSA) yield return "DSA";
    if (s.IsStraniero) yield return "STRANIERO";
    if (s.IsEccellenza) yield return "ECCELLENZA";
}
