using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlaisePascal.ProjectWork._3E.Application.ImportModels;

namespace BlaisePascal.ProjectWork._3E.Application.Interfaces
{
    public interface IImportazioneExcelService
    {
        void EstrapolaDati(string percorsoFile);
    }
}