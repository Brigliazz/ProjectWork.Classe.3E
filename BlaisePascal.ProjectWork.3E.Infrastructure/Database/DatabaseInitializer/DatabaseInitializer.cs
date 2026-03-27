using BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data;
using BlaisePascal.ProjectWork._3E.Application.ImportModels;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.DatabaseInitializer
{
    public static class DatabaseInitializer
    {
        // 1. Aggiungi "DatiImportatiDto dati" come parametro
        public static void Initialize(DatiImportatiDto dati)
        {
            // FASE 1: CREAZIONE TABELLE (L'ordine non è vitale)
            AlunnoRepository.CreaTabella();
            GenitoriRepository.CreaTabella();
            PreferenzaCompagnoRepository.CreaTabella();
            ScelteEffettuateAlunnoRepository.CreaTabella();
            ScuolaDiProvenienzaRepository.CreaTabella();

            // FASE 2: SVUOTAMENTO TABELLE (ORDINE INVERSO! Prima i figli, poi il padre)
            GenitoriRepository.SvuotaTabella();
            PreferenzaCompagnoRepository.SvuotaTabella();
            ScelteEffettuateAlunnoRepository.SvuotaTabella();
            ScuolaDiProvenienzaRepository.SvuotaTabella();
            AlunnoRepository.SvuotaTabella(); // L'alunno si svuota PER ULTIMO seno' crashano i vincoli di chiave esterna (perché i figli puntano al padre, se il padre non c'è più, i figli non possono esistere)

            // FASE 3: SALVATAGGIO DATI (Usa "dati." invece di "DatiImportatiDto.")
            // ORDINE CORRETTO: Prima il padre, poi i figli
            AlunnoRepository.SalvaStudenti(dati.Alunni);

            GenitoriRepository.SalvaGenitori(dati.Genitori);
            PreferenzaCompagnoRepository.SalvaPreferenze(dati.PreferenzeCompagni);
            ScelteEffettuateAlunnoRepository.SalvaScelte(dati.Scelte);
            ScuolaDiProvenienzaRepository.SalvaScuole(dati.Scuole);
        }
    }
}