using System.IO;
using System.Windows;

namespace LAGA
{
    /// <summary>
    /// Service zum Testen der Etikett-Funktionalität
    /// Hilfreich für Debugging und Setup-Tests
    /// </summary>
    public static class EtikettTestService
    {
        /// <summary>
        /// Erstellt ein Test-Etikett zum Überprüfen der PDF-Generierung
        /// </summary>
        public static async Task<bool> ErstelleTestEtikettAsync()
        {
            try
            {
                // Test-Artikel erstellen
                var testArtikel = new Artikel
                {
                    Id = 999,
                    Bezeichnung = "TEST-ARTIKEL"
                };

                // Test-ArtikelEinheit erstellen
                var testEinheit = new ArtikelEinheit
                {
                    Id = 999,
                    ArtikelId = 999,
                    Barcode = "1234567890"
                };

                var testEinheiten = new List<ArtikelEinheit> { testEinheit };

                // Test-Etikett erstellen (ohne Druck)
                bool erfolg = await BarcodeEtikettService.ErstelleUndDruckeEtikettenAsync(
                    testEinheiten, testArtikel);

                if (erfolg)
                {
                    string verzeichnis = BarcodeEtikettService.GetEtikettenVerzeichnis();
                    MessageBox.Show($"Test-Etikett erfolgreich erstellt!\n\n" +
                                   $"Verzeichnis: {verzeichnis}\n" +
                                   $"Datei: 999_1234567890.pdf\n\n" +
                                   $"Bitte prüfen Sie das erstellte PDF.",
                                   "Test erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Verzeichnis im Explorer öffnen
                    System.Diagnostics.Process.Start("explorer.exe", verzeichnis);
                }

                return erfolg;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Test-Etikett:\n\n{ex.Message}",
                    "Test fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Öffnet das Etikett-Verzeichnis im Windows Explorer
        /// </summary>
        public static void OeffneEtikettVerzeichnis()
        {
            try
            {
                string verzeichnis = BarcodeEtikettService.GetEtikettenVerzeichnis();
                System.Diagnostics.Process.Start("explorer.exe", verzeichnis);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Verzeichnisses:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt Informationen über das Etikett-System
        /// </summary>
        public static void ZeigeSystemInfo()
        {
            try
            {
                string verzeichnis = BarcodeEtikettService.GetEtikettenVerzeichnis();
                var verzeichnisInfo = new DirectoryInfo(verzeichnis);
                
                int anzahlPDFs = verzeichnisInfo.Exists 
                    ? verzeichnisInfo.GetFiles("*.pdf").Length 
                    : 0;

                MessageBox.Show($"LAGA Etikett-System Informationen:\n\n" +
                               $"PDF-Bibliothek: QuestPDF\n" +
                               $"Barcode-Bibliothek: ZXing.Net\n" +
                               $"Etikett-Format: 40×20mm\n" +
                               $"Barcode-Format: Code128\n\n" +
                               $"Speicher-Verzeichnis:\n{verzeichnis}\n\n" +
                               $"Gespeicherte PDFs: {anzahlPDFs}",
                               "System-Informationen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abrufen der System-Informationen:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}