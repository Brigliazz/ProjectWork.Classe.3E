using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente
{
    public sealed record SceltaCompagno
    {
        public string Testo { get; }

        private SceltaCompagno(string testo)
        {
            Testo = testo;
        }

        public static SceltaCompagno Crea(string testo)
        {
            if (string.IsNullOrWhiteSpace(testo))
                throw new DomainException("La scelta compagno non può essere vuota.");

            return new SceltaCompagno(testo);
        }
    }
}
