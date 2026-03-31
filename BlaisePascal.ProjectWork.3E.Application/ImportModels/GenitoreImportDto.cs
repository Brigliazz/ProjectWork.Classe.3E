using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Application.ImportModels
{
    public class GenitoreImportDTO
    {
        public string Numero { get; set; }
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string Mail { get; set; }
        public int Id { get; set; }

        public string CodiceFiscale { get; set; }
    }
}
