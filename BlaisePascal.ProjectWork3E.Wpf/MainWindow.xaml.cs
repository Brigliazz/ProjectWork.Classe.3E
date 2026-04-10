using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BlaisePascal.ProjectWork._3E.Infrastructure.ExcelServices;
using Microsoft.Win32;
using BlaisePascal.ProjectWork._3E.Domain.Services;
using BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data;
using BlaisePascal.ProjectWork._3E.Infrastructure.Database.DatabaseInitializer;
using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using BlaisePascal.ProjectWork._3E.Infrastructure.Services;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;

namespace BlaisePascal.ProjectWork3E.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ImportazioneExcelService ImportazioneService { get; set; } = new ImportazioneExcelService();
        private string importedFilePath = string.Empty;
        public int numeroClassiInformatica;
        public int numeroClassiElettronica;
        public int numeroClassiBiotecnologie;

        // Flag: i dati sono stati importati con successo
        private bool _datiImportati = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "File Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tutti i file (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                TxtFilePath.Text = openFileDialog.FileName;
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            importedFilePath = TxtFilePath.Text;

            // 1. Controllo del file
            if (string.IsNullOrWhiteSpace(importedFilePath))
            {
                MessageBox.Show("Seleziona prima un file Excel usando il tasto Sfoglia file.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Lettura e validazione del numero delle classi
            bool isInfoValid    = int.TryParse(TxtClassiInformatica.Text,   out numeroClassiInformatica);
            bool isElettroValid = int.TryParse(TxtClassiElettronica.Text,   out numeroClassiElettronica);
            bool isBiotecValid  = int.TryParse(TxtClassiBiotecnologie.Text, out numeroClassiBiotecnologie);

            if (!isInfoValid || !isElettroValid || !isBiotecValid)
            {
                MessageBox.Show("Inserisci dei valori numerici validi per il numero delle classi.", "Errore Inserimento", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. Svuota le liste per evitare duplicati in caso di re-importazione
            DatiImportatiDto.Alunni.Clear();
            DatiImportatiDto.Scelte.Clear();
            DatiImportatiDto.Scuole.Clear();
            DatiImportatiDto.Genitori.Clear();
            DatiImportatiDto.PreferenzeCompagni.Clear();

            // 4. Legge il file Excel e popola DatiImportatiDto
            try
            {
                ImportazioneService.EstrapolaDati(importedFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore durante la lettura del file Excel:\n" + ex.Message, "Errore Importazione", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DatiImportatiDto.Alunni.Count == 0)
            {
                MessageBox.Show("Il file Excel non contiene alunni validi. Verificare il formato del file.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5. Salva numero classi nel DTO
            DatiImportatiDto.NumeroClassiInformatica   = numeroClassiInformatica;
            DatiImportatiDto.NumeroClassiAutomazione   = numeroClassiElettronica;
            DatiImportatiDto.NumeroClassiBiotecnologie = numeroClassiBiotecnologie;

            // 6. Persiste tutto su SQLite (studenti.db)
            DatabaseInitializer.Initialize();

            _datiImportati = true;

            MessageBox.Show(
                "Importazione completata!\n\n" +
                "Studenti importati: " + DatiImportatiDto.Alunni.Count + "\n" +
                "Classi Informatica: " + numeroClassiInformatica + "\n" +
                "Classi Elettronica: " + numeroClassiElettronica + "\n" +
                "Classi Biotecnologie: " + numeroClassiBiotecnologie + "\n\n" +
                "Premi Scarica Risultati per avviare la distribuzione.",
                "Verifica Importazione", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnDownload_Click_1(object sender, RoutedEventArgs e)
        {
            if (!_datiImportati || DatiImportatiDto.Alunni == null || DatiImportatiDto.Alunni.Count == 0)
            {
                MessageBox.Show(
                    "Nessun dato importato.\nEsegui prima l importazione del file Excel e premi Importa.",
                    "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProgBar.Visibility    = Visibility.Visible;
            TxtStatus.Visibility  = Visibility.Visible;
            TxtStatus.Text        = "Distribuzione in corso... (OR-Tools, max 30s)";
            ProgBar.IsIndeterminate = true;

            try
            {
                // Disabilita subito nel blocco in modo da coprire tutta la computazione
                BtnDownload.IsEnabled = false;
                BtnImport.IsEnabled   = false;
                
                // Cede il controllo al thread UI per un istante, aggiornando visibilmente 
                // lo state "disabled" dei pulsanti prima dello spawn in background.
                await Task.Yield();

                var useCase   = new DistribuzioneUseCase();
                var risultato = await useCase.EseguiAsync();

                GestisciRisultatoDistribuzione(useCase, risultato);
            }
            catch (BlaisePascal.ProjectWork._3E.Domain.Exceptions.ClasseCapienzaSuperataException ex)
            {
                // Avvisa l'utente tramite Dialog dedicato WPF che verifica se far ammorbidire i vincoli o annullare.
                var dialog = new ConfermaCapienzaDialog(ex.NomeClasse, ex.Limite);
                dialog.Owner = this; // Imposta l'owner per bloccare la UI principale dietro al model.

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // Costruisce le opzioni con SezioniPerIndirizzo corrette dai dati importati,
                        // poi aggiunge ConsentiSforo = true per il tentativo con sforo.
                        var opzioniFallback = DistribuzioneUseCase.CostruisciOpzioniDaDatiImportati();
                        opzioniFallback.ConsentiSforo = true;

                        var useCaseSforo = new DistribuzioneUseCase();
                        var risultatoSforo = await useCaseSforo.EseguiAsync(opzioniFallback);

                        GestisciRisultatoDistribuzione(useCaseSforo, risultatoSforo);
                    }
                    catch (Exception exSforo)
                    {
                        MessageBox.Show("Rilevato un errore irrisolvibile anche tentando la distribuzione con capienza allargata:\n" + exSforo.Message, "Errore Strutturale Infeasibility", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // L'utente ha cliccato "Annulla" o chiuso il dialog
                    MessageBox.Show("Distribuzione interrotta. La capienza limite deve essere garantita o aumentata modificando i dati.", "Operazione Annullata", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Errore nei dati:\n" + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += "\n\nDettaglio: " + ex.InnerException.Message;
                }
                MessageBox.Show("Errore durante la distribuzione:\n" + errorMessage, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnDownload.IsEnabled   = true;
                BtnImport.IsEnabled     = true;
                ProgBar.Visibility      = Visibility.Collapsed;
                TxtStatus.Visibility    = Visibility.Collapsed;
                ProgBar.IsIndeterminate = false;
            }
        }

        private void GestisciRisultatoDistribuzione(DistribuzioneUseCase useCase, List<List<BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.Studente>> risultato)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Distribuzione completata! " + BlaisePascal.ProjectWork._3E.Application.ImportModels.DatiImportatiDto.Alunni.Count + " studenti assegnati.\n");

            int numClasse = 1;
            foreach (var classe in risultato)
            {
                sb.AppendLine("-- Classe " + numClasse + " (" + classe.Count + " studenti) --");
                foreach (var s in classe.OrderBy(x => x.Cognome).ThenBy(x => x.Nome))
                    sb.AppendLine("  * " + s.Cognome + " " + s.Nome);
                sb.AppendLine();
                numClasse++;
            }

            if (useCase.MatchIncerti.Count > 0)
            {
                sb.AppendLine("\nATTENZIONE: " + useCase.MatchIncerti.Count + " preferenze incerte:");
                foreach (var m in useCase.MatchIncerti)
                    sb.AppendLine("  * " + m.Messaggio);
            }

            // Genera l'Excel
            var esportatore = new BlaisePascal.ProjectWork._3E.Application.ExportModels.EsportazioneDatiExcel();
            esportatore.Esporta(risultato, useCase.ClassiGenerate.ToList());

            MessageBox.Show("Distribuzione completata con successo!\nIl file Excel è stato generato e aperto.\n\n" + (useCase.MatchIncerti.Count > 0 ? "Ci sono match incerti, vedi l'altro avviso." : ""), "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}