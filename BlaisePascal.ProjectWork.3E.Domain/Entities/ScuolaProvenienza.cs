using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Entities
{
    public class ScuolaProvenienza
    {
        public Guid Id { get; private set; }
        public string CodiceScuola { get; private set; }   // Codice meccanografico
        public string NomeScuola { get; private set; }
        public string ComuneScuola { get; private set; }

        // Costruttore EF Core
        private ScuolaProvenienza() { }

        private ScuolaProvenienza(Guid id, string codiceScuola, string nomeScuola, string comuneScuola)
        {
            Id = id;
            CodiceScuola = codiceScuola;
            NomeScuola = nomeScuola;
            ComuneScuola = comuneScuola;
        }

        public static ScuolaProvenienza Crea(string codiceScuola, string nomeScuola, string comuneScuola)
        {
            if (string.IsNullOrWhiteSpace(codiceScuola))
                throw new DomainException("Il codice scuola è obbligatorio.");
            if (string.IsNullOrWhiteSpace(nomeScuola))
                throw new DomainException("Il nome scuola è obbligatorio.");
            if (string.IsNullOrWhiteSpace(comuneScuola))
                throw new DomainException("Il comune scuola è obbligatorio.");

            return new ScuolaProvenienza(Guid.NewGuid(), codiceScuola, nomeScuola, comuneScuola);
        }
    }
}
