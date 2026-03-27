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


namespace BlaisePascal.ProjectWork3E.Wpf
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ImportazioneExcelService ImportazioneService { get; set; } = new ImportazioneExcelService();
        private string importedFilePath = string.Empty;

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

     
            var datiEstratti = ImportazioneService.EstrapolaDati(importedFilePath);

          
            DatabaseInitializer.Initialize(datiEstratti);

            if (string.IsNullOrWhiteSpace(importedFilePath))
            {
                MessageBox.Show("Seleziona prima un file Excel usando il tasto 'Sfoglia file'.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Percorso file copiato con successo nella variabile!\n\nPercorso: {importedFilePath}", "Verifica Importazione", MessageBoxButton.OK, MessageBoxImage.Information);
        }


    }

}