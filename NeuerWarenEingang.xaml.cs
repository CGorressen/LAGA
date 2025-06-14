﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster für den Wareneingang - ermöglicht das Einlagern von Artikel-Einheiten
    /// mit automatischer Barcode-Generierung
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

                // Liste für die neuen ArtikelEinheiten
                var neueEinheiten = new List<ArtikelEinheit>();

                // Für jede Stückzahl eine ArtikelEinheit mit eindeutigem Barcode erstellen
                for (int i = 0; i < stueckzahl; i++)
                {
                    string barcode = await GenerateUniqueBarcode();

                    var einheit = new ArtikelEinheit
                    {
                        ArtikelId = _artikel.Id,
                        Barcode = barcode
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

                // PDF-Etiketten erstellen und drucken
                try
                {
                    ShowStatus("Erstelle PDF-Etiketten...", Brushes.Blue);

                    bool druckErfolgreich = await BarcodeEtikettService.ErstelleUndDruckeEtikettenAsync(
                        neueEinheiten, _artikel);

                    if (druckErfolgreich)
                    {
                        ShowStatus("✓ PDF-Etiketten erstellt und gedruckt!", Brushes.Green);

                        MessageBox.Show($"Wareneingang erfolgreich abgeschlossen!\n\n" +
                                       $"Artikel: {_artikel.Bezeichnung}\n" +
                                       $"Eingelagerte Stück: {stueckzahl}\n" +
                                       $"Barcodes generiert: {neueEinheiten.Count}\n" +
                                       $"PDF-Etiketten: ✓ Erstellt\n\n" +
                                       $"Etikett-PDFs gespeichert in:\n{BarcodeEtikettService.GetEtikettenVerzeichnis()}\n\n" +
                                       $"Falls der automatische Druck nicht funktioniert hat,\n" +
                                       $"öffnen Sie das Verzeichnis und drucken Sie die PDFs manuell.",
                                       "Wareneingang abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ShowStatus("⚠ Einlagerung OK, aber Etikett-Fehler", Brushes.Orange);

                        MessageBox.Show($"Wareneingang erfolgreich, aber Probleme beim Erstellen der Etiketten.\n\n" +
                                       $"Die Artikel-Einheiten wurden korrekt eingelagert.\n" +
                                       $"Bitte prüfen Sie die PDF-Bibliotheken.",
                                       "Teilweise erfolgreich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception etikettenEx)
                {
                    ShowStatus("⚠ Einlagerung OK, aber Etikett-Fehler", Brushes.Orange);

                    // Prüfen ob Etiketten erstellt wurden, auch wenn Druck fehlschlug
                    string etikettVerzeichnis = BarcodeEtikettService.GetEtikettenVerzeichnis();
                    bool etikettenErstellt = Directory.GetFiles(etikettVerzeichnis, "*.pdf")
                        .Any(f => Path.GetFileName(f).StartsWith($"{_artikel.Id}_"));

                    if (etikettenErstellt)
                    {
                        var result = MessageBox.Show($"Wareneingang erfolgreich!\n\n" +
                                       $"Die Etiketten wurden erstellt, aber der automatische Druck war nicht möglich:\n" +
                                       $"{etikettenEx.Message}\n\n" +
                                       $"Lösung: Öffnen Sie das Etikett-Verzeichnis und drucken Sie die PDFs manuell:\n" +
                                       $"{etikettVerzeichnis}\n\n" +
                                       $"Soll das Verzeichnis jetzt geöffnet werden?",
                                       "Manueller Druck erforderlich", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            EtikettTestService.OeffneEtikettVerzeichnis();
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Wareneingang erfolgreich, aber Fehler beim Erstellen der Etiketten:\n\n" +
                                       $"{etikettenEx.Message}\n\n" +
                                       $"Die Artikel-Einheiten wurden korrekt eingelagert.",
                                       "Teilweise erfolgreich", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
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