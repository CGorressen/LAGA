using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Standard-Startfenster mit Scanner-Integration
    /// Öffnet sich beim Programmstart und nach dem Schließen anderer Ansichten
    /// Erweitert um automatische Bestandsüberwachung nach Scanner-Auslagerung
    /// </summary>
    public partial class StartFenster : UserControl
    {
        public StartFenster()
        {
            InitializeComponent();

            // Fokus auf Scanner-Eingabefeld setzen beim Laden
            this.Loaded += (s, e) => SetScannerFocus();

            // Fokus wieder setzen bei Klick auf das UserControl
            this.MouseDown += (s, e) => SetScannerFocus();

            // Warnmeldungen-Status laden
            LoadWarnmeldungenStatus();
        }

        /// <summary>
        /// Setzt den Fokus auf das Scanner-Eingabefeld
        /// </summary>
        private void SetScannerFocus()
        {
            if (this.IsVisible && this.IsLoaded)
            {
                txtScannerInput.Focus();
            }
        }

        /// <summary>
        /// Deaktiviert Scanner-Events (wird nicht mehr benötigt)
        /// </summary>
        public void DisableScannerEvents()
        {
            // Nicht mehr benötigt
        }

        /// <summary>
        /// Aktiviert Scanner-Events (wird nicht mehr benötigt)
        /// </summary>
        public void EnableScannerEvents()
        {
            // Nicht mehr benötigt
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Text im Scanner-Eingabefeld ändert
        /// </summary>
        private void TxtScannerInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Hier passiert erstmal nichts - die eigentliche Verarbeitung erfolgt bei Enter/KeyDown
        }

        /// <summary>
        /// Behandelt Tastatur-Eingaben im Scanner-Eingabefeld
        /// Scanner sendet normalerweise Enter nach dem Barcode
        /// </summary>
        private void TxtScannerInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                // Barcode-Text auslesen
                string barcode = txtScannerInput.Text.Trim();

                if (!string.IsNullOrEmpty(barcode))
                {
                    // Barcode verarbeiten
                    ProcessScannedBarcode(barcode);

                    // Eingabefeld leeren für nächsten Scan
                    txtScannerInput.Clear();
                }

                // Event als behandelt markieren
                e.Handled = true;
            }
        }

        /// <summary>
        /// Verarbeitet einen gescannten Barcode und öffnet das Auslagern-Fenster
        /// Erweitert um automatische Bestandsüberwachung nach Auslagerung
        /// </summary>
        private async void ProcessScannedBarcode(string barcode)
        {
            try
            {
                // Prüfen ob Barcode in der Datenbank existiert
                using (var context = new LagerContext())
                {
                    var artikelEinheit = await context.ArtikelEinheiten
                        .Include(ae => ae.Artikel)
                        .FirstOrDefaultAsync(ae => ae.Barcode == barcode);

                    if (artikelEinheit != null)
                    {
                        // Gültiger Barcode - Auslagern-Fenster öffnen
                        await OpenArtikelAuslagernDialogAsync(barcode, artikelEinheit);
                    }
                    else
                    {
                        // Ungültiger Barcode - Fehlermeldung
                        ShowBarcodeError($"Barcode '{barcode}' wurde nicht im System gefunden.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowBarcodeError($"Fehler beim Verarbeiten des Barcodes: {ex.Message}");
            }
        }

        /// <summary>
        /// Öffnet das ArtikelAuslagern-Dialog mit dem gescannten Barcode
        /// Erweitert um automatische Bestandsüberwachung nach Auslagerung für ALLE betroffenen Artikel
        /// </summary>
        private async Task OpenArtikelAuslagernDialogAsync(string barcode, ArtikelEinheit artikelEinheit)
        {
            try
            {
                // ArtikelAuslagern-Dialog als modales Fenster öffnen
                var auslagernDialog = new ArtikelAuslagern(barcode, artikelEinheit);
                auslagernDialog.Owner = Window.GetWindow(this);

                // Dialog anzeigen und nach dem Schließen Bestandsmonitor ausführen
                var dialogResult = auslagernDialog.ShowDialog();

                // WICHTIG: Wenn Artikel ausgelagert wurden, Bestandsmonitor für ALLE betroffenen Artikel ausführen
                if (dialogResult == true && auslagernDialog.BetroffeneArtikelIds.Any())
                {
                    // Bestandsmonitor für jeden betroffenen Artikel ausführen
                    foreach (int artikelId in auslagernDialog.BetroffeneArtikelIds)
                    {
                        await BestandsMonitor.PruefeBestandNachAenderungAsync(artikelId);
                    }

                    // Debug-Information
                    System.Diagnostics.Debug.WriteLine($"Bestandsmonitor ausgeführt für {auslagernDialog.BetroffeneArtikelIds.Count} verschiedene Artikel");
                }
                else if (dialogResult == true)
                {
                    // Fallback: Nur der ursprünglich gescannte Artikel
                    await BestandsMonitor.PruefeBestandNachAenderungAsync(artikelEinheit.ArtikelId);
                }

                // Nach dem Schließen Fokus wieder auf Scanner setzen
                SetScannerFocus();
            }
            catch (Exception ex)
            {
                ShowBarcodeError($"Fehler beim Öffnen des Auslagern-Dialogs: {ex.Message}");
            }
        }

        /// <summary>
        /// Zeigt eine Barcode-Fehlermeldung an
        /// </summary>
        private void ShowBarcodeError(string message)
        {
            MessageBox.Show(message, "Barcode-Fehler",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            // Fokus zurück auf Scanner-Eingabe
            SetScannerFocus();
        }

        /// <summary>
        /// Lädt den aktuellen Status der Warnmeldungen (TODO: wird später implementiert)
        /// </summary>
        private void LoadWarnmeldungenStatus()
        {
            // TODO: Hier wird später die Warnmeldungen-Logik implementiert
            // Momentan zeigen wir immer "keine Warnmeldungen"
            txtWarnmeldungen.Text = "Aktuell sind keine Warnmeldungen vorhanden.";
            borderWarnmeldungen.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(64, 64, 64)); // #FF404040
        }
    }
}