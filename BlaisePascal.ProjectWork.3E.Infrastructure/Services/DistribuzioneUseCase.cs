using Microsoft.EntityFrameworkCore;
using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using BlaisePascal.ProjectWork._3E.Application.Services;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Services;
using BlaisePascal.ProjectWork._3E.Infrastructure.Persistence;
using BlaisePascal.ProjectWork._3E.Infrastructure.Repositories;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Services
{
    /// <summary>
    /// Orchestrator che collega i dati importati (DatiImportatiDto) con
    /// il DistribuzioneClassiService basato su EF Core.
    ///
    /// Flusso:
    ///   1. Legge i DTO gia in memoria (DatiImportatiDto)
    ///   2. Mappa DTO -> entita di dominio (Studente) tramite StudenteMapper
    ///   3. Persiste gli studenti in EF Core (SQLite temporaneo, isolato per run)
    ///   4. Esegue DistribuzioneClassiService.DistribuisciAsync()
    ///   5. Restituisce la matrice classi-studenti al chiamante (WPF)
    /// </summary>
    public class DistribuzioneUseCase
    {
        /// <summary>
        /// Esegue la distribuzione completa e restituisce le classi formate.
        /// Ogni elemento della lista esterna e una classe; ogni elemento interno e uno studente.
        /// </summary>
        /// <param name="opzioni">Opzioni di distribuzione (numero classi, limiti, ecc.)</param>
        /// <param name="opzioniMatcher">Opzioni fuzzy per il matching delle preferenze</param>
        public async Task<List<List<Studente>>> EseguiAsync(
            OpzioniDistribuzione? opzioni = null,
            OpzioniMatcher? opzioniMatcher = null)
        {
            // STEP 1: Verifica dati in ingresso
            if (DatiImportatiDto.Alunni == null || DatiImportatiDto.Alunni.Count == 0)
                throw new InvalidOperationException(
                    "Nessun alunno importato. Eseguire prima l'importazione del file Excel.");

            // STEP 2: Mapping DTO -> entita di dominio
            var studentiDominio = StudenteMapper.MappaStudenti(
                DatiImportatiDto.Alunni,
                DatiImportatiDto.PreferenzeCompagni,
                DatiImportatiDto.Scuole);

            if (studentiDominio.Count == 0)
                throw new InvalidOperationException(
                    "Nessuno studente e stato convertito correttamente. Verificare il formato del file Excel.");

            // STEP 3: Configura AppDbContext con SQLite su file temporaneo
            // Ogni esecuzione usa un file DB diverso per evitare conflitti con studenti.db
            var tempDbPath = Path.Combine(Path.GetTempPath(), $"dist_{Guid.NewGuid():N}.db");
            var connString = $"Data Source={tempDbPath}";
            var dbOptions  = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connString)
                .Options;

            await using var context = new AppDbContext(dbOptions);
            await context.Database.EnsureCreatedAsync();

            // STEP 4: Persiste gli studenti nel context EF Core
            await context.Studenti.AddRangeAsync(studentiDominio);
            await context.SaveChangesAsync();

            // STEP 5: Prepara i repository e il servizio di distribuzione
            var studenteRepo = new StudenteRepository(context);
            var classeRepo   = new ClasseRepository(context);

            var service = new DistribuzioneClassiService(
                studenteRepo,
                classeRepo,
                opzioniMatcher);

            // STEP 6: Costruisce le opzioni se non fornite esternamente
            //    SezioniPerIndirizzo viene popolato dai contatori inseriti nella WPF
            //    e gia salvati in DatiImportatiDto durante BtnImport_Click
            opzioni ??= CostruisciOpzioniDaDatiImportati();

            // STEP 7: Esegue la distribuzione OR-Tools CP-SAT
            var risultato = await service.DistribuisciAsync(opzioni);

            // Esponi i match incerti per eventuale visualizzazione nella UI
            MatchIncerti = service.MatchIncerti;

            // Pulizia del file temporaneo (best-effort)
            try { File.Delete(tempDbPath); } catch { /* non critico */ }

            return risultato;
        }

        /// <summary>Ultimi match incerti (scelta compagno non risolta con certezza).</summary>
        public IReadOnlyList<RisultatoMatch> MatchIncerti { get; private set; } =
            Array.Empty<RisultatoMatch>();

        // Helper: costruisce OpzioniDistribuzione dai dati gia nel DTO globale

        private static OpzioniDistribuzione CostruisciOpzioniDaDatiImportati()
        {
            var sezioni = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (DatiImportatiDto.NumeroClassiInformatica > 0)
                sezioni["Informatica"] = DatiImportatiDto.NumeroClassiInformatica;

            if (DatiImportatiDto.NumeroClassiAutomazione > 0)
                sezioni["Automazione"] = DatiImportatiDto.NumeroClassiAutomazione;

            if (DatiImportatiDto.NumeroClassiBiotecnologie > 0)
                sezioni["Bio"] = DatiImportatiDto.NumeroClassiBiotecnologie;

            return new OpzioniDistribuzione
            {
                SezioniPerIndirizzo = sezioni,
                UsaPreferenze       = true
            };
        }
    }
}
