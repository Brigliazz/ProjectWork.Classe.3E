using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Application.ImportModels
{
    public class StudenteImportDto
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
        public int VotoEsameTerzaMedia { get; set; } 
        public bool FaReligione { get; set; }
        public string DataArrivoInItalia { get; set; } 
        public string DataDiNascita { get; set; }
        public bool DisabilitaAssistenzaBase { get; set; }
        public string? PreferenzaCompagno { get; set; }

        public int Id { get; set; }
    }
}
