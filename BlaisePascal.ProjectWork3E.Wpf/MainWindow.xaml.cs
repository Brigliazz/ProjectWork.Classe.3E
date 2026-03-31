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

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            importedFilePath = TxtFilePath.Text;

            // 1. Controllo del file (spostato IN CIMA per evitare errori se il file è vuoto)
            if (string.IsNullOrWhiteSpace(importedFilePath))
            {
                MessageBox.Show("Seleziona prima un file Excel usando il tasto 'Sfoglia file'.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Lettura, conversione e salvataggio del numero delle classi
            // Usiamo int.TryParse per assicurarci che l'utente abbia inserito dei numeri e non lettere
            bool isInfoValid = int.TryParse(TxtClassiInformatica.Text, out numeroClassiInformatica);
            bool isElettroValid = int.TryParse(TxtClassiElettronica.Text, out numeroClassiElettronica);
            bool isBiotecValid = int.TryParse(TxtClassiBiotecnologie.Text, out numeroClassiBiotecnologie);

            if (!isInfoValid || !isElettroValid || !isBiotecValid)
            {
                MessageBox.Show("Inserisci dei valori numerici validi per il numero delle classi.", "Errore Inserimento", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. Elaborazione dei dati (ora sicura)
            DatiImportatiDto.NumeroClassiInformatica = numeroClassiInformatica;
            DatiImportatiDto.NumeroClassiAutomazione = numeroClassiElettronica;
            DatiImportatiDto.NumeroClassiBiotecnologie = numeroClassiBiotecnologie;
            DatabaseInitializer.Initialize();

            // Messaggio di successo (aggiornato per mostrarti che le variabili funzionano)
            MessageBox.Show($"Importazione completata!\n\nFile: {importedFilePath}\nClassi Informatica: {numeroClassiInformatica}\nClassi Elettronica: {numeroClassiElettronica}\nClassi Biotecnologie: {numeroClassiBiotecnologie}",
                            "Verifica Importazione", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}