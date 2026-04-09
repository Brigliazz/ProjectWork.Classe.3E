using System;
using System.Collections.Generic;
using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Aggregates.ClassePrima
{
    public sealed record Sezione
    {
        // Valori ammessi per sezione
        private static readonly HashSet<string> ValoriAmmessi = new(StringComparer.OrdinalIgnoreCase)
        {
            "A", "B", "C", "D",                         // Automazione
            "E", "F", "G", "H", "I", "L", "M", "N", "O", // Informatica
            "Bio"                                         // Bio
        };

        public string Valore { get; private set; }

        private Sezione() { Valore = string.Empty; } // for EF Core

        private Sezione(string valore)
        {
            Valore = valore;
        }

        public static Sezione Crea(string valore)
        {
            if (string.IsNullOrWhiteSpace(valore))
                throw new DomainException("Il valore della sezione non può essere vuoto.");

            if (!ValoriAmmessi.Contains(valore))
                throw new DomainException($"Sezione '{valore}' non ammessa. Valori validi: {string.Join(", ", ValoriAmmessi)}.");

            return new Sezione(valore);
        }
    }
}
