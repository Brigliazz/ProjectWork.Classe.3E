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

       
        public async Task DistribuisciAsync()
        {
            var studenti = await _studenteRepository.GetNonAssegnatiAsync();
            var classi = await _classeRepository.GetAllAsync();

            if (classi.Count == 0)
                throw new DomainException("Non ci sono classi disponibili per la distribuzione.");

            if (studenti.Count == 0)
                return;

            // ─── P1: VINCOLI HARD ───────────────────────────────────
            // Distribuisce prima gli studenti con disabilità: max 1 per classe, riduce limite a 20
            var studentiConDisabilita = studenti
                .Where(s => s.ProfiloBES.HasDisabilita)
                .ToList();

            foreach (var studente in studentiConDisabilita)
            {
                var classeDisponibile = classi
                    .Where(c => !c.HasStudenteConDisabilita)
                    .OrderBy(c => c.NumeroStudenti)
                    .FirstOrDefault();

                if (classeDisponibile == null)
                    throw new DomainException(
                        "Impossibile soddisfare i vincoli P1: non ci sono classi disponibili per studenti con disabilità.");

                classeDisponibile.AggiungiStudente(studente);
            }

            // ─── P2: BILANCIAMENTO ──────────────────────────────────
            var studentiRimanenti = studenti
                .Where(s => s.Stato == StatoAssegnazione.NonAssegnato)
                .ToList();

            // P2.1 — Ragazze in gruppi di 3/4 (no isolamento)
            var ragazze = studentiRimanenti
                .Where(s => s.Sesso == Sesso.Femmina)
                .ToList();

            foreach (var classe in classi.OrderBy(c => c.NumeroStudenti))
            {
                var gruppoRagazze = ragazze.Take(3).ToList();
                if (gruppoRagazze.Count < 3 && ragazze.Count > 0)
                    gruppoRagazze = ragazze.Take(ragazze.Count).ToList();

                foreach (var ragazza in gruppoRagazze)
                {
                    try
                    {
                        classe.AggiungiStudente(ragazza);
                        ragazze.Remove(ragazza);
                        studentiRimanenti.Remove(ragazza);
                    }
                    catch (DomainException)
                    {
                        // Classe piena, prova la prossima
                        break;
                    }
                }
            }

            // P2.2 — DSA distribuiti uniformemente
            var studentiDSA = studentiRimanenti
                .Where(s => s.ProfiloBES.HasDSA)
                .ToList();

            foreach (var studente in studentiDSA)
            {
                var classeMinDsa = classi
                    .OrderBy(c => c.NumeroStudenti)
                    .FirstOrDefault();

                if (classeMinDsa != null)
                {
                    try
                    {
                        classeMinDsa.AggiungiStudente(studente);
                        studentiRimanenti.Remove(studente);
                    }
                    catch (DomainException) { }
                }
            }

            // P2.3 — Stranieri distribuiti uniformemente
            var stranieri = studentiRimanenti
                .Where(s => s.IsStraniero)
                .ToList();

            foreach (var studente in stranieri)
            {
                var classeMinStranieri = classi
                    .OrderBy(c => c.NumeroStudenti)
                    .FirstOrDefault();

                if (classeMinStranieri != null)
                {
                    try
                    {
                        classeMinStranieri.AggiungiStudente(studente);
                        studentiRimanenti.Remove(studente);
                    }
                    catch (DomainException) { }
                }
            }

            // ─── P3: UNIFORMITÀ ─────────────────────────────────────
            // Distribuisce gli studenti rimanenti bilanciando per numero
            foreach (var studente in studentiRimanenti.ToList())
            {
                var classePiuVuota = classi
                    .OrderBy(c => c.NumeroStudenti)
                    .FirstOrDefault();

                if (classePiuVuota != null)
                {
                    try
                    {
                        classePiuVuota.AggiungiStudente(studente);
                    }
                    catch (DomainException) { }
                }
            }

            
            await _studenteRepository.SaveChangesAsync();
            await _classeRepository.SaveChangesAsync();
        }
    }
}
