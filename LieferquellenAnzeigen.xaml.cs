using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// UserControl zur Anzeige aller Lieferquellen mit Such- und Bearbeitungsfunktionen
    /// </summary>
    public partial class LieferquellenAnzeigen : UserControl
    {
        /// <summary>
        /// Observable Collection für die Lieferquellen (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<Lieferquelle> _lieferquellen;

        /// <summary>
        /// CollectionView für Filterung und Sortierung
        /// </summary>
        private ICollectionView _lieferquellenView;

        public LieferquellenAnzeigen()
        {
            InitializeComponent();
            _lieferquellen = new ObservableCollection<Lieferquelle>();

            // CollectionView für Filterung erstellen
            _lieferquellenView = CollectionViewSource.GetDefaultView(_lieferquellen);
            _lieferquellenView.Filter = FilterLieferquellen;

            // Alphabetische Sortierung nach Bezeichnung
            _lieferquellenView.SortDescriptions.Add(new SortDescription("Bezeichnung", ListSortDirection.Ascending));

            // DataGrid an CollectionView binden
            dgLieferquellen.ItemsSource = _lieferquellenView;

            // Daten beim Laden asynchron abrufen
            LoadLieferquellenAsync();
        }

        /// <summary>
        /// Lädt alle Lieferquellen asynchron aus der Datenbank
        /// </summary>
        private async void LoadLieferquellenAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle Lieferquellen asynchron laden
                    var lieferquellen = await context.Lieferquellen.ToListAsync();

                    // ObservableCollection aktualisieren (UI wird automatisch aktualisiert)
                    _lieferquellen.Clear();
                    foreach (var lieferquelle in lieferquellen)
                    {
                        _lieferquellen.Add(lieferquelle);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Lieferquellen: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Suchtext ändert
        /// </summary>
        private void TxtSuche_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Filter der CollectionView aktualisieren
            _lieferquellenView.Refresh();
        }

        /// <summary>
        /// Filterfunktion für die Suche in allen Spalten
        /// </summary>
        private bool FilterLieferquellen(object item)
        {
            if (item is Lieferquelle lieferquelle)
            {
                string suchtext = txtSuche.Text?.ToLower() ?? "";

                // Suche in allen Feldern der Lieferquelle
                return string.IsNullOrEmpty(suchtext) ||
                       lieferquelle.Bezeichnung.ToLower().Contains(suchtext) ||
                       lieferquelle.Email.ToLower().Contains(suchtext) ||
                       lieferquelle.Telefon.ToLower().Contains(suchtext) ||
                       lieferquelle.Webseite.ToLower().Contains(suchtext);
            }
            return false;
        }

        /// <summary>
        /// Behandelt Rechtsklick auf DataGrid-Zeilen
        /// </summary>
        private void DgLieferquellen_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Finde das geklickte Element
            var hitTest = dgLieferquellen.InputHitTest(e.GetPosition(dgLieferquellen));

            // Gehe den visuellen Baum hoch um die DataGridRow zu finden
            DependencyObject dep = (DependencyObject)hitTest;
            while (dep != null && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridRow row)
            {
                // Zeile auswählen wenn sie noch nicht ausgewählt ist
                row.IsSelected = true;
                dgLieferquellen.SelectedItem = row.Item;

                // ContextMenu ist bereits im XAML definiert, daher automatisch verfügbar
                // Kein manuelles Setzen nötig - WPF macht das automatisch
            }
        }

        /// <summary>
        /// Öffnet das Bearbeiten-Fenster für die ausgewählte Lieferquelle
        /// </summary>
        private void MenuItemBearbeiten_Click(object sender, RoutedEventArgs e)
        {
            if (dgLieferquellen.SelectedItem is Lieferquelle selectedLieferquelle)
            {
                // Bearbeiten-Fenster öffnen
                var bearbeitenFenster = new LieferquelleBearbeiten(selectedLieferquelle);
                bearbeitenFenster.Owner = Window.GetWindow(this);

                // Nach dem Schließen des Bearbeiten-Fensters Daten neu laden wenn erfolgreich gespeichert
                if (bearbeitenFenster.ShowDialog() == true)
                {
                    LoadLieferquellenAsync();
                }
            }
        }

        /// <summary>
        /// Löscht die ausgewählte Lieferquelle nach Bestätigung
        /// </summary>
        private async void MenuItemLoeschen_Click(object sender, RoutedEventArgs e)
        {
            if (dgLieferquellen.SelectedItem is Lieferquelle selectedLieferquelle)
            {
                // Bestätigungsdialog anzeigen
                var result = MessageBox.Show(
                    "Sind Sie sicher, dass die Lieferquelle gelöscht werden soll?",
                    "Lieferquelle löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Lieferquelle asynchron aus Datenbank löschen
                        using (var context = new LagerContext())
                        {
                            // Lieferquelle anhand der ID finden und löschen
                            var lieferquelleToDelete = await context.Lieferquellen
                                .FirstOrDefaultAsync(l => l.Id == selectedLieferquelle.Id);

                            if (lieferquelleToDelete != null)
                            {
                                context.Lieferquellen.Remove(lieferquelleToDelete);
                                await context.SaveChangesAsync();

                                // Erfolgsmeldung
                                MessageBox.Show("Lieferquelle wurde erfolgreich gelöscht.",
                                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                                // Daten neu laden
                                LoadLieferquellenAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen der Lieferquelle: {ex.Message}",
                            "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Öffnet das Hinzufügen-Fenster für eine neue Lieferquelle
        /// </summary>
        private void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Lieferquelle-Hinzufügen-Fenster als modalen Dialog öffnen
            var hinzufuegenFenster = new LieferquelleHinzufuegen();
            hinzufuegenFenster.Owner = Window.GetWindow(this);

            // Nach dem Schließen des Hinzufügen-Fensters prüfen ob erfolgreich gespeichert
            if (hinzufuegenFenster.ShowDialog() == true)
            {
                // Daten neu laden um neue Lieferquelle anzuzeigen
                LoadLieferquellenAsync();
            }
        }

        /// <summary>
        /// Schließt die Lieferquellen-Ansicht
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            // MainWindow finden und Content leeren
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ClearMainContent();
        }
    }
}