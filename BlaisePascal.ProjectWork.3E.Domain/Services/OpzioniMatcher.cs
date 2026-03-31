using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Domain.Services
{
    // OpzioniMatcher
    //
    // Soglie configurabili per la classificazione dei match.
    // I valori di default sono conservativi per minimizzare i falsi positivi.
    public class OpzioniMatcher
    {
        // Score FuzzySharp (0–100) sopra il quale il match è considerato certo
        public int SogliaMatchCerto { get; set; } = 90;

        // Score tra SogliaMatchIncerto e SogliaMatchCerto → richiede revisione umana
        public int SogliaMatchIncerto { get; set; } = 70;

        // Istanza di default
        public static OpzioniMatcher Default => new();
    }
}
