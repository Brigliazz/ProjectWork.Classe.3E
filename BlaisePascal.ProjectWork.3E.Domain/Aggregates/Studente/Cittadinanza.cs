using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente
{
    public sealed record Cittadinanza
    {
        // Codice ISTAT — 200 = Italia (da verificare nel dataset reale)
        private const int CodiceItalia = 200;

        public int Codice { get; }

        // Proprietà derivate
        public bool IsItaliana => Codice == CodiceItalia;
        public bool IsStraniera => !IsItaliana;

        private Cittadinanza(int codice)
        {
            Codice = codice;
        }

        public static Cittadinanza Crea(int codice)
        {
            if (codice <= 0)
                throw new DomainException($"Codice cittadinanza non valido: {codice}.");

            return new Cittadinanza(codice);
        }

        // Factory method semantico
        public static Cittadinanza Italiana => Crea(CodiceItalia);
    }
}
