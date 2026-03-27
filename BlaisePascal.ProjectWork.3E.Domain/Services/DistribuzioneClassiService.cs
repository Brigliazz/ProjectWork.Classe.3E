using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Exceptions;
using BlaisePascal.ProjectWork._3E.Domain.Repositories;
using BlaisePascal.ProjectWork._3E.Domain.Enums;

namespace BlaisePascal.ProjectWork._3E.Domain.Services
{
    public class DistribuzioneClassiService
    {

        private readonly IStudenteRepository _studenteRepository;
        private readonly IClasseRepository _classeRepository;

        public DistribuzioneClassiService(
            IStudenteRepository studenteRepository,
            IClasseRepository classeRepository)
        {
            _studenteRepository = studenteRepository;
            _classeRepository = classeRepository;
        }

        /// <summary>
        /// Esegue la distribuzione degli studenti nelle classi in 3 fasi:
        /// F1 — Scheletro delle Classi (disabili)
        /// F2 — Distribuzione Ragazze
        /// F3 — Distribuzione Maschi + bilanciamento soft
        /// Restituisce una matrice (lista di liste) con gli studenti per ogni classe.
        /// </summary>
        public async Task<List<List<Studente>>> DistribuisciAsync(OpzioniDistribuzione? opzioni = null)
        {
            opzioni ??= OpzioniDistribuzione.Default;

            var studenti = await _studenteRepository.GetNonAssegnatiAsync();
            var classi = await _classeRepository.GetAllAsync();

            if (classi.Count == 0)
                throw new DomainException("Non ci sono classi disponibili per la distribuzione.");

            if (studenti.Count == 0)
                return new List<List<Studente>>();

            // Pool mutabile: man mano che assegniamo uno studente, lo rimuoviamo
            var pool = new List<Studente>(studenti);

            // 
            //  F1 — SCHELETRO DELLE CLASSI (Disabili)
            // 
            F1_AssegnaDisabili(pool, classi, opzioni);

            // 
            //  F2 — DISTRIBUZIONE RAGAZZE
            // 
            F2_DistribuisciRagazze(pool, classi, opzioni);

            // 
            //  F3 — DISTRIBUZIONE MASCHI + BILANCIAMENTO SOFT
            // 
            F3_DistribuisciMaschiEBilancia(pool, classi, studenti, opzioni);

            // Salva tutto
            await _studenteRepository.SaveChangesAsync();
            await _classeRepository.SaveChangesAsync();

            // Creazione della matrice (lista di liste) degli studenti per classe
            var tuttiStudentiAssegnati = await _studenteRepository.GetAllAsync();
            var matriceClassi = new List<List<Studente>>();

            foreach (var classe in classi.OrderBy(c => c.Sezione.Valore))
            {
                var studentiDellaClasse = tuttiStudentiAssegnati
                    .Where(s => s.ClasseId == classe.Id)
                    .ToList();
                matriceClassi.Add(studentiDellaClasse);
            }

            return matriceClassi;
        }

        // 
        //  F1 — Scheletro delle Classi
        // 
        private void F1_AssegnaDisabili(List<Studente> pool, List<ClassePrima> classi, OpzioniDistribuzione opzioni)
        {
            var disabili = pool
                .Where(s => s.ProfiloBES.HasDisabilita)
                .ToList();

            // Precondizione hard protetta da eventuale deroga
            if (!opzioni.ConsentiSforo && disabili.Count > classi.Count)
                throw new DomainException(
                    $"Impossibile soddisfare P1: {disabili.Count} studenti con disabilità " +
                    $"ma solo {classi.Count} classi disponibili.");

            // Assegna un disabile per classe (classe protetta, cap. 20)
            foreach (var studente in disabili)
            {
                var classeTarget = classi
                    .OrderBy(c => c.HasStudenteConDisabilita ? 1 : 0) // Prima quelle senza disabili
                    .ThenBy(c => c.NumeroStudenti)
                    .First();

                classeTarget.AggiungiStudente(studente, opzioni);
                pool.Remove(studente);
            }
        }

        // 
        //  F2 — Distribuzione Ragazze
        // 
        private void F2_DistribuisciRagazze(List<Studente> pool, List<ClassePrima> classi, OpzioniDistribuzione opzioni)
        {
            var ragazze = pool
                .Where(s => s.Sesso == Sesso.Femmina)
                .ToList();

            if (ragazze.Count == 0)
                return;

            // Caso speciale: meno di 2 ragazze → impossibile garantire non-isolamento
            if (ragazze.Count < 2)
            {
                // Logga warning e assegna comunque alla classe più vuota
                var classeTarget = classi.OrderBy(c => c.NumeroStudenti).First();
                classeTarget.AggiungiStudente(ragazze[0], opzioni);
                pool.Remove(ragazze[0]);
                return;
            }

            // Calcola dimensione target del gruppo
            int targetDim = Math.Clamp(ragazze.Count / classi.Count, 2, 4);

            // Ordina per scuola di provenienza per non separare compagne
            var ragOrdinate = ragazze
                .OrderBy(s => s.CodiceScuolaProvenienza)
                .ToList();

            // Crea i gruppi sequenzialmente
            var gruppi = new List<List<Studente>>();
            for (int i = 0; i < ragOrdinate.Count; i += targetDim)
            {
                var gruppo = ragOrdinate.Skip(i).Take(targetDim).ToList();
                gruppi.Add(gruppo);
            }

            // Gestione residuo
            if (gruppi.Count > 1)
            {
                var ultimo = gruppi[^1];
                if (ultimo.Count == 1)
                {
                    // Residuo == 1 → aggiungi all'ultimo gruppo esistente
                    var penultimo = gruppi[^2];
                    penultimo.Add(ultimo[0]);
                    gruppi.RemoveAt(gruppi.Count - 1);
                }
                // Residuo >= 2 → va bene così (nuovo gruppo)
                // Residuo == 0 → nessuna azione
            }

            // Distribuisci un gruppo per classe, partendo dalla classe con più posti
            foreach (var gruppo in gruppi)
            {
                var classeTarget = classi
                    .OrderByDescending(c => c.OttieniCapienzaResidua(opzioni))
                    .First();

                foreach (var ragazza in gruppo)
                {
                    classeTarget.AggiungiStudente(ragazza, opzioni);
                    pool.Remove(ragazza);
                }
            }
        }

        // 
        //  F3 — Distribuzione Maschi + Bilanciamento Soft
        // 
        private void F3_DistribuisciMaschiEBilancia(
            List<Studente> pool, List<ClassePrima> classi, List<Studente> tuttiStudenti, OpzioniDistribuzione opzioni)
        {
            // ── 3a. Raggruppa maschi rimanenti per scuola di provenienza ──
            var clusterPerScuola = pool
                .GroupBy(s => s.CodiceScuolaProvenienza)
                .OrderByDescending(g => g.Count()) // cluster grandi prima
                .ToList();

            // Calcoliamo la capienza ideale per mantenere il bilanciamento numerico
            int capienzaIdeale = (int)Math.Ceiling((double)tuttiStudenti.Count / classi.Count);

            foreach (var cluster in clusterPerScuola)
            {
                var studentiCluster = cluster.ToList();

                while (studentiCluster.Count > 0)
                {
                    // Ordina prima per posti liberi rispetto alla capienza ideale, poi per capienza residua assoluta
                    var classeTarget = classi
                        .OrderByDescending(c => Math.Max(0, capienzaIdeale - c.NumeroStudenti))
                        .ThenByDescending(c => c.OttieniCapienzaResidua(opzioni))
                        .First();

                    if (classeTarget.OttieniCapienzaResidua(opzioni) <= 0)
                        break; // tutte le classi sono piene (improbabile se in sforo)

                    // Posti liberi fino alla capienza ideale (almeno 1 se la classe non è già oltre l'ideale)
                    int postiIdeali = Math.Max(1, capienzaIdeale - classeTarget.NumeroStudenti);
                    postiIdeali = Math.Min(postiIdeali, classeTarget.OttieniCapienzaResidua(opzioni));

                    // Quanti ne possiamo inserire in questa classe (massimo postiIdeali)
                    int daInserire = Math.Min(studentiCluster.Count, postiIdeali);

                    for (int i = 0; i < daInserire; i++)
                    {
                        classeTarget.AggiungiStudente(studentiCluster[0], opzioni);
                        pool.Remove(studentiCluster[0]);
                        studentiCluster.RemoveAt(0);
                    }
                }
            }

            // ── 3b. Bilanciamento soft ──
            // Ordine: scuola provenienza → stranieri → DSA → IRC → eccellenze
            BilanciaSoft_ScuolaProvenienza(classi, tuttiStudenti, opzioni);
            BilanciaSoft_Stranieri(classi, tuttiStudenti, opzioni);
            BilanciaSoft_Attributo(classi, tuttiStudenti, s => s.ProfiloBES.HasDSA, "DSA", opzioni, 
                escludi: s => s.IsStraniero);
            BilanciaSoft_Attributo(classi, tuttiStudenti, s => s.FaReligione, "IRC", opzioni, 
                escludi: s => s.IsStraniero || s.ProfiloBES.HasDSA);
            BilanciaSoft_Attributo(classi, tuttiStudenti, s => s.IsEccellenza, "Eccellenza", opzioni, 
                escludi: s => s.IsStraniero || s.ProfiloBES.HasDSA || s.FaReligione);
        }

        // 
        //  Bilanciamento Stranieri (max 30% per classe + distribuzione
        //  uniforme finché Δ > 1)
        // 
        private void BilanciaSoft_Stranieri(List<ClassePrima> classi, List<Studente> tuttiStudenti, OpzioniDistribuzione opzioni)
        {
            while (true)
            {
                var contiPerClasse = classi
                    .Select(c => new
                    {
                        Classe = c,
                        Count = tuttiStudenti.Count(s => s.ClasseId == c.Id && s.IsStraniero)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                int maxCount = contiPerClasse.First().Count;
                int minCount = contiPerClasse.Last().Count;

                if (maxCount - minCount <= 1)
                    break;

                var sorgente = contiPerClasse.First();
                var destinazione = contiPerClasse.Last();

                // Trova un candidato straniero da spostare (mai spostare un disabile o ragazza)
                // Evitiamo di spostare ragazze per non rompere i gruppi F2
                var candidatoOut = tuttiStudenti
                    .Where(s => s.ClasseId == sorgente.Classe.Id
                                && s.IsStraniero
                                && !s.ProfiloBES.HasDisabilita
                                && s.Sesso == Sesso.Maschio)
                    .FirstOrDefault();

                if (candidatoOut == null)
                    break;

                // Trova un candidato non-straniero nella destinazione da scambiare
                var candidatoIn = tuttiStudenti
                    .Where(s => s.ClasseId == destinazione.Classe.Id
                                && !s.IsStraniero
                                && !s.ProfiloBES.HasDisabilita
                                && s.Sesso == Sesso.Maschio)
                    .FirstOrDefault();

                // Se non c'è nessuno da scambiare (improbabile), proviamo un semplice spostamento se c'è posto
                if (candidatoIn == null)
                {
                    if (destinazione.Classe.OttieniCapienzaResidua(opzioni) > 0)
                    {
                        int strDest = tuttiStudenti.Count(s => s.ClasseId == destinazione.Classe.Id && s.IsStraniero);
                        if ((strDest + 1.0) / (destinazione.Classe.NumeroStudenti + 1) <= 0.30)
                        {
                            sorgente.Classe.RimuoviStudente(candidatoOut);
                            destinazione.Classe.AggiungiStudente(candidatoOut, opzioni);
                            continue;
                        }
                    }
                    break; // Non possiamo fare nulla
                }

                // Verifica vincolo 30% sulla destinazione dopo lo swap
                // La dimensione della classe non cambia, quindi controlliamo solo il conteggio
                int strDestDopoSwap = minCount + 1;
                if ((double)strDestDopoSwap / destinazione.Classe.NumeroStudenti > 0.30)
                    break;

                // Esegui lo swap
                sorgente.Classe.RimuoviStudente(candidatoOut);
                destinazione.Classe.RimuoviStudente(candidatoIn);
                
                destinazione.Classe.AggiungiStudente(candidatoOut, opzioni);
                sorgente.Classe.AggiungiStudente(candidatoIn, opzioni);
            }
        }

        // 
        //  Bilanciamento generico per attributo booleano
        //  (DSA, IRC, Eccellenze)
        //  Sposta dalla classe con più alla classe con meno finché Δ > 1
        // 
        private void BilanciaSoft_Attributo(
            List<ClassePrima> classi, List<Studente> tuttiStudenti,
            Func<Studente, bool> predicate, string nome, OpzioniDistribuzione opzioni,
            Func<Studente, bool>? escludi = null)
        {
            // Se tutti o nessuno degli studenti hanno l'attributo, il bilanciamento è inutile
            int totaleConAttributo = tuttiStudenti.Count(s => s.ClasseId != null && predicate(s));
            int totaleAssegnati = tuttiStudenti.Count(s => s.ClasseId != null);
            if (totaleConAttributo == 0 || totaleConAttributo == totaleAssegnati)
                return;

            // Continua finché max - min > 1
            while (true)
            {
                // Ricalcola i conteggi ad ogni iterazione
                var contiPerClasse = classi
                    .Select(c => new
                    {
                        Classe = c,
                        Count = tuttiStudenti.Count(s => s.ClasseId == c.Id && predicate(s))
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                int maxCount = contiPerClasse.First().Count;
                int minCount = contiPerClasse.Last().Count;

                // Tolleranza: swap solo se differenza > 1
                if (maxCount - minCount <= 1)
                    break;

                var sorgente = contiPerClasse.First();
                var destinazione = contiPerClasse.Last();

                // Trova un candidato da spostare (mai spostare un disabile o ragazza o un protetto)
                var candidatoOut = tuttiStudenti
                    .Where(s => s.ClasseId == sorgente.Classe.Id
                                && predicate(s)
                                && (escludi == null || !escludi(s))
                                && !s.ProfiloBES.HasDisabilita
                                && s.Sesso == Sesso.Maschio)
                    .FirstOrDefault();

                if (candidatoOut == null)
                    break;

                // Trova un candidato senza l'attributo nella destinazione
                var candidatoIn = tuttiStudenti
                    .Where(s => s.ClasseId == destinazione.Classe.Id
                                && !predicate(s)
                                && (escludi == null || !escludi(s))
                                && !s.ProfiloBES.HasDisabilita
                                && s.Sesso == Sesso.Maschio)
                    .FirstOrDefault();

                if (candidatoIn == null)
                {
                    // Fallback a spostamento semplice se possibile
                    if (destinazione.Classe.OttieniCapienzaResidua(opzioni) > 0)
                    {
                        sorgente.Classe.RimuoviStudente(candidatoOut);
                        destinazione.Classe.AggiungiStudente(candidatoOut, opzioni);
                        continue;
                    }
                    break;
                }

                // Swap
                sorgente.Classe.RimuoviStudente(candidatoOut);
                destinazione.Classe.RimuoviStudente(candidatoIn);
                
                destinazione.Classe.AggiungiStudente(candidatoOut, opzioni);
                sorgente.Classe.AggiungiStudente(candidatoIn, opzioni);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Bilanciamento per Scuola di Provenienza
        //  Per ogni codice scuola presente, bilancia il numero di studenti
        //  provenienti da quella scuola tra le classi (swap finché Δ > 1)
        // ─────────────────────────────────────────────────────────────────
        private void BilanciaSoft_ScuolaProvenienza(
            List<ClassePrima> classi, List<Studente> tuttiStudenti, OpzioniDistribuzione opzioni)
        {
            // Individua tutte le scuole di provenienza distinte
            var codiciScuola = tuttiStudenti
                .Where(s => s.ClasseId != null)
                .Select(s => s.CodiceScuolaProvenienza)
                .Distinct()
                .ToList();

            foreach (var codice in codiciScuola)
            {
                // Conta quanti studenti di questa scuola ci sono in totale
                int totaleScuola = tuttiStudenti.Count(s => s.ClasseId != null && s.CodiceScuolaProvenienza == codice);
                
                // Se sono pochi (meno di 2 per classe in media), il bilanciamento non ha senso
                if (totaleScuola < classi.Count)
                    continue;

                int maxIterazioni = 100; // Limite di sicurezza anti-loop infinito
                int iterazione = 0;

                while (iterazione++ < maxIterazioni)
                {
                    var contiPerClasse = classi
                        .Select(c => new
                        {
                            Classe = c,
                            Count = tuttiStudenti.Count(s => s.ClasseId == c.Id && s.CodiceScuolaProvenienza == codice)
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    int maxCount = contiPerClasse.First().Count;
                    int minCount = contiPerClasse.Last().Count;

                    // Tolleranza: swap solo se differenza > 1
                    if (maxCount - minCount <= 1)
                        break;

                    var sorgente = contiPerClasse.First();
                    var destinazione = contiPerClasse.Last();

                    // Trova un candidato dalla scuola da spostare (mai spostare disabili o ragazze)
                    var candidatoOut = tuttiStudenti
                        .Where(s => s.ClasseId == sorgente.Classe.Id
                                    && s.CodiceScuolaProvenienza == codice
                                    && !s.ProfiloBES.HasDisabilita
                                    && s.Sesso == Sesso.Maschio)
                        .FirstOrDefault();

                    if (candidatoOut == null)
                        break;

                    // Trova un candidato di un'ALTRA scuola nella destinazione da scambiare
                    var candidatoIn = tuttiStudenti
                        .Where(s => s.ClasseId == destinazione.Classe.Id
                                    && s.CodiceScuolaProvenienza != codice
                                    && !s.ProfiloBES.HasDisabilita
                                    && s.Sesso == Sesso.Maschio)
                        .FirstOrDefault();

                    if (candidatoIn == null)
                    {
                        // Fallback: spostamento semplice se c'è posto
                        if (destinazione.Classe.OttieniCapienzaResidua(opzioni) > 0)
                        {
                            sorgente.Classe.RimuoviStudente(candidatoOut);
                            destinazione.Classe.AggiungiStudente(candidatoOut, opzioni);
                            continue;
                        }
                        break;
                    }

                    // Esegui lo swap
                    sorgente.Classe.RimuoviStudente(candidatoOut);
                    destinazione.Classe.RimuoviStudente(candidatoIn);

                    destinazione.Classe.AggiungiStudente(candidatoOut, opzioni);
                    sorgente.Classe.AggiungiStudente(candidatoIn, opzioni);
                }
            }
        }
    }
}
