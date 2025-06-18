using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster zur Anzeige und zum Neudruck von Barcodes eines Artikels
    /// Ermöglicht die Auswahl spezifischer Barcodes zum erneuten Drucken
    /// </summary>
    public partial class BarcodeAnzeigen : Window
    {
        /// <summary>
        /// Der Artikel dessen Barcodes angezeigt werden
        /// </summary>
        private readonly Artikel _artikel;

        /// <summary>
        /// Observable Collection für die Barcode-Anzeige-DTOs (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<BarcodeAnzeigeDto> _barcodes;

        /// <summary>
        /// CollectionView für Sortierung und Filterung
        /// </summary>
        private ICollectionView _barcodesView;

        /// <summary>
        /// Toggle-Zustand für Alle auswählen/Alle abwählen
        /// </summary>
        private bool _alleAusgewaehlt = false;

        public BarcodeAnzeigen(Artikel artikel)
        {
            InitializeComponent();

            _artikel = artikel ?? throw new ArgumentNullException(nameof(artikel));

            // Collections initialisieren
            _barcodes = new ObservableCollection<BarcodeAnzeigeDto>();

            // CollectionView für Sortierung und Filterung erstellen
            _barcodesView = CollectionViewSource.GetDefaultView(_barcodes);
            _barcodesView.Filter = FilterBarcodes;

            // Sortierung nach ErstellungsDatum (neueste zuerst)
            _barcodesView.SortDescriptions.Add(new SortDescription("ErstellungsDatum", ListSortDirection.Descending));

            // DataGrid an CollectionView binden
            dgBarcodes.ItemsSource = _barcodesView;

            // Titel setzen (nur Artikelbezeichnung)
            txtTitel.Text = _artikel.Bezeichnung;

            // Daten beim Laden asynchron abrufen
            LoadBarcodesAsync();
        }

        /// <summary>
        /// Lädt alle Barcodes des Artikels aus der Datenbank
        /// Markiert automatisch die Barcodes vom neuesten Erstellungsdatum
        /// </summary>
        private async void LoadBarcodesAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle ArtikelEinheiten dieses Artikels laden, sortiert nach ErstellungsDatum (neueste zuerst)
                    var artikelEinheiten = await context.ArtikelEinheiten
                        .Where(ae => ae.ArtikelId == _artikel.Id)
                        .OrderByDescending(ae => ae.ErstellungsDatum)
                        .ToListAsync();

                    if (!artikelEinheiten.Any())
                    {
                        MessageBox.Show("Für diesen Artikel wurden noch keine Barcodes erstellt.",
                            "Keine Barcodes vorhanden", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                        return;
                    }

                    // Neuestes ErstellungsDatum für Auto-Selektion ermitteln
                    DateTime neuestesErstellungsDatum = artikelEinheiten.Max(ae => ae.ErstellungsDatum);

                    // DTOs erstellen
                    var barcodeDtos = artikelEinheiten.Select(ae => new BarcodeAnzeigeDto
                    {
                        IstAusgewaehlt = ae.ErstellungsDatum == neuestesErstellungsDatum, // Auto-Selektion des neuesten Batches
                        ErstellungsDatumFormatiert = ae.ErstellungsDatum.ToString("dd.MM.yyyy | HH:mm"),
                        Barcode = ae.Barcode,
                        OriginalEinheit = ae,
                        ErstellungsDatum = ae.ErstellungsDatum
                    }).ToList();

                    // ObservableCollection aktualisieren
                    _barcodes.Clear();
                    foreach (var dto in barcodeDtos)
                    {
                        _barcodes.Add(dto);
                    }

                    // Button-Status aktualisieren
                    UpdateButtonStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Barcodes: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Suchtext ändert
        /// Filtert die Barcode-Liste basierend auf dem Suchtext
        /// </summary>
        private void TxtSuche_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Filter der CollectionView aktualisieren
            _barcodesView.Refresh();
        }

        /// <summary>
        /// Filterfunktion für die Barcode-Suche
        /// Sucht in Barcode und Erstellungsdatum
        /// </summary>
        private bool FilterBarcodes(object item)
        {
            if (item is BarcodeAnzeigeDto barcode)
            {
                string suchtext = txtSuche.Text?.ToLower() ?? "";

                // Suche in Barcode und Erstellungsdatum
                return string.IsNullOrEmpty(suchtext) ||
                       barcode.Barcode.ToLower().Contains(suchtext) ||
                       barcode.ErstellungsDatumFormatiert.ToLower().Contains(suchtext);
            }
            return false;
        }

        /// <summary>
        /// Toggle-Button im Header: Alle auswählen/abwählen
        /// </summary>
        private void BtnToggleAuswahl_Click(object sender, RoutedEventArgs e)
        {
            // Toggle-Zustand umkehren
            _alleAusgewaehlt = !_alleAusgewaehlt;

            // Alle sichtbaren Barcodes entsprechend setzen
            foreach (var barcode in _barcodesView.Cast<BarcodeAnzeigeDto>())
            {
                barcode.IstAusgewaehlt = _alleAusgewaehlt;
            }

            // UI aktualisieren
            dgBarcodes.Items.Refresh();
            UpdateButtonStatus();
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Auswahlstatus einer Checkbox ändert
        /// </summary>
        private void Checkbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateButtonStatus();
        }

        /// <summary>
        /// Aktualisiert den Status des "Ausgewählte drucken" Buttons
        /// </summary>
        private void UpdateButtonStatus()
        {
            int ausgewaehlteAnzahl = _barcodes.Count(b => b.IstAusgewaehlt);
            btnAusgewaehlteDrucken.IsEnabled = ausgewaehlteAnzahl > 0;

            // Button-Text anpassen
            if (ausgewaehlteAnzahl > 0)
            {
                btnAusgewaehlteDrucken.Content = $"Ausgewählte drucken ({ausgewaehlteAnzahl})";
            }
            else
            {
                btnAusgewaehlteDrucken.Content = "Ausgewählte drucken";
            }
        }

        /// <summary>
        /// Druckt die ausgewählten Barcodes über den ZebraEtikettService
        /// </summary>
        private async void BtnAusgewaehlteDrucken_Click(object sender, RoutedEventArgs e)
        {
            var ausgewaehlteEinheiten = _barcodes
                .Where(b => b.IstAusgewaehlt)
                .Select(b => b.OriginalEinheit)
                .ToList();

            if (!ausgewaehlteEinheiten.Any())
            {
                MessageBox.Show("Bitte wählen Sie mindestens einen Barcode zum Drucken aus.",
                    "Keine Auswahl", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Button während des Druckens deaktivieren
                btnAusgewaehlteDrucken.IsEnabled = false;
                btnAusgewaehlteDrucken.Content = "Druckt...";

                // Barcodes über ZebraEtikettService drucken
                bool druckErfolgreich = await ZebraEtikettService.DruckeBestehendeBarcodes(
                    ausgewaehlteEinheiten, _artikel);

                if (druckErfolgreich)
                {
                    MessageBox.Show($"Erfolgreich {ausgewaehlteEinheiten.Count} Barcode(s) gedruckt!",
                        "Druck abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Beim Drucken der Barcodes ist ein Fehler aufgetreten.\n\n" +
                                   "Bitte prüfen Sie:\n" +
                                   "• Zebra GX420t USB-Verbindung\n" +
                                   "• Drucker-Status und Treiber\n" +
                                   "• ZPL-Dateien im Backup-Verzeichnis",
                                   "Druck-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Drucken der Barcodes: {ex.Message}",
                    "Druck-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                UpdateButtonStatus();
            }
        }

        /// <summary>
        /// Schließt das Barcode-Anzeige-Fenster
        /// </summary>
        private void BtnSchliessen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}