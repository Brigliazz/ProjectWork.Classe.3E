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

        public bool UsaPreferenze { get; set; } = true;

        public Dictionary<string, int> SezioniPerIndirizzo { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static OpzioniDistribuzione Default => new OpzioniDistribuzione();
    }
}
