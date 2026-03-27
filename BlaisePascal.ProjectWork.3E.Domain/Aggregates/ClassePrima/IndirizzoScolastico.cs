using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima
{
    public sealed record IndirizzoScolastico
    {
        public static readonly IndirizzoScolastico Automazione = new("Automazione");
        public static readonly IndirizzoScolastico Informatica = new("Informatica");
        public static readonly IndirizzoScolastico Bio = new("Bio");

        private static readonly Dictionary<string, IndirizzoScolastico> ValoriAmmessi =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Automazione", Automazione },
                { "Informatica", Informatica },
                { "Bio", Bio }
            };

        // Mapping sezione → indirizzo
        private static readonly Dictionary<string, string> SezioneToIndirizzo =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "A", "Automazione" }, { "B", "Automazione" }, { "C", "Automazione" }, { "D", "Automazione" },
                { "E", "Informatica" }, { "F", "Informatica" }, { "G", "Informatica" }, { "H", "Informatica" },
                { "I", "Informatica" }, { "L", "Informatica" }, { "M", "Informatica" }, { "N", "Informatica" },
                { "O", "Informatica" },
                { "Bio", "Bio" }
            };

        public string Nome { get; }

        private IndirizzoScolastico(string nome)
        {
            Nome = nome;
        }

        public static IndirizzoScolastico Crea(string valore)
        {
            if (string.IsNullOrWhiteSpace(valore))
                throw new DomainException("L'indirizzo scolastico non può essere vuoto.");

            if (!ValoriAmmessi.TryGetValue(valore, out var indirizzo))
                throw new DomainException($"Indirizzo '{valore}' non valido. Valori ammessi: Automazione, Informatica, Bio.");

            return indirizzo;
        }

        
        public bool IsCoerente(Sezione sezione)
        {
            if (!SezioneToIndirizzo.TryGetValue(sezione.Valore, out var indirizzoAtteso))
                return false;

            return string.Equals(Nome, indirizzoAtteso, StringComparison.OrdinalIgnoreCase);
        }

        public static IndirizzoScolastico DaSezione(Sezione sezione)
        {
            if (!SezioneToIndirizzo.TryGetValue(sezione.Valore, out var indirizzoNome))
                throw new DomainException($"Impossibile determinare l'indirizzo per la sezione {sezione.Valore}");

            return Crea(indirizzoNome);
        }
    }
}
