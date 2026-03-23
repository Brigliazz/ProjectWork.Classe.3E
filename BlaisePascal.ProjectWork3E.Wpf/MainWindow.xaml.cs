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
using Microsoft.Win32;
using BlaisePascal.ProjectWork._3E.Domain.Services;

namespace BlaisePascal.ProjectWork3E.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string percorsoDati = string.Empty;

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
            percorsoDati = TxtFilePath.Text;

            if (string.IsNullOrWhiteSpace(percorsoDati))
            {
                MessageBox.Show("Seleziona prima un file Excel usando il tasto 'Sfoglia file'.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtNumeroClassi.Text, out int nClassi) || nClassi <= 0)
            {
                MessageBox.Show("Per favore inserisci un numero di classi valido (es. 1, 2, 3...).", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Assegna la variabile nel Domain Service
            BlaisePascal.ProjectWork.ImportazioneDati.percorsoFile = percorsoDati;

            MessageBox.Show($"File ed impostazioni acquisiti con successo!\n\nPercorso: {percorsoDati}\nNumero Classi impostato a: {BlaisePascal.ProjectWork._3E.Domain.Services.DistribuzioneClassiService.NumClassi}", "Verifica Importazione", MessageBoxButton.OK, MessageBoxImage.Information);
        }


    }
}