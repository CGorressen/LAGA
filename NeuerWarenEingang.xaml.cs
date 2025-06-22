using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster für den Wareneingang - ermöglicht das Einlagern von Artikel-Einheiten
    /// mit automatischer Barcode-Generierung und ZPL-Etikett-Druck für Zebra GX420t
    /// Erweitert um automatische Bestandsüberwachung nach Einlagerung
    /// </summary>
    public partial class NeuerWarenEingang : Window
    {
        /// <summary>
        /// Der Artikel für den der Wareneingang durchgeführt wird
        /// </summary>
        private readonly Artikel _artikel;

        /// <summary>
        /// Random-Generator für Barcode-Erstellung
        /// </summary>
        private readonly Random _random;

        public NeuerWarenEingang(Artikel artikel)
        {
            InitializeComponent();

            _artikel = artikel ?? throw new ArgumentNullException(nameof(artikel));
            _random = new Random();

            // Artikelbezeichnung anzeigen (schreibgeschützt)
            txtArtikelbezeichnung.Text = _artikel.Bezeichnung;

            // Fokus auf Stückzahl-Feld setzen
            txtStueckzahl.Focus();
        }

        /// <summary>
        /// Wird ausgelöst wenn sich die Stückzahl ändert
        /// Validiert die Eingabe und aktiviert/deaktiviert den Einlagern-Button
        /// </summary>
        private void TxtStueckzahl_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        /// <summary>
        /// Validiert die Stückzahl-Eingabe und aktiviert/deaktiviert den Einlagern-Button
        /// </summary>
        private void ValidateInput()
        {
            bool isValid = false;

            // Prüfen ob Eingabe eine gültige positive Zahl ist
            if (int.TryParse(txtStueckzahl.Text, out int stueckzahl))
            {
                if (stueckzahl > 0)
                {
                    isValid = true;
                    // Status zurücksetzen
                    txtStatus.Visibility = Visibility.Collapsed;
                }
                else if (stueckzahl <= 0)
                {
                    ShowStatus("Stückzahl muss größer als 0 sein.", Brushes.Orange);
                }
            }
            else if (!string.IsNullOrWhiteSpace(txtStueckzahl.Text))
            {
                ShowStatus("Bitte geben Sie eine gültige Zahl ein.", Brushes.Orange);
            }
            else
            {
                // Leeres Feld - Status verstecken
                txtStatus.Visibility = Visibility.Collapsed;
            }

            btnEinlagern.IsEnabled = isValid;
        }

        /// <summary>
        /// Zeigt eine Status-Nachricht an
        /// </summary>
        private void ShowStatus(string message, Brush color)
        {
            txtStatus.Text = message;
            txtStatus.Foreground = color;
            txtStatus.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Führt den Wareneingang durch - erstellt ArtikelEinheiten mit eindeutigen Barcodes
        /// und druckt ZPL-Etiketten auf dem Zebra GX420t
        /// Erweitert um automatische Bestandsüberwachung nach Einlagerung
        /// </summary>
        private async void BtnEinlagern_Click(object sender, RoutedEventArgs e)
        {
            // Finale Validierung
            if (!int.TryParse(txtStueckzahl.Text, out int stueckzahl) || stueckzahl <= 0)
            {
                ShowStatus("Ungültige Stückzahl!", Brushes.Red);
                return;
            }

            try
            {
                // Button während des Einlagerns deaktivieren
                btnEinlagern.IsEnabled = false;
                btnEinlagern.Content = "Lagert ein...";
                ShowStatus($"Lagere {stueckzahl} Einheiten ein...", Brushes.Blue);

                // Aktuelles Datum und Uhrzeit für alle Einheiten dieses Wareneingangs
                DateTime erstellungsDatum = DateTime.Now;

                // Liste für die neuen ArtikelEinheiten
                var neueEinheiten = new List<ArtikelEinheit>();

                // Für jede Stückzahl eine ArtikelEinheit mit eindeutigem Barcode erstellen
                for (int i = 0; i < stueckzahl; i++)
                {
                    string barcode = await GenerateUniqueBarcode();

                    var einheit = new ArtikelEinheit
                    {
                        ArtikelId = _artikel.Id,
                        Barcode = barcode,
                        ErstellungsDatum = erstellungsDatum // Alle Einheiten dieses Batches haben dasselbe ErstellungsDatum
                    };

                    neueEinheiten.Add(einheit);
                }

                // Alle Einheiten in die Datenbank speichern
                using (var context = new LagerContext())
                {
                    context.ArtikelEinheiten.AddRange(neueEinheiten);
                    await context.SaveChangesAsync();
                }

                ShowStatus($"✓ {stueckzahl} Einheiten erfolgreich eingelagert!", Brushes.Green);

                // ZPL-Etiketten für Zebra GX420t erstellen und drucken
                try
                {
                    ShowStatus("Erstelle ZPL-Etiketten für Zebra GX420t...", Brushes.Blue);

                    bool druckErfolgreich = await ZebraEtikettService.ErstelleUndDruckeEtikettenAsync(
                        neueEinheiten, _artikel);

                    if (druckErfolgreich)
                    {
                        ShowStatus("✓ ZPL-Etiketten erstellt und an Zebra gedruckt!", Brushes.Green);

                        MessageBox.Show($"Wareneingang erfolgreich abgeschlossen!\n\n" +
                                       $"Artikel: {_artikel.Bezeichnung}\n" +
                                       $"Eingelagerte Stück: {stueckzahl}\n",
                                       "Wareneingang abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);

                        // WICHTIG: Bestandsmonitor für Warnungs-Prüfung nach Einlagerung
                        await BestandsMonitor.PruefeBestandNachAenderungAsync(_artikel.Id);
                    }
                    else
                    {
                        ShowStatus("⚠ Einlagerung OK, aber Zebra-Druck-Fehler", Brushes.Orange);

                        MessageBox.Show($"Wareneingang erfolgreich, aber Probleme beim Zebra-Druck.\n\n" +
                                       $"Die Artikel-Einheiten wurden korrekt eingelagert.\n\n" +
                                       $"Lösungsvorschläge:\n" +
                                       $"• USB-Verbindung zum Zebra GX420t prüfen\n" +
                                       $"• Zebra-Treiber installieren/aktualisieren\n" +
                                       $"• Drucker-Name in Windows überprüfen\n" +
                                       $"• Test-Etikett über Zebra Setup Utilities drucken\n\n" +
                                       $"ZPL-Dateien wurden gespeichert und können manuell gedruckt werden.",
                                       "Zebra-Drucker Problem", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // WICHTIG: Bestandsmonitor für Warnungs-Prüfung nach Einlagerung (auch bei Druck-Fehlern)
                        await BestandsMonitor.PruefeBestandNachAenderungAsync(_artikel.Id);
                    }
                }
                catch (Exception etikettenEx)
                {
                    ShowStatus("⚠ Einlagerung OK, aber Zebra-Druck-Fehler", Brushes.Orange);

                    // Prüfen ob ZPL-Dateien erstellt wurden, auch wenn Druck fehlschlug
                    string zplVerzeichnis = ZebraEtikettService.GetEtikettenVerzeichnis();
                    bool zplDateienErstellt = Directory.GetFiles(zplVerzeichnis, "*.zpl")
                        .Any(f => Path.GetFileName(f).StartsWith($"{_artikel.Id}_"));

                    if (zplDateienErstellt)
                    {
                        var result = MessageBox.Show($"Wareneingang erfolgreich!\n\n" +
                                       $"Die ZPL-Etiketten wurden erstellt, aber der Zebra-Druck war nicht möglich:\n" +
                                       $"{etikettenEx.Message}\n\n" +
                                       $"Lösungsmöglichkeiten:\n" +
                                       $"• Zebra GX420t USB-Verbindung prüfen\n" +
                                       $"• Zebra-Treiber neu installieren\n" +
                                       $"• ZPL-Dateien manuell drucken über Zebra Setup Utilities\n\n" +
                                       $"ZPL-Verzeichnis:\n{zplVerzeichnis}\n\n" +
                                       $"Soll das ZPL-Verzeichnis jetzt geöffnet werden?",
                                       "Manueller Zebra-Druck erforderlich",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            ZebraTestService.OeffneZPLVerzeichnis();
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Wareneingang erfolgreich, aber Fehler beim Erstellen der Zebra-Etiketten:\n\n" +
                                       $"{etikettenEx.Message}\n\n" +
                                       $"Die Artikel-Einheiten wurden korrekt eingelagert.\n\n" +
                                       $"Bitte prüfen Sie:\n" +
                                       $"• Zebra GX420t Drucker-Konfiguration\n" +
                                       $"• Windows-Drucker-Einstellungen\n" +
                                       $"• USB/Netzwerk-Verbindung",
                                       "Zebra-Etikett Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    // WICHTIG: Bestandsmonitor für Warnungs-Prüfung nach Einlagerung (auch bei Etikett-Fehlern)
                    await BestandsMonitor.PruefeBestandNachAenderungAsync(_artikel.Id);
                }

                // Fenster schließen und Erfolg signalisieren
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Fehler beim Einlagern: {ex.Message}", Brushes.Red);
                MessageBox.Show($"Fehler beim Wareneingang: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                btnEinlagern.Content = "Einlagern";
                ValidateInput(); // Status neu bewerten
            }
        }

        /// <summary>
        /// Generiert einen eindeutigen 10-stelligen Barcode
        /// Prüft Eindeutigkeit in der Datenbank
        /// </summary>
        private async Task<string> GenerateUniqueBarcode()
        {
            string barcode;
            bool isUnique;

            do
            {
                // 10-stellige Zufallszahl generieren
                barcode = GenerateRandomBarcode();

                // Eindeutigkeit in Datenbank prüfen
                using (var context = new LagerContext())
                {
                    isUnique = !await context.ArtikelEinheiten
                        .AnyAsync(e => e.Barcode == barcode);
                }

            } while (!isUnique);

            return barcode;
        }

        /// <summary>
        /// Generiert eine zufällige 10-stellige Barcode-Nummer
        /// </summary>
        private string GenerateRandomBarcode()
        {
            // 10-stellige Zahl: von 1000000000 bis 9999999999
            long minValue = 1000000000L;
            long maxValue = 9999999999L;

            // Random.NextInt64 ist ab .NET 6 verfügbar
            long randomNumber = _random.NextInt64(minValue, maxValue + 1);

            return randomNumber.ToString();
        }

        /// <summary>
        /// Schließt das Fenster ohne Wareneingang
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}