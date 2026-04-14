using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        // Metodo diventato ASYNC per permettere l'animazione a schermo
        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            importedFilePath = TxtFilePath.Text;

            // 1. Controllo del file
            if (string.IsNullOrWhiteSpace(importedFilePath))
            {
                MessageBox.Show("Seleziona prima un file Excel usando il tasto 'Sfoglia file'.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Lettura e salvataggio del numero delle classi
            bool isInfoValid = int.TryParse(TxtClassiInformatica.Text, out numeroClassiInformatica);
            bool isElettroValid = int.TryParse(TxtClassiElettronica.Text, out numeroClassiElettronica);
            bool isBiotecValid = int.TryParse(TxtClassiBiotecnologie.Text, out numeroClassiBiotecnologie);

            if (!isInfoValid || !isElettroValid || !isBiotecValid)
            {
                MessageBox.Show("Inserisci dei valori numerici validi per il numero delle classi.", "Errore Inserimento", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // MOSTRA CARICAMENTO e disabilita il pulsante
                BtnImport.IsEnabled = false;
                BtnImport.Content = "Importazione...";
                ProgBar.Visibility = Visibility.Visible;
                TxtStatus.Text = "Lettura del file Excel in corso...";
                TxtStatus.Visibility = Visibility.Visible;

                // 3. Elaborazione dei dati spostata in background per non bloccare la grafica
                await Task.Run(() =>
                {
                    DatiImportatiDto.Alunni.Clear();
                    DatiImportatiDto.Scelte.Clear();
                    DatiImportatiDto.Scuole.Clear();
                    DatiImportatiDto.Genitori.Clear();
                    DatiImportatiDto.PreferenzeCompagni.Clear();

                    ImportazioneService.EstrapolaDati(importedFilePath);

                    DatiImportatiDto.NumeroClassiInformatica = numeroClassiInformatica;
                    DatiImportatiDto.NumeroClassiAutomazione = numeroClassiElettronica;
                    DatiImportatiDto.NumeroClassiBiotecnologie = numeroClassiBiotecnologie;
                    DatabaseInitializer.Initialize();
                });

                // Messaggio di successo
                MessageBox.Show($"Importazione completata!\n\nTrovati {DatiImportatiDto.Alunni.Count} studenti nel file Excel.\nFile: {importedFilePath}\nClassi Informatica: {numeroClassiInformatica}\nClassi Elettronica: {numeroClassiElettronica}\nClassi Biotecnologie: {numeroClassiBiotecnologie}",
                                "Verifica Importazione", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Si è verificato un errore durante l'importazione del file Excel:\n\n{ex.Message}", "Errore Lettura File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // NASCONDE IL CARICAMENTO e ripristina il pulsante
                BtnImport.IsEnabled = true;
                BtnImport.Content = "Importa";
                ProgBar.Visibility = Visibility.Collapsed;
                TxtStatus.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnDownload_Click_1(object sender, RoutedEventArgs e)
        {
            if (DatiImportatiDto.Alunni.Count == 0)
            {
                MessageBox.Show("Nessun dato importato. Effettua prima l'importazione.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // MOSTRA CARICAMENTO e disabilita il pulsante
                BtnDownload.IsEnabled = false;
                BtnDownload.Content = "Generazione in corso...";
                ProgBar.Visibility = Visibility.Visible;
                TxtStatus.Text = "Creazione classi e file in corso...";
                TxtStatus.Visibility = Visibility.Visible;

                var studenteRepo = new BlaisePascal.ProjectWork._3E.Infrastructure.Persistence.InMemoryStudenteRepository();
                var classeRepo = new BlaisePascal.ProjectWork._3E.Infrastructure.Persistence.InMemoryClasseRepository();

                // Il ciclo foreach pesante spostato su un task separato
                await Task.Run(async () =>
                {
                    foreach (var dto in DatiImportatiDto.Alunni)
                    {
                        var preferenza = DatiImportatiDto.PreferenzeCompagni.FirstOrDefault(p => p.CodiceFiscaleStudente == dto.CodiceFiscale);
                        var scelta = DatiImportatiDto.Scelte.FirstOrDefault(s => s.CodiceFiscaleStudente == dto.CodiceFiscale);
                        var scuola = DatiImportatiDto.Scuole.FirstOrDefault(s => s.CodiceFiscaleStudente == dto.CodiceFiscale);

                        var cittadinanza = string.IsNullOrWhiteSpace(dto.Cittadinanza) || dto.Cittadinanza.ToLower() == "italiana" || dto.Cittadinanza.ToLower() == "italia"
                            ? BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.Cittadinanza.Italiana
                            : BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.Cittadinanza.Crea(100);

                        var profiloBES = BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.ProfiloBES.Crea(dto.Disabilita, dto.Dsa, dto.DisabilitaAssistenzaBase);

                        BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.SceltaCompagno? compagno = null;
                        if (preferenza != null && !string.IsNullOrWhiteSpace(preferenza.NomeStudenteScelto))
                        {
                            compagno = BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.SceltaCompagno.Crea(preferenza.NomeStudenteScelto);
                        }

                        DateOnly dataNascita = DateOnly.TryParse(dto.DataDiNascita, out var dn) ? dn : new DateOnly(2010, 1, 1);
                        DateOnly? dataArrivo = DateOnly.TryParse(dto.DataArrivoInItalia, out var da) ? da : null;

                        var studente = BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente.Studente.Crea(
                            nome: string.IsNullOrWhiteSpace(dto.Nome) ? "Anonimo" : dto.Nome,
                            cognome: string.IsNullOrWhiteSpace(dto.Cognome) ? "Anonimo" : dto.Cognome,
                            sesso: dto.Sesso ? BlaisePascal.ProjectWork._3E.Domain.Enums.Sesso.Maschio : BlaisePascal.ProjectWork._3E.Domain.Enums.Sesso.Femmina,
                            codiceFiscale: string.IsNullOrWhiteSpace(dto.CodiceFiscale) ? Guid.NewGuid().ToString().Substring(0, 16) : dto.CodiceFiscale,
                            dataNascita: dataNascita,
                            dataArrivoItalia: dataArrivo,
                            cittadinanza: cittadinanza,
                            codiceScuolaProvenienza: scuola?.CodiceScuola ?? "BOH",
                            profiloBES: profiloBES,
                            faReligione: dto.FaReligione,
                            votoEsame: (dto.VotoEsameTerzaMedia > 0 && dto.VotoEsameTerzaMedia <= 10) ? dto.VotoEsameTerzaMedia : null,
                            sceltaCompagno: compagno,
                            indirizzoPreferito: scelta?.IndirizzoScelto
                        );

                        await studenteRepo.AddAsync(studente);
                    }
                });

                var opzioni = new OpzioniDistribuzione
                {
                    ConsentiSforo = true,
                    UsaPreferenze = true,
                };

                opzioni.SezioniPerIndirizzo["Informatica"] = numeroClassiInformatica;
                opzioni.SezioniPerIndirizzo["Automazione"] = numeroClassiElettronica;
                opzioni.SezioniPerIndirizzo["Bio"] = numeroClassiBiotecnologie;

                var service = new DistribuzioneClassiService(studenteRepo, classeRepo);
                var esportazione = new BlaisePascal.ProjectWork._3E.Application.ExportModels.EsportazioneDatiExcel(service);

                // Distribuisce ed esporta
                await esportazione.EsportaAsync(opzioni);

                MessageBox.Show("Risultati distribuiti ed esportati con successo nel Desktop!", "Completato", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante la distribuzione o esportazione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // NASCONDE IL CARICAMENTO e ripristina il pulsante
                BtnDownload.IsEnabled = true;
                BtnDownload.Content = "Scarica Risultati";
                ProgBar.Visibility = Visibility.Collapsed;
                TxtStatus.Visibility = Visibility.Collapsed;
            }
        }
    }
}