using System.Windows;
using System.Windows.Controls;

namespace LAGA
{
    /// <summary>
    /// Hauptfenster der LAGA-Anwendung mit dynamisch austauschbarem Content-Bereich
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Datenbank beim Start initialisieren
            InitializeDatabaseAsync();

            // StartFenster als Standard-Ansicht laden
            LoadStartFenster();
        }

        /// <summary>
        /// Lädt das StartFenster und deaktiviert Event-Handler während Menü-Nutzung
        /// </summary>
        private void LoadStartFenster()
        {
            var startFenster = new StartFenster();
            MainContentArea.Content = startFenster;
        }

        /// <summary>
        /// Öffentliche Methode zum Leeren des Content-Bereichs
        /// Lädt automatisch das StartFenster als Standard-Ansicht
        /// </summary>
        public void ClearMainContent()
        {
            // StartFenster als Standard-Ansicht laden
            LoadStartFenster();
        }

        /// <summary>
        /// Initialisiert die SQLite-Datenbank asynchron beim Programmstart
        /// </summary>
        private async void InitializeDatabaseAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Erstellt die Datenbank und alle Tabellen falls sie nicht existieren
                    await context.Database.EnsureCreatedAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Initialisieren der Datenbank: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt die Lieferquellen-Anzeige im Haupt-Content-Bereich
        /// </summary>
        private void LieferquellenAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            // Erstellt eine neue Instanz der Lieferquellen-Anzeige
            var lieferquellenAnzeige = new LieferquellenAnzeigen();

            // Lädt den UserControl in den Haupt-Content-Bereich
            MainContentArea.Content = lieferquellenAnzeige;
        }

        /// <summary>
        /// Öffnet das modale Fenster zur Anzeige aller Kostenstellen
        /// </summary>
        private void KostenstellenAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var kostenstellenAnzeige = new KostenstellenAnzeigen();
                kostenstellenAnzeige.Owner = this;
                kostenstellenAnzeige.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Kostenstellen-Anzeige-Fensters: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Öffnet das modale Fenster zur Anzeige aller Lagerorte
        /// </summary>
        private void LagerorteAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lagerorteAnzeige = new LagerorteAnzeigen();
                lagerorteAnzeige.Owner = this;
                lagerorteAnzeige.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Lagerorte-Anzeige-Fensters: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt die Artikel-Anzeige im Haupt-Content-Bereich
        /// </summary>
        private void ArtikelAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var artikelAnzeige = new ArtikelAnzeigen();
                MainContentArea.Content = artikelAnzeige;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Artikel-Anzeige: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt die Lagerbestand-Anzeige im Haupt-Content-Bereich
        /// </summary>
        private void LagerbestandAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Scanner-Events wieder aktivieren falls sie deaktiviert waren
                if (MainContentArea.Content is StartFenster startFenster)
                {
                    startFenster.EnableScannerEvents();
                }

                var lagerbestandAnzeige = new LagerBestandAnzeigen();
                MainContentArea.Content = lagerbestandAnzeige;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Lagerbestand-Anzeige: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}