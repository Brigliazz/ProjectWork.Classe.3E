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

        // Costanti P1
        private const int MaxStudentiSenzaDisabilita = 27;
        private const int MaxStudentiConDisabilita = 20;
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

        
        public void AggiungiStudente(Studente.Studente studente)
        {
            // Invariante P1: max 1 studente con disabilità per classe
            if (studente.ProfiloBES.HasDisabilita)
            {
                if (HasStudenteConDisabilita)
                    throw new DomainException(
                        $"La classe {Sezione.Valore} ha già uno studente con disabilità. Max {MaxStudentiConDisabilitaPerClasse} per classe.");

                // Se aggiungiamo un disabile, il limite scende a 20
                if (NumeroStudenti >= MaxStudentiConDisabilita)
                    throw new DomainException(
                        $"La classe {Sezione.Valore} ha già {NumeroStudenti} studenti. Con uno studente con disabilità il massimo è {MaxStudentiConDisabilita}.");

                HasStudenteConDisabilita = true;
            }
            else
            {
                // Controlla il limite corretto in base alla presenza di studenti con disabilità
                int limite = HasStudenteConDisabilita ? MaxStudentiConDisabilita : MaxStudentiSenzaDisabilita;
                if (NumeroStudenti >= limite)
                    throw new DomainException(
                        $"La classe {Sezione.Valore} ha raggiunto il limite di {limite} studenti.");
            }

            _studentiIds.Add(studente.Id);
            studente.AssegnaAClasse(Id);
        }
    }
}
