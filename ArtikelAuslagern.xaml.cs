using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// DTO für die Anzeige gescannter Artikel in der Auslagern-Tabelle
    /// </summary>
    public class GescannterArtikelDto
    {
        public string Artikelbezeichnung { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public ArtikelEinheit OriginalEinheit { get; set; } = new ArtikelEinheit();
    }

    /// <summary>
    /// Modales Fenster für das Auslagern von Artikeln mittels Barcode-Scanner
    /// </summary>
    public partial class ArtikelAuslagern : Window
    {
        /// <summary>
        /// Observable Collection für die gescannten Artikel
        /// </summary>
        private ObservableCollection<GescannterArtikelDto> _gescannteArtikel;

        /// <summary>
        /// Timer für automatische Fokus-Rücksetzung auf Scanner-Eingabefeld
        /// </summary>
        private readonly System.Windows.Threading.DispatcherTimer _focusTimer;

        /// <summary>
        /// Flag um zu verhindern, dass während einer Verarbeitung weitere Scans verarbeitet werden
        /// </summary>
        private bool _isProcessing = false;

        public ArtikelAuslagern(string initialBarcode, ArtikelEinheit initialEinheit)
        {
            InitializeComponent();

            // Observable Collection initialisieren
            _gescannteArtikel = new ObservableCollection<GescannterArtikelDto>();
            dgGescannteArtikel.ItemsSource = _gescannteArtikel;

            // Timer für automatische Fokus-Rücksetzung
            _focusTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _focusTimer.Tick += (s, e) => SetScannerFocus();
            _focusTimer.Start();

            // Fokus auf Scanner-Eingabefeld setzen
            this.Loaded += (s, e) => SetScannerFocus();

            // Ersten Artikel hinzufügen (vom StartFenster übertragen)
            AddScannedArticle(initialBarcode, initialEinheit);
        }

        /// <summary>
        /// Setzt den Fokus auf das Scanner-Eingabefeld
        /// </summary>
        private void SetScannerFocus()
        {
            if (!_isProcessing)
            {
                txtScannerInput.Focus();
            }
        }

        /// <summary>
        /// Fügt einen gescannten Artikel zur Liste hinzu
        /// </summary>
        private void AddScannedArticle(string barcode, ArtikelEinheit einheit)
        {
            var dto = new GescannterArtikelDto
            {
                Artikelbezeichnung = einheit.Artikel?.Bezeichnung ?? "Unbekannt",
                Barcode = barcode,
                OriginalEinheit = einheit
            };

            _gescannteArtikel.Add(dto);
            UpdateAuslagernButtonStatus();
        }

        /// <summary>
        /// Aktualisiert den Status des Auslagern-Buttons
        /// </summary>
        private void UpdateAuslagernButtonStatus()
        {
            btnAuslagern.IsEnabled = _gescannteArtikel.Count > 0;
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Text im Scanner-Eingabefeld ändert
        /// </summary>
        private void TxtScannerInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Hier passiert nichts - Verarbeitung erfolgt bei Enter
        }

        /// <summary>
        /// Behandelt Scanner-Eingaben
        /// </summary>
        private void TxtScannerInput_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Return) && !_isProcessing)
            {
                string barcode = txtScannerInput.Text.Trim();

                if (!string.IsNullOrEmpty(barcode))
                {
                    ProcessScannedBarcode(barcode);
                    txtScannerInput.Clear();
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Verarbeitet einen gescannten Barcode
        /// </summary>
        private async void ProcessScannedBarcode(string barcode)
        {
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;

                // Prüfen ob Barcode bereits in der Liste ist
                if (_gescannteArtikel.Any(a => a.Barcode == barcode))
                {
                    ShowBarcodeError($"Barcode '{barcode}' wurde bereits gescannt!");
                    return;
                }

                // Barcode in Datenbank suchen
                using (var context = new LagerContext())
                {
                    var artikelEinheit = await context.ArtikelEinheiten
                        .Include(ae => ae.Artikel)
                        .FirstOrDefaultAsync(ae => ae.Barcode == barcode);

                    if (artikelEinheit != null)
                    {
                        // Gültiger Barcode - zur Liste hinzufügen
                        AddScannedArticle(barcode, artikelEinheit);
                    }
                    else
                    {
                        // Ungültiger Barcode
                        ShowBarcodeError($"Barcode '{barcode}' wurde nicht im System gefunden!");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowBarcodeError($"Fehler beim Verarbeiten des Barcodes: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                SetScannerFocus();
            }
        }

        /// <summary>
        /// Zeigt eine Barcode-Fehlermeldung an
        /// </summary>
        private void ShowBarcodeError(string message)
        {
            MessageBox.Show(message, "Barcode-Fehler",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Entfernt einen Artikel aus der Liste
        /// </summary>
        private void BtnEntfernen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GescannterArtikelDto dto)
            {
                _gescannteArtikel.Remove(dto);
                UpdateAuslagernButtonStatus();
                SetScannerFocus();
            }
        }

        /// <summary>
        /// Führt das Auslagern aller gescannten Artikel durch
        /// </summary>
        private async void BtnAuslagern_Click(object sender, RoutedEventArgs e)
        {
            if (_gescannteArtikel.Count == 0) return;

            // Bestätigungsdialog
            var result = MessageBox.Show(
                "Sind Sie sicher, dass Sie die Artikel auslagern möchten?",
                "Artikel auslagern",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    btnAuslagern.IsEnabled = false;
                    btnAuslagern.Content = "Lagert aus...";

                    // Alle ArtikelEinheiten aus der Datenbank löschen
                    using (var context = new LagerContext())
                    {
                        var einheitenIds = _gescannteArtikel.Select(a => a.OriginalEinheit.Id).ToList();

                        var einheitenZumLoeschen = await context.ArtikelEinheiten
                            .Where(ae => einheitenIds.Contains(ae.Id))
                            .ToListAsync();

                        context.ArtikelEinheiten.RemoveRange(einheitenZumLoeschen);
                        await context.SaveChangesAsync();
                    }

                    MessageBox.Show($"Erfolgreich {_gescannteArtikel.Count} Artikel ausgelagert!",
                        "Auslagern abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Dialog schließen
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Auslagern: {ex.Message}",
                        "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btnAuslagern.Content = "Auslagern";
                    btnAuslagern.IsEnabled = _gescannteArtikel.Count > 0;
                }
            }
        }

        /// <summary>
        /// Schließt das Dialog ohne Auslagern
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Wird aufgerufen wenn das Fenster geschlossen wird
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _focusTimer?.Stop();
            base.OnClosed(e);
        }
    }
}