using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LAGA
{
    /// <summary>
    /// UserControl zur Anzeige aller WarnArtikel (Artikel mit aktivem Warnsystem)
    /// Zeigt eine Übersicht aller Artikel an, die den Mindestbestand erreicht haben
    /// </summary>
    public partial class WarnArtikelAnzeigen : UserControl
    {
        /// <summary>
        /// Observable Collection für die WarnArtikel-Anzeige-DTOs (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<WarnArtikelAnzeigeDto> _warnArtikel;

        /// <summary>
        /// CollectionView für Sortierung
        /// </summary>
        private ICollectionView _warnArtikelView;

        public WarnArtikelAnzeigen()
        {
            InitializeComponent();
            _warnArtikel = new ObservableCollection<WarnArtikelAnzeigeDto>();

            // CollectionView für Sortierung erstellen
            _warnArtikelView = CollectionViewSource.GetDefaultView(_warnArtikel);

            // Sortierung nach Datum (neueste Warnungen zuerst)
            _warnArtikelView.SortDescriptions.Add(new SortDescription("LetzteWarnungVersendet", ListSortDirection.Descending));

            // DataGrid an CollectionView binden
            dgWarnArtikel.ItemsSource = _warnArtikelView;

            // Daten beim Laden asynchron abrufen
            LoadWarnArtikelAsync();
        }

        /// <summary>
        /// Lädt alle WarnArtikel asynchron über den BestandsMonitor Service
        /// </summary>
        private async void LoadWarnArtikelAsync()
        {
            try
            {
                // WarnArtikel über BestandsMonitor Service laden
                var warnArtikelListe = await BestandsMonitor.GetWarnArtikelAsync();

                // ObservableCollection aktualisieren (UI wird automatisch aktualisiert)
                _warnArtikel.Clear();
                foreach (var warnArtikel in warnArtikelListe)
                {
                    _warnArtikel.Add(warnArtikel);
                }

                // Debug-Information über geladene Warnungen
                System.Diagnostics.Debug.WriteLine($"WarnArtikel geladen: {warnArtikelListe.Count} Artikel mit aktiven Warnungen");

                // Info-Meldung anzeigen wenn keine Warnungen vorhanden
                if (warnArtikelListe.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Keine aktiven Warnungen gefunden - alle Artikel haben ausreichend Bestand");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der WarnArtikel: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der WarnArtikel: {ex.Message}");
            }
        }

        /// <summary>
        /// Schließt die WarnArtikel-Ansicht und kehrt zum StartFenster zurück
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ClearMainContent();
        }

        /// <summary>
        /// Öffentliche Methode zum Aktualisieren der WarnArtikel-Anzeige
        /// Wird aufgerufen wenn sich der Warnungs-Status von Artikeln geändert hat
        /// </summary>
        public void RefreshWarnArtikel()
        {
            LoadWarnArtikelAsync();
        }

        /// <summary>
        /// Prüft alle Artikel im System und aktualisiert die Warnungs-Status
        /// Nützlich für initiale Synchronisation oder nach System-Updates
        /// </summary>
        public async void SynchronisiereWarnungen()
        {
            try
            {
                // Vollständige Prüfung aller Artikel über BestandsMonitor
                int aktivierteWarnungen = await BestandsMonitor.PruefeAlleArtikelAsync();

                // Anzeige aktualisieren
                LoadWarnArtikelAsync();

                System.Diagnostics.Debug.WriteLine($"Warnungs-Synchronisation abgeschlossen: {aktivierteWarnungen} neue Warnungen aktiviert");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Warnungs-Synchronisation: {ex.Message}",
                    "Synchronisationsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}