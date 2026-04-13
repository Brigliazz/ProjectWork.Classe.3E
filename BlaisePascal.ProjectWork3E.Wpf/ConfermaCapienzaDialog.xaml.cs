using System.Windows;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BlaisePascal.ProjectWork3E.Wpf
{
    public partial class ConfermaCapienzaDialog : Window
    {
        public ConfermaCapienzaDialog(string nomeClasse, int limite)
        {
            InitializeComponent();
            MessageText.Text = $"La classe {nomeClasse} ha superato il limite di {limite} studenti. Procedere comunque con lo sforo dei limiti su tutte le classi configurate?";
        }

        private void BtnProcedi_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnAnnulla_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
