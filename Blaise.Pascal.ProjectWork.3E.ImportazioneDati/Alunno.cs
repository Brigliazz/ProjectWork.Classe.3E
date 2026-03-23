using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Linq;

namespace Importazione_Dati_Excel
{
    // --- DEFINIZIONE DELLE CLASSI ---
    public class Alunno
    {
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public bool Sesso { get; set; }
        public string CodiceFiscale { get; set; }
        public string Cittadinanza { get; set; }
        public string ComuneResidenza { get; set; }
        public bool Disabilita { get; set; }
        public bool Dsa { get; set; }
        public string Indirizzo { get; set; }
    }
}