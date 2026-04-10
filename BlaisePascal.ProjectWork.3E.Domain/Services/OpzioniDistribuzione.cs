using System;

namespace BlaisePascal.ProjectWork._3E.Domain.Services
{
    public class OpzioniDistribuzione
    {
        public int LimiteStandard { get; set; } = 27;
        public int LimiteDisabili { get; set; } = 20;


        // Se impostato a true, le classi permetteranno un sovraffollamento oltre i limiti di legge, 
        // bypassando i check vincolanti e simulando aule più ampie nel calcolo della loro capienza residua.

        public bool ConsentiSforo { get; set; } = false;

        // In modalità "Sforo", questo limite pone un tetto rigido invalicabile per il solver (es. capienza fisica aula).
        public int LimiteMassimoAssoluto { get; set; } = 32;

        public bool UsaPreferenze { get; set; } = true;

        public Dictionary<string, int> SezioniPerIndirizzo { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static OpzioniDistribuzione Default => new OpzioniDistribuzione();

        public OpzioniDistribuzione Clone()
        {
            return new OpzioniDistribuzione
            {
                LimiteStandard = this.LimiteStandard,
                LimiteDisabili = this.LimiteDisabili,
                ConsentiSforo = this.ConsentiSforo,
                LimiteMassimoAssoluto = this.LimiteMassimoAssoluto,
                UsaPreferenze = this.UsaPreferenze,
                SezioniPerIndirizzo = new Dictionary<string, int>(this.SezioniPerIndirizzo, StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}
