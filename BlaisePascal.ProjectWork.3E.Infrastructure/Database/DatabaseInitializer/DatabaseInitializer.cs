using BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data;
using BlaisePascal.ProjectWork._3E.Application.ImportModels;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.DatabaseInitializer
{
    public static class DatabaseInitializer
    {
        // 1. Aggiungi "DatiImportatiDto dati" come parametro
        public static void Initialize()
        {
            // FASE 1: CREAZIONE TABELLE (L'ordine non è vitale)
            AlunnoRepository.CreaTabella();
            GenitoriRepository.CreaTabella();
            PreferenzaCompagnoRepository.CreaTabella();
            ScelteEffettuateAlunnoRepository.CreaTabella();
            ScuolaDiProvenienzaRepository.CreaTabella();
            NumeroClassiRepository.CreaTabella();

            // FASE 2: SVUOTAMENTO TABELLE (ORDINE INVERSO! Prima i figli, poi il padre)
            GenitoriRepository.SvuotaTabella();
            PreferenzaCompagnoRepository.SvuotaTabella();
            ScelteEffettuateAlunnoRepository.SvuotaTabella();
            ScuolaDiProvenienzaRepository.SvuotaTabella();
            NumeroClassiRepository.SvuotaTabella();
            AlunnoRepository.SvuotaTabella(); // L'alunno si svuota PER ULTIMO seno' crashano i vincoli di chiave esterna (perché i figli puntano al padre, se il padre non c'è più, i figli non possono esistere)

            // FASE 3: SALVATAGGIO DATI (Usa "dati." invece di "DatiImportatiDto.")
            // ORDINE CORRETTO: Prima il padre, poi i figli
            AlunnoRepository.SalvaStudenti(DatiImportatiDto.Alunni);
            NumeroClassiRepository.SalvaNumeroClassi(DatiImportatiDto.NumeroClassiAutomazione,DatiImportatiDto.NumeroClassiInformatica,DatiImportatiDto.NumeroClassiBiotecnologie);
            GenitoriRepository.SalvaGenitori(DatiImportatiDto.Genitori);
            PreferenzaCompagnoRepository.SalvaPreferenze(DatiImportatiDto.PreferenzeCompagni);
            ScelteEffettuateAlunnoRepository.SalvaScelte(DatiImportatiDto.Scelte);
            ScuolaDiProvenienzaRepository.SalvaScuole(DatiImportatiDto.Scuole);
        }
    }
}