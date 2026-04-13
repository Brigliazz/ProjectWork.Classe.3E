using BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima;
using BlaisePascal.ProjectWork._3E.Domain.Enums;
using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente
{
    public class Studente
    {
        // Identità
        public Guid Id { get; private set; }
        public string CodiceFiscale { get; private set; }

        // Anagrafica
        public string Nome { get; private set; }
        public string Cognome { get; private set; }
        public Sesso Sesso { get; private set; }
        public DateOnly DataNascita { get; private set; }
        public DateOnly? DataArrivoItalia { get; private set; }

        // Value Objects
        public ProfiloBES ProfiloBES { get; private set; }
        public Cittadinanza Cittadinanza { get; private set; }
        public SceltaCompagno? SceltaCompagno { get; private set; }
        public IndirizzoScolastico IndirizzoScolastico { get; private set; }

        // Riferimenti
        public string CodiceScuolaProvenienza { get; private set; }

        // Criteri clustering
        public bool FaReligione { get; private set; }
        public int? VotoEsame { get; private set; }

        // Stato assegnazione
        public StatoAssegnazione Stato { get; private set; }
        public Guid? ClasseId { get; private set; }

        // Proprietà derivate
        public bool IsEccellenza => VotoEsame == 10;
        public bool IsStraniero => Cittadinanza.IsStraniera;

        // Costruttore EF Core
        private Studente() { }

        private Studente(Guid id, string nome, string cognome, Sesso sesso, string codiceFiscale, DateOnly dataNascita,
            DateOnly? dataArrivoItalia, Cittadinanza cittadinanza, string codiceScuolaProvenienza,
            ProfiloBES profiloBES, bool faReligione, int? votoEsame, SceltaCompagno? sceltaCompagno,
            IndirizzoScolastico indirizzoScolastico)
        {
            Id = id;
            Nome = nome;
            Cognome = cognome;
            Sesso = sesso;
            CodiceFiscale = codiceFiscale;
            DataNascita = dataNascita;
            DataArrivoItalia = dataArrivoItalia;
            Cittadinanza = cittadinanza;
            CodiceScuolaProvenienza = codiceScuolaProvenienza;
            ProfiloBES = profiloBES;
            FaReligione = faReligione;
            VotoEsame = votoEsame;
            SceltaCompagno = sceltaCompagno;
            IndirizzoScolastico = indirizzoScolastico;
            Stato = StatoAssegnazione.NonAssegnato;
            ClasseId = null;
        }

        public static Studente Crea(string nome, string cognome, Sesso sesso, string codiceFiscale, DateOnly dataNascita,
            DateOnly? dataArrivoItalia, Cittadinanza cittadinanza, string codiceScuolaProvenienza,
            ProfiloBES profiloBES, bool faReligione, int? votoEsame,
            SceltaCompagno? sceltaCompagno = null, IndirizzoScolastico? indirizzoScolastico = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new DomainException("Il nome è obbligatorio.");
            if (string.IsNullOrWhiteSpace(cognome))
                throw new DomainException("Il cognome è obbligatorio.");
            if (string.IsNullOrWhiteSpace(codiceFiscale))
                throw new DomainException("Il codice fiscale è obbligatorio.");

            return new Studente(Guid.NewGuid(), nome, cognome, sesso, codiceFiscale, dataNascita, dataArrivoItalia,
                cittadinanza, codiceScuolaProvenienza, profiloBES, faReligione, votoEsame, sceltaCompagno,
                indirizzoScolastico ?? IndirizzoScolastico.Informatica);
        }

       
        public void AssegnaAClasse(Guid classeId)
        {
            if (Stato == StatoAssegnazione.Assegnato)
                throw new DomainException($"Lo studente {Nome} {Cognome} è già assegnato a una classe.");

            ClasseId = classeId;
            Stato = StatoAssegnazione.Assegnato;
        }

       
        public void RimuoviDaClasse()
        {
            ClasseId = null;
            Stato = StatoAssegnazione.NonAssegnato;
        }
    }
}
