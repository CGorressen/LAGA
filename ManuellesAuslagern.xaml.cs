using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster für die manuelle Auslagerung von Artikeln über Barcode-Auswahl
    /// EXAKT identisch mit BarcodeAnzeigen, aber für Auslagerung statt Neudruck
    /// WICHTIG: Keine automatische Selektion der neuesten Barcodes!
    /// </summary>
    public partial class ManuellesAuslagern : Window
    {
        /// <summary>
        /// Der Artikel dessen Barcodes zur Auslagerung angezeigt werden
        /// </summary>
        private readonly Artikel _artikel;

        /// <summary>
        /// Observable Collection für die Barcode-Auswahl-DTOs (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<ManuellesAuslagernDto> _barcodes;

        /// <summary>
        /// CollectionView für Sortierung und Filterung
        /// </summary>
        private ICollectionView _barcodesView;

        /// <summary>
        /// Toggle-Zustand für Alle auswählen/Alle abwählen
        /// </summary>
        private bool _alleAusgewaehlt = false;

        /// <summary>
        /// Liste der ArtikelIds, die von der Auslagerung betroffen waren (für Bestandsmonitor)
        /// Wird an das aufrufende Fenster zurückgegeben
        /// </summary>
        public List<int> BetroffeneArtikelIds { get; private set; } = new List<int>();

        public ManuellesAuslagern(Artikel artikel)
        {
            InitializeComponent();

            _artikel = artikel ?? throw new ArgumentNullException(nameof(artikel));

            // Collections initialisieren
            _barcodes = new ObservableCollection<ManuellesAuslagernDto>();

            // CollectionView für Sortierung und Filterung erstellen
            _barcodesView = CollectionViewSource.GetDefaultView(_barcodes);
            _barcodesView.Filter = FilterBarcodes;

            // Sortierung nach ErstellungsDatum (neueste zuerst) - identisch mit BarcodeAnzeigen
            _barcodesView.SortDescriptions.Add(new SortDescription("ErstellungsDatum", ListSortDirection.Descending));

            // DataGrid an CollectionView binden
            dgBarcodes.ItemsSource = _barcodesView;

            // Titel setzen (nur Artikelbezeichnung) - identisch mit BarcodeAnzeigen
            txtTitel.Text = _artikel.Bezeichnung;

            // Daten beim Laden asynchron abrufen
            LoadBarcodesAsync();
        }

        /// <summary>
        /// Lädt alle Barcodes des Artikels aus der Datenbank
        /// IDENTISCH mit BarcodeAnzeigen, aber OHNE automatische Selektion der neuesten Barcodes!
        /// </summary>
        private async void LoadBarcodesAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle ArtikelEinheiten dieses Artikels laden, sortiert nach ErstellungsDatum (neueste zuerst)
                    // IDENTISCH mit BarcodeAnzeigen
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

                    // DTOs erstellen - UNTERSCHIED: ALLE bleiben unselektiert (IstAusgewaehlt = false)
                    var barcodeDtos = artikelEinheiten.Select(ae => new ManuellesAuslagernDto
                    {
                        IstAusgewaehlt = false, // WICHTIG: Keine automatische Selektion (Unterschied zu BarcodeAnzeigen)!
                        ErstellungsDatumFormatiert = ae.ErstellungsDatum.ToString("dd.MM.yyyy | HH:mm"),
                        Barcode = ae.Barcode,
                        OriginalEinheit = ae,
                        ErstellungsDatum = ae.ErstellungsDatum
                    }).ToList();

                    // ObservableCollection aktualisieren - identisch mit BarcodeAnzeigen
                    _barcodes.Clear();
                    foreach (var dto in barcodeDtos)
                    {
                        _barcodes.Add(dto);
                    }

                    // Button-Status aktualisieren (sollte deaktiviert sein da nichts ausgewählt)
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
        /// IDENTISCH mit BarcodeAnzeigen
        /// </summary>
        private void TxtSuche_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Filter der CollectionView aktualisieren
            _barcodesView.Refresh();
        }

        /// <summary>
        /// Filterfunktion für die Barcode-Suche
        /// Sucht in Barcode und Erstellungsdatum
        /// IDENTISCH mit BarcodeAnzeigen
        /// </summary>
        private bool FilterBarcodes(object item)
        {
            if (item is ManuellesAuslagernDto barcode)
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
        /// IDENTISCH mit BarcodeAnzeigen
        /// </summary>
        private void BtnToggleAuswahl_Click(object sender, RoutedEventArgs e)
        {
            // Toggle-Zustand umkehren
            _alleAusgewaehlt = !_alleAusgewaehlt;

            // Alle sichtbaren Barcodes entsprechend setzen
            foreach (var barcode in _barcodesView.Cast<ManuellesAuslagernDto>())
            {
                barcode.IstAusgewaehlt = _alleAusgewaehlt;
            }

            // UI aktualisieren
            dgBarcodes.Items.Refresh();
            UpdateButtonStatus();
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Auswahlstatus einer Checkbox ändert
        /// IDENTISCH mit BarcodeAnzeigen
        /// </summary>
        private void Checkbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateButtonStatus();
        }

        /// <summary>
        /// Aktualisiert den Status des "Ausgewählte auslagern" Buttons
        /// IDENTISCH mit BarcodeAnzeigen (nur Button-Text geändert von "Drucken" zu "Auslagern")
        /// </summary>
        private void UpdateButtonStatus()
        {
            int ausgewaehlteAnzahl = _barcodes.Count(b => b.IstAusgewaehlt);
            btnAusgewaehlteDrucken.IsEnabled = ausgewaehlteAnzahl > 0;

            // Button-Text anpassen (EINZIGER Unterschied: "Auslagern" statt "Drucken")
            if (ausgewaehlteAnzahl > 0)
            {
                btnAusgewaehlteDrucken.Content = $"Auslagern ({ausgewaehlteAnzahl})";
            }
            else
            {
                btnAusgewaehlteDrucken.Content = "Auslagern";
            }
        }

        /// <summary>
        /// Lagert die ausgewählten Barcodes aus und führt Bestandsüberwachung durch
        /// UNTERSCHIED zu BarcodeAnzeigen: Auslagerung statt Drucken
        /// </summary>
        private async void BtnAusgewaehlteAuslagern_Click(object sender, RoutedEventArgs e)
        {
            var ausgewaehlteEinheiten = _barcodes
                .Where(b => b.IstAusgewaehlt)
                .Select(b => b.OriginalEinheit)
                .ToList();

            if (!ausgewaehlteEinheiten.Any())
            {
                MessageBox.Show("Bitte wählen Sie mindestens einen Barcode zum Auslagern aus.",
                    "Keine Auswahl", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Bestätigungsdialog (identisch mit ArtikelAuslagern)
            var result = MessageBox.Show(
                $"Sind Sie sicher, dass Sie {ausgewaehlteEinheiten.Count} Artikel auslagern möchten?",
                "Auslagerung bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Button während der Auslagerung deaktivieren
                btnAusgewaehlteDrucken.IsEnabled = false;
                btnAusgewaehlteDrucken.Content = "Lagert aus...";

                // Daten für das Logging sammeln (BEVOR die Einheiten gelöscht werden)
                string artikelBezeichnung = _artikel.Bezeichnung;
                var barcodes = ausgewaehlteEinheiten.Select(e => e.Barcode).ToList();
                int menge = ausgewaehlteEinheiten.Count;

                // Bestand vorher ermitteln
                int bestandVorher;
                using (var context = new LagerContext())
                {
                    bestandVorher = await context.ArtikelEinheiten
                        .CountAsync(ae => ae.ArtikelId == _artikel.Id);

                    // Alle ausgewählten ArtikelEinheiten aus der Datenbank entfernen
                    var einheitenIds = ausgewaehlteEinheiten.Select(e => e.Id).ToList();
                    var zuLoeschendeEinheiten = await context.ArtikelEinheiten
                        .Where(ae => einheitenIds.Contains(ae.Id))
                        .ToListAsync();

                    context.ArtikelEinheiten.RemoveRange(zuLoeschendeEinheiten);
                    await context.SaveChangesAsync();

                    // Liste der betroffenen Artikel-IDs für Bestandsmonitor sammeln
                    var betroffeneArtikelIds = zuLoeschendeEinheiten
                        .Select(e => e.ArtikelId)
                        .Distinct()
                        .ToList();

                    BetroffeneArtikelIds.AddRange(betroffeneArtikelIds);
                }

                // Bestand nachher berechnen
                int bestandNachher = bestandVorher - menge;

                // LAGERBEWEGUNG LOGGEN - Auslagerung dokumentieren
                await LagerbewegungsLogger.LoggeAuslagerungAsync(
                    artikelBezeichnung: artikelBezeichnung,
                    menge: menge,
                    bestandVorher: bestandVorher,
                    bestandNachher: bestandNachher,
                    barcodes: barcodes
                );

                // Erfolgsmeldung
                MessageBox.Show($"Erfolgreich {ausgewaehlteEinheiten.Count} Artikel ausgelagert!",
                    "Auslagerung abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);

                // Dialog mit Erfolg schließen
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Auslagern der Artikel: {ex.Message}",
                    "Auslagerungs-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren falls noch geöffnet
                UpdateButtonStatus();
            }
        }

        /// <summary>
        /// Schließt das Manuelle-Auslagerungs-Fenster ohne Auslagerung
        /// IDENTISCH mit BarcodeAnzeigen
        /// </summary>
        private void BtnSchliessen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}