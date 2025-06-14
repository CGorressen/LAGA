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
        }

        /// <summary>
        /// Öffentliche Methode zum Leeren des Content-Bereichs
        /// </summary>
        public void ClearMainContent()
        {
            MainContentArea.Content = null;
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
        /// Öffnet das modale Fenster zum Hinzufügen einer neuen Lieferquelle
        /// </summary>
        private void LieferquelleHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            var hinzufuegenFenster = new LieferquelleHinzufuegen();
            hinzufuegenFenster.Owner = this; // Macht das Fenster modal
            hinzufuegenFenster.ShowDialog(); // Blockiert die Interaktion mit dem Hauptfenster
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
        /// Öffnet das modale Fenster zum Hinzufügen einer neuen Kostenstelle
        /// </summary>
        private void KostenstelleHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hinzufuegenFenster = new KostenstelleHinzufuegen();
                hinzufuegenFenster.Owner = this;
                hinzufuegenFenster.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Kostenstelle-Hinzufügen-Fensters: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        /// Öffnet das modale Fenster zum Hinzufügen eines neuen Lagerortes
        /// </summary>
        private void LagerortHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hinzufuegenFenster = new LagerortHinzufuegen();
                hinzufuegenFenster.Owner = this;
                hinzufuegenFenster.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Lagerort-Hinzufügen-Fensters: {ex.Message}",
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
    }
}