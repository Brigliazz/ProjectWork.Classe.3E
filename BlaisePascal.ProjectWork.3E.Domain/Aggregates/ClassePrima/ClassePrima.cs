using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima
{
    public class ClassePrima
    {
        // Identità
        public Guid Id { get; private set; }

        // Value Objects
        public Sezione Sezione { get; private set; }
        public IndirizzoScolastico Indirizzo { get; private set; }

        // Studenti assegnati — solo riferimenti, non oggetti interi
        private readonly List<Guid> _studentiIds = new();
        public IReadOnlyList<Guid> StudentiIds => _studentiIds.AsReadOnly();

        // Stato derivato
        public int NumeroStudenti => _studentiIds.Count;
        public bool HasStudenteConDisabilita { get; private set; }

        // I limiti P1 (es. 27, 20) sono ora dinamicizzati tramite OpzioniDistribuzione
        // passate come parametro ai metodi del dominio.
        private const int MaxStudentiConDisabilitaPerClasse = 1;

        // Costruttore EF Core
        private ClassePrima() { }

        private ClassePrima(Guid id, Sezione sezione, IndirizzoScolastico indirizzo)
        {
            Id = id;
            Sezione = sezione;
            Indirizzo = indirizzo;
            HasStudenteConDisabilita = false;
        }

        
        public static ClassePrima Crea(Sezione sezione, IndirizzoScolastico indirizzo)
        {
            if (!indirizzo.IsCoerente(sezione))
                throw new DomainException(
                    $"La sezione '{sezione.Valore}' non è coerente con l'indirizzo '{indirizzo.Nome}'.");

            return new ClassePrima(Guid.NewGuid(), sezione, indirizzo);
        }

        public void AggiungiStudente(Studente.Studente studente, BlaisePascal.ProjectWork._3E.Domain.Services.OpzioniDistribuzione opzioni)
        {
            // Invariante P1: max 1 studente con disabilità per classe
            if (studente.ProfiloBES.HasDisabilita)
            {
                if (HasStudenteConDisabilita)
                    throw new DomainException(
                        $"La classe {Sezione.Valore} ha già uno studente con disabilità. Max {MaxStudentiConDisabilitaPerClasse} per classe.");

                if (!opzioni.ConsentiSforo && NumeroStudenti >= opzioni.LimiteDisabili)
                    throw new DomainException(
                        $"La classe {Sezione.Valore} ha già {NumeroStudenti} studenti. Con uno studente con disabilità il massimo è {opzioni.LimiteDisabili}.");

                HasStudenteConDisabilita = true;
            }
            else
            {
                // Controlla il limite corretto se non siamo in modalità sforo
                if (!opzioni.ConsentiSforo)
                {
                    int limite = HasStudenteConDisabilita ? opzioni.LimiteDisabili : opzioni.LimiteStandard;
                    if (NumeroStudenti >= limite)
                        throw new DomainException(
                            $"La classe {Sezione.Valore} ha raggiunto il limite di {limite} studenti.");
                }
            }

            _studentiIds.Add(studente.Id);
            studente.AssegnaAClasse(Id);
        }


        /// Rimuove uno studente dalla classe (usato per swap in F3).

        public void RimuoviStudente(Studente.Studente studente)
        {
            if (!_studentiIds.Remove(studente.Id))
                throw new DomainException(
                    $"Lo studente {studente.Nome} {studente.Cognome} non appartiene alla classe {Sezione.Valore}.");

            studente.RimuoviDaClasse();

            // Se lo studente rimosso aveva una disabilità, aggiorna il flag
            if (studente.ProfiloBES.HasDisabilita)
                HasStudenteConDisabilita = false;
        }


        /// Posti liberi rimanenti nella classe. Accetta OpzioniDistribuzione per gestire lo sforo.

        public int OttieniCapienzaResidua(BlaisePascal.ProjectWork._3E.Domain.Services.OpzioniDistribuzione opzioni)
        {
            int limite = HasStudenteConDisabilita
                ? opzioni.LimiteDisabili
                : opzioni.LimiteStandard;

            if (opzioni.ConsentiSforo)
            {
                // In modalità sforo diamo un margine fittizio equo a tutte le classi,
                // così che chi ha meno alunni risulti sempre avere 'più posti liberi'
                // e la distribuzione continui a spalmare uniformemente l'eccedenza.
                limite = Math.Min(limite + 50, opzioni.LimiteMassimoSforo);
            }

            return limite - NumeroStudenti;
        }
    }
}
