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
    /// UserControl zur Anzeige aller Artikel mit Such- und Bearbeitungsfunktionen
    /// </summary>
    public partial class ArtikelAnzeigen : UserControl
    {
        /// <summary>
        /// Observable Collection für die Artikel-Anzeige-DTOs (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<ArtikelAnzeigeDto> _artikel;

        /// <summary>
        /// CollectionView für Filterung und Sortierung
        /// </summary>
        private ICollectionView _artikelView;

        public ArtikelAnzeigen()
        {
            InitializeComponent();
            _artikel = new ObservableCollection<ArtikelAnzeigeDto>();

            // CollectionView für Filterung erstellen
            _artikelView = CollectionViewSource.GetDefaultView(_artikel);
            _artikelView.Filter = FilterArtikel;

            // Alphabetische Sortierung nach Bezeichnung
            _artikelView.SortDescriptions.Add(new SortDescription("Bezeichnung", ListSortDirection.Ascending));

            // DataGrid an CollectionView binden
            dgArtikel.ItemsSource = _artikelView;

            // Daten beim Laden asynchron abrufen
            LoadArtikelAsync();
        }

        /// <summary>
        /// Lädt alle Artikel mit Join-Daten asynchron aus der Datenbank
        /// </summary>
        private async void LoadArtikelAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Artikel mit allen verknüpften Daten laden (Join-Query)
                    var artikelQuery = await context.Artikel
                        .Include(a => a.Lieferant)
                        .Include(a => a.Hersteller)
                        .Include(a => a.Kostenstelle)
                        .Include(a => a.Lagerort)
                        .ToListAsync();

                    // In DTOs umwandeln für bessere Anzeige
                    var artikelDtos = artikelQuery.Select(a => new ArtikelAnzeigeDto
                    {
                        Id = a.Id,
                        Bezeichnung = a.Bezeichnung,
                        KostenstelleBezeichnung = a.Kostenstelle?.Bezeichnung ?? "Unbekannt",
                        EinheitBezeichnung = a.IstEinzelteil ? "Einzelteil" : "Karton mit Einzelteilen",
                        LagerortBezeichnung = a.Lagerort?.Bezeichnung ?? "Unbekannt",
                        LieferantBezeichnung = a.Lieferant?.Bezeichnung ?? "Unbekannt",
                        HerstellerBezeichnung = a.Hersteller?.Bezeichnung ?? "Unbekannt",
                        OriginalArtikel = a
                    }).ToList();

                    // ObservableCollection aktualisieren (UI wird automatisch aktualisiert)
                    _artikel.Clear();
                    foreach (var artikelDto in artikelDtos)
                    {
                        _artikel.Add(artikelDto);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Artikel: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Suchtext ändert
        /// </summary>
        private void TxtSuche_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Filter der CollectionView aktualisieren
            _artikelView.Refresh();
        }

        /// <summary>
        /// Filterfunktion für die Suche in allen Spalten
        /// </summary>
        private bool FilterArtikel(object item)
        {
            if (item is ArtikelAnzeigeDto artikel)
            {
                string suchtext = txtSuche.Text?.ToLower() ?? "";

                // Suche in allen Feldern des Artikels
                return string.IsNullOrEmpty(suchtext) ||
                       artikel.Bezeichnung.ToLower().Contains(suchtext) ||
                       artikel.EinheitBezeichnung.ToLower().Contains(suchtext) ||
                       artikel.LagerortBezeichnung.ToLower().Contains(suchtext) ||
                       artikel.LieferantBezeichnung.ToLower().Contains(suchtext) ||
                       artikel.HerstellerBezeichnung.ToLower().Contains(suchtext);
            }
            return false;
        }

        /// <summary>
        /// Behandelt Rechtsklick auf DataGrid-Zeilen
        /// </summary>
        private void DgArtikel_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Finde das geklickte Element
            var hitTest = dgArtikel.InputHitTest(e.GetPosition(dgArtikel));

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
                dgArtikel.SelectedItem = row.Item;

                // ContextMenu ist bereits im XAML definiert, daher automatisch verfügbar
            }
        }

        /// <summary>
        /// Öffnet das Bearbeiten-Fenster für den ausgewählten Artikel
        /// </summary>
        private void MenuItemBearbeiten_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is ArtikelAnzeigeDto selectedArtikelDto)
            {
                // Bearbeiten-View im MainWindow anzeigen
                var bearbeitenView = new ArtikelBearbeiten(selectedArtikelDto.OriginalArtikel);

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.MainContentArea.Content = bearbeitenView;
                }
            }
        }

        /// <summary>
        /// Löscht den ausgewählten Artikel nach Bestätigung
        /// </summary>
        private async void MenuItemLoeschen_Click(object sender, RoutedEventArgs e)
        {
            if (dgArtikel.SelectedItem is ArtikelAnzeigeDto selectedArtikelDto)
            {
                // Bestätigungsdialog anzeigen
                var result = MessageBox.Show(
                    "Sind Sie sicher, dass der Artikel gelöscht werden soll?",
                    "Artikel löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Artikel asynchron aus Datenbank löschen
                        using (var context = new LagerContext())
                        {
                            // Artikel anhand der ID finden und löschen
                            var artikelToDelete = await context.Artikel
                                .FirstOrDefaultAsync(a => a.Id == selectedArtikelDto.Id);

                            if (artikelToDelete != null)
                            {
                                context.Artikel.Remove(artikelToDelete);
                                await context.SaveChangesAsync();

                                // Erfolgsmeldung
                                MessageBox.Show("Artikel wurde erfolgreich gelöscht.",
                                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                                // Daten neu laden
                                LoadArtikelAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen des Artikels: {ex.Message}",
                            "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Schließt die Artikel-Ansicht
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ClearMainContent();
        }
    }
}