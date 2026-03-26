using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Application.ImportModels
{
    public class DatiImportatiDto
    {
        public List<StudenteImportDto> Alunni { get; set; } = new List<StudenteImportDto>();
        public List<SceltaImportDto> Scelte { get; set; } = new List<SceltaImportDto>();
        public List<ScuolaProvImportDto> Scuole { get; set; } = new List<ScuolaProvImportDto>();
        public List<GenitoreImportDTO> Genitori { get; set; } = new List<GenitoreImportDTO>();
        public List<PreferenzaCompagnoImportDto> PreferenzeCompagni { get; set; } = new List<PreferenzaCompagnoImportDto>();
    }
}
