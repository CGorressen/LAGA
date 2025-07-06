using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Standard-Startfenster mit Scanner-Integration
    /// Öffnet sich beim Programmstart und nach dem Schließen anderer Ansichten
    /// Erweitert um automatische Bestandsüberwachung nach Scanner-Auslagerung
    /// NEU: Warn-Info-Fenster für nicht versendete E-Mail-Warnungen
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

            // NEU: Prüfe nicht versendete Warnungen beim Laden
            _ = PruefeNichtVersendeteWarnungenAsync();
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
        /// NEU: Aktualisiert StartFenster nach Auslagerung für sofortige Warnungs-Anzeige
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

                    // NEU: StartFenster nach Auslagerung aktualisieren
                    await AktualisiereStartFensterNachAuslagerungAsync();
                }
                else if (dialogResult == true)
                {
                    // Fallback: Nur der ursprünglich gescannte Artikel
                    await BestandsMonitor.PruefeBestandNachAenderungAsync(artikelEinheit.ArtikelId);

                    // NEU: StartFenster auch bei Fallback aktualisieren
                    await AktualisiereStartFensterNachAuslagerungAsync();
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
        /// Lädt den aktuellen Status der Warnmeldungen für die Anzeige
        /// </summary>
        private async void LoadWarnmeldungenStatus()
        {
            try
            {
                // Alle Warn-Artikel laden
                var warnArtikel = await BestandsMonitor.GetWarnArtikelAsync();

                if (warnArtikel.Count > 0)
                {
                    // Rote Anzeige für aktive Warnungen
                    var ellipse = borderWarnmeldungen.Child as StackPanel;
                    if (ellipse?.Children[0] is Ellipse punkt)
                    {
                        punkt.Fill = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF44336"));
                    }

                    txtWarnmeldungen.Text = warnArtikel.Count == 1
                        ? "1 Warnmeldung im System vorhanden."
                        : $"{warnArtikel.Count} Warnmeldungen im System vorhanden.";
                }
                else
                {
                    // Grüne Anzeige für keine Warnungen
                    var ellipse = borderWarnmeldungen.Child as StackPanel;
                    if (ellipse?.Children[0] is Ellipse punkt)
                    {
                        punkt.Fill = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF4CAF50"));
                    }

                    txtWarnmeldungen.Text = "Aktuell sind keine Warnmeldungen vorhanden.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Laden des Warnmeldungen-Status: {ex.Message}");
            }
        }

        // ===============================================
        // NEU: WARN-INFO-FENSTER FUNKTIONALITÄT
        // ===============================================

        /// <summary>
        /// Aktualisiert das StartFenster nach einer Auslagerung
        /// Lädt Warnmeldungen-Status neu und prüft auf neue nicht versendete Warnungen
        /// </summary>
        private async Task AktualisiereStartFensterNachAuslagerungAsync()
        {
            try
            {
                // Kurze Verzögerung damit die Datenbank-Änderungen sicher übernommen sind
                await Task.Delay(500);

                // Warnmeldungen-Status in der oberen rechten Ecke aktualisieren
                LoadWarnmeldungenStatus();

                // Prüfen ob neue nicht versendete Warnungen entstanden sind
                await PruefeNichtVersendeteWarnungenAsync();

                System.Diagnostics.Debug.WriteLine("✅ StartFenster nach Auslagerung aktualisiert");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Aktualisieren des StartFensters: {ex.Message}");
            }
        }

        /// <summary>
        /// Prüft asynchron ob es nicht versendete Warnungen gibt und zeigt das Warn-Info-Fenster an
        /// Smart Logic: Zeigt nur an wenn tatsächlich "nicht versendet" Artikel vorhanden sind
        /// </summary>
        private async Task PruefeNichtVersendeteWarnungenAsync()
        {
            try
            {
                // Alle Warn-Artikel laden
                var warnArtikel = await BestandsMonitor.GetWarnArtikelAsync();

                // Filtern nach nicht erfolgreich versendeten E-Mails
                var nichtVersendeteWarnungen = warnArtikel
                    .Where(w => !w.IstBenachrichtigungErfolgreich)
                    .ToList();

                // Warn-Info-Fenster nur anzeigen wenn nicht versendete Warnungen existieren
                if (nichtVersendeteWarnungen.Count > 0)
                {
                    // Nachricht basierend auf Anzahl anpassen
                    string nachricht = nichtVersendeteWarnungen.Count == 1
                        ? "Es befindet sich eine Warnung im System, die nicht versendet wurde."
                        : $"Es befinden sich {nichtVersendeteWarnungen.Count} Warnungen im System, die nicht versendet wurden.";

                    txtWarnInfoMessage.Text = nachricht;

                    // Warn-Info-Fenster mit Animation anzeigen
                    ZeigWarnInfoFensterAsync();

                    System.Diagnostics.Debug.WriteLine($"⚠️ Warn-Info-Fenster angezeigt: {nichtVersendeteWarnungen.Count} nicht versendete Warnungen");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ Keine nicht versendeten Warnungen - Warn-Info-Fenster nicht angezeigt");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Prüfen nicht versendeter Warnungen: {ex.Message}");
            }
        }

        /// <summary>
        /// Zeigt das Warn-Info-Fenster mit eleganter Slide-Animation an
        /// NON-BLOCKING: Unterbricht den Scanner-Workflow nicht
        /// Positioned am unteren Rand: Slided nur soweit hoch bis vollständig sichtbar
        /// </summary>
        private async void ZeigWarnInfoFensterAsync()
        {
            try
            {
                // Warn-Info-Fenster sichtbar machen (ohne Modal-Overlay)
                borderWarnInfo.Visibility = Visibility.Visible;

                // Slide-In Animation (von außerhalb nach unten ins Sichtfeld)
                // Startet komplett außerhalb und endet am unteren Rand
                var slideInAnimation = new DoubleAnimation
                {
                    From = 200,  // Startet außerhalb des Fensters (unten)
                    To = 0,      // Endet am unteren Rand (vollständig sichtbar)
                    Duration = TimeSpan.FromSeconds(1.2),
                    EasingFunction = new BackEase
                    {
                        EasingMode = EasingMode.EaseOut,
                        Amplitude = 0.3
                    }
                };

                // Animation starten
                transformWarnInfo.BeginAnimation(TranslateTransform.YProperty, slideInAnimation);

                // Nach 5 Sekunden automatisch ausblenden
                await Task.Delay(5000);
                VersteckeWarnInfoFensterAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Anzeigen des Warn-Info-Fensters: {ex.Message}");
            }
        }

        /// <summary>
        /// Versteckt das Warn-Info-Fenster mit eleganter Slide-Down Animation
        /// Slided zurück nach außerhalb des sichtbaren Bereichs
        /// </summary>
        private void VersteckeWarnInfoFensterAsync()
        {
            try
            {
                // Slide-Out Animation (vom unteren Rand nach außerhalb)
                var slideOutAnimation = new DoubleAnimation
                {
                    From = 0,     // Startet am unteren Rand (sichtbar)
                    To = 200,     // Endet außerhalb des Fensters (unsichtbar)
                    Duration = TimeSpan.FromSeconds(0.8),
                    EasingFunction = new QuadraticEase
                    {
                        EasingMode = EasingMode.EaseIn
                    }
                };

                // Nach Animation-Ende Warn-Info-Fenster komplett ausblenden
                slideOutAnimation.Completed += (s, e) =>
                {
                    borderWarnInfo.Visibility = Visibility.Collapsed;
                };

                // Animation starten
                transformWarnInfo.BeginAnimation(TranslateTransform.YProperty, slideOutAnimation);

                System.Diagnostics.Debug.WriteLine("✅ Warn-Info-Fenster automatisch ausgeblendet");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Verstecken des Warn-Info-Fensters: {ex.Message}");
            }
        }
    }
}