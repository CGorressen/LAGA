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
    /// UserControl zur Anzeige des aktuellen Lagerbestands aller Artikel
    /// mit dynamischer Bestandsberechnung über ArtikelEinheiten
    /// </summary>
    public partial class LagerBestandAnzeigen : UserControl
    {
        /// <summary>
        /// Observable Collection für die Lagerbestand-Anzeige-DTOs (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<LagerbestandAnzeigeDto> _lagerbestand;

        /// <summary>
        /// CollectionView für Filterung und Sortierung
        /// </summary>
        private ICollectionView _lagerbestandView;

        public LagerBestandAnzeigen()
        {
            InitializeComponent();
            _lagerbestand = new ObservableCollection<LagerbestandAnzeigeDto>();

            // CollectionView für Filterung erstellen
            _lagerbestandView = CollectionViewSource.GetDefaultView(_lagerbestand);
            _lagerbestandView.Filter = FilterLagerbestand;

            // Alphabetische Sortierung nach Bezeichnung
            _lagerbestandView.SortDescriptions.Add(new SortDescription("Bezeichnung", ListSortDirection.Ascending));

            // DataGrid an CollectionView binden
            dgLagerbestand.ItemsSource = _lagerbestandView;

            // Daten beim Laden asynchron abrufen
            LoadLagerbestandAsync();
        }

        /// <summary>
        /// Lädt alle Artikel mit dynamisch berechnetem Bestand aus der Datenbank
        /// Verwendet LINQ um den aktuellen Bestand über ArtikelEinheiten zu ermitteln
        /// </summary>
        private async void LoadLagerbestandAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle Artikel mit verknüpften Daten laden
                    var artikel = await context.Artikel
                        .Include(a => a.Lagerort)
                        .ToListAsync();

                    // DTOs erstellen mit dynamischer Bestandsberechnung
                    var lagerbestandDtos = new List<LagerbestandAnzeigeDto>();

                    foreach (var a in artikel)
                    {
                        // LINQ: Bestand dynamisch über ArtikelEinheiten berechnen
                        // Zählt alle Einheiten die zu diesem Artikel gehören
                        int bestand = context.ArtikelEinheiten.Count(e => e.ArtikelId == a.Id);

                        var dto = new LagerbestandAnzeigeDto
                        {
                            Id = a.Id,
                            Bezeichnung = a.Bezeichnung,
                            Bestand = bestand, // Dynamisch berechnet!
                            LagerortBezeichnung = a.Lagerort?.Bezeichnung ?? "Unbekannt",
                            EinheitText = a.IstEinzelteil ? "Einzelteil" : "Karton mit mehreren Einzelteilen",
                            Mindestbestand = a.Mindestbestand,
                            Maximalbestand = a.Maximalbestand,
                            OriginalArtikel = a
                        };

                        lagerbestandDtos.Add(dto);
                    }

                    // ObservableCollection aktualisieren (UI wird automatisch aktualisiert)
                    _lagerbestand.Clear();
                    foreach (var dto in lagerbestandDtos)
                    {
                        _lagerbestand.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des Lagerbestands: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Suchtext ändert
        /// </summary>
        private void TxtSuche_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Filter der CollectionView aktualisieren
            _lagerbestandView.Refresh();
        }

        /// <summary>
        /// Filterfunktion für die Suche in allen Spalten
        /// </summary>
        private bool FilterLagerbestand(object item)
        {
            if (item is LagerbestandAnzeigeDto bestand)
            {
                string suchtext = txtSuche.Text?.ToLower() ?? "";

                // Suche in allen Feldern des Lagerbestands
                return string.IsNullOrEmpty(suchtext) ||
                       bestand.Bezeichnung.ToLower().Contains(suchtext) ||
                       bestand.LagerortBezeichnung.ToLower().Contains(suchtext) ||
                       bestand.EinheitText.ToLower().Contains(suchtext) ||
                       bestand.Bestand.ToString().Contains(suchtext) ||
                       bestand.Mindestbestand.ToString().Contains(suchtext) ||
                       bestand.Maximalbestand.ToString().Contains(suchtext);
            }
            return false;
        }

        /// <summary>
        /// Behandelt Rechtsklick auf DataGrid-Zeilen
        /// </summary>
        private void DgLagerbestand_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Finde das geklickte Element
            var hitTest = dgLagerbestand.InputHitTest(e.GetPosition(dgLagerbestand));

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
                dgLagerbestand.SelectedItem = row.Item;

                // ContextMenu ist bereits im XAML definiert, daher automatisch verfügbar
            }
        }

        /// <summary>
        /// Öffnet das Neuer-Wareneingang-Fenster für den ausgewählten Artikel
        /// </summary>
        private void MenuItemNeuerWareneingang_Click(object sender, RoutedEventArgs e)
        {
            if (dgLagerbestand.SelectedItem is LagerbestandAnzeigeDto selectedBestand)
            {
                try
                {
                    // Neuer-Wareneingang-Fenster als modalen Dialog öffnen
                    var wareneingangFenster = new NeuerWarenEingang(selectedBestand.OriginalArtikel);
                    wareneingangFenster.Owner = Window.GetWindow(this);

                    // Nach dem Schließen des Fensters prüfen ob Wareneingang erfolgreich war
                    if (wareneingangFenster.ShowDialog() == true)
                    {
                        // Lagerbestand neu laden um aktuelle Bestände anzuzeigen
                        RefreshLagerbestand();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen des Wareneingang-Fensters: {ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Öffnet das Barcode-Anzeige-Fenster für den ausgewählten Artikel
        /// </summary>
        private void MenuItemBarcodes_Click(object sender, RoutedEventArgs e)
        {
            if (dgLagerbestand.SelectedItem is LagerbestandAnzeigeDto selectedBestand)
            {
                try
                {
                    // Barcode-Anzeige-Fenster als modalen Dialog öffnen
                    var barcodeAnzeigeFenster = new BarcodeAnzeigen(selectedBestand.OriginalArtikel);
                    barcodeAnzeigeFenster.Owner = Window.GetWindow(this);

                    // Dialog anzeigen
                    barcodeAnzeigeFenster.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen des Barcode-Fensters: {ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void MenuItemManuellesAuslagern_Click(object sender, RoutedEventArgs e)
        {
            if (dgLagerbestand.SelectedItem is LagerbestandAnzeigeDto selectedBestand)
            {
                try
                {
                    // Zuerst prüfen ob Artikel überhaupt Bestand hat
                    if (selectedBestand.Bestand <= 0)
                    {
                        MessageBox.Show("Dieser Artikel hat keinen Bestand zum Auslagern.",
                            "Kein Bestand vorhanden", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Manuelles-Auslagerungs-Fenster als modalen Dialog öffnen
                    var manuellesAuslagernFenster = new ManuellesAuslagern(selectedBestand.OriginalArtikel);
                    manuellesAuslagernFenster.Owner = Window.GetWindow(this);

                    // Dialog anzeigen und nach dem Schließen Bestandsmonitor ausführen
                    var dialogResult = manuellesAuslagernFenster.ShowDialog();

                    // Wenn Artikel ausgelagert wurden, Bestandsmonitor für alle betroffenen Artikel ausführen
                    if (dialogResult == true && manuellesAuslagernFenster.BetroffeneArtikelIds.Any())
                    {
                        // Bestandsmonitor für jeden betroffenen Artikel ausführen
                        foreach (int artikelId in manuellesAuslagernFenster.BetroffeneArtikelIds)
                        {
                            await BestandsMonitor.PruefeBestandNachAenderungAsync(artikelId);
                        }

                        // Debug-Information
                        System.Diagnostics.Debug.WriteLine($"Bestandsmonitor ausgeführt für {manuellesAuslagernFenster.BetroffeneArtikelIds.Count} Artikel nach manueller Auslagerung");

                        // Lagerbestand neu laden um aktuelle Bestände anzuzeigen
                        RefreshLagerbestand();

                        // Optional: Kurze Erfolgsmeldung für Benutzer
                        MessageBox.Show("Auslagerung abgeschlossen und Bestände aktualisiert.",
                            "Erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen des Manuellen-Auslagerungs-Fensters: {ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        /// <summary>
        /// Schließt die Lagerbestand-Ansicht
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ClearMainContent();
        }

        /// <summary>
        /// Öffentliche Methode zum Aktualisieren des Bestands
        /// Wird nach Wareneingang aufgerufen um die Anzeige zu aktualisieren
        /// </summary>
        public void RefreshLagerbestand()
        {
            LoadLagerbestandAsync();
        }
    }
}