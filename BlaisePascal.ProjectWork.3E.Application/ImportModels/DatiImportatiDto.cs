using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Application.ImportModels
{
    public static class DatiImportatiDto
    {
        public static List<StudenteImportDto> Alunni { get; set; } = new List<StudenteImportDto>();
        public static List<SceltaImportDto> Scelte { get; set; } = new List<SceltaImportDto>();
        public static List<ScuolaProvImportDto> Scuole { get; set; } = new List<ScuolaProvImportDto>();
        public static List<GenitoreImportDTO> Genitori { get; set; } = new List<GenitoreImportDTO>();
        public static List<PreferenzaCompagnoImportDto> PreferenzeCompagni { get; set; } = new List<PreferenzaCompagnoImportDto>();
        public static int NumeroClassiInformatica { get;set;}
        public static int NumeroClassiAutomazione { get; set;}
        public static int NumeroClassiBiotecnologie { get; set;}
    }
}
