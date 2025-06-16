using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LAGA
{
    /// <summary>
    /// Service zum Testen der Zebra-Etikett-Funktionalität
    /// Hilfreich für Debugging und Setup-Tests mit dem GX420t
    /// </summary>
    public static class ZebraTestService
    {
        /// <summary>
        /// Erstellt ein Test-Etikett zum Überprüfen der ZPL-Generierung und des Drucks
        /// </summary>
        public static async Task<bool> ErstelleTestEtikettAsync()
        {
            try
            {
                // Test-Artikel erstellen
                var testArtikel = new Artikel
                {
                    Id = 999,
                    Bezeichnung = "TEST-ARTIKEL-ZEBRA"
                };

                // Test-ArtikelEinheit erstellen
                var testEinheit = new ArtikelEinheit
                {
                    Id = 999,
                    ArtikelId = 999,
                    Barcode = "1234567890"
                };

                var testEinheiten = new List<ArtikelEinheit> { testEinheit };

                // Test-Etikett erstellen und drucken
                bool erfolg = await ZebraEtikettService.ErstelleUndDruckeEtikettenAsync(
                    testEinheiten, testArtikel);

                if (erfolg)
                {
                    string verzeichnis = ZebraEtikettService.GetEtikettenVerzeichnis();
                    MessageBox.Show($"Test-Etikett erfolgreich erstellt und gedruckt!\n\n" +
                                   $"ZPL-Verzeichnis: {verzeichnis}\n" +
                                   $"Datei: 999_1234567890.zpl\n\n" +
                                   $"Prüfen Sie den Zebra GX420t Drucker.",
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
        /// Öffnet das ZPL-Verzeichnis im Windows Explorer
        /// </summary>
        public static void OeffneZPLVerzeichnis()
        {
            try
            {
                string verzeichnis = ZebraEtikettService.GetEtikettenVerzeichnis();
                System.Diagnostics.Process.Start("explorer.exe", verzeichnis);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Verzeichnisses:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt Informationen über das Zebra-Etikett-System
        /// </summary>
        public static void ZeigeSystemInfo()
        {
            try
            {
                string verzeichnis = ZebraEtikettService.GetEtikettenVerzeichnis();
                var verzeichnisInfo = new DirectoryInfo(verzeichnis);

                int anzahlZPL = verzeichnisInfo.Exists
                    ? verzeichnisInfo.GetFiles("*.zpl").Length
                    : 0;

                MessageBox.Show($"LAGA Zebra-Etikett-System Informationen:\n\n" +
                               $"Drucker-Modell: Zebra GX420t\n" +
                               $"Etikett-Format: 57×24mm\n" +
                               $"Sprache: ZPL (Zebra Programming Language)\n" +
                               $"Barcode-Format: Code128\n" +
                               $"Druck-Methoden: USB/Seriell + Netzwerk\n\n" +
                               $"ZPL-Speicher-Verzeichnis:\n{verzeichnis}\n\n" +
                               $"Gespeicherte ZPL-Dateien: {anzahlZPL}\n\n" +
                               $"Drucker-Konfiguration:\n" +
                               $"- Überprüfen Sie die USB-Verbindung\n" +
                               $"- Drucker-Name: Prüfen Sie in Windows-Druckern\n" +
                               $"- Für Netzwerk: IP-Adresse konfigurieren",
                               "System-Informationen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abrufen der System-Informationen:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt eine Anleitung für die Zebra-Drucker-Konfiguration
        /// </summary>
        public static void ZeigeDruckerKonfiguration()
        {
            string anleitung = @"Zebra GX420t Drucker-Konfiguration:

1. HARDWARE-SETUP:
   • USB-Kabel zwischen PC und Drucker verbinden
   • Drucker einschalten
   • Etiketten-Rolle einlegen (57×24mm)

2. WINDOWS-TREIBER:
   • Zebra-Treiber von zebra.com herunterladen
   • Treiber installieren
   • Drucker sollte als 'ZDesigner GX420t' erscheinen

3. DRUCKER-EINSTELLUNGEN:
   • Windows → Einstellungen → Drucker
   • Zebra-Drucker auswählen → Eigenschaften
   • Etikett-Größe: 57×24mm einstellen
   • Druckqualität: Normal/Standard

4. NETZWERK-DRUCK (Optional):
   • Drucker über Ethernet verbinden
   • IP-Adresse im Code konfigurieren:
     ZebraEtikettService.SetZebraDruckerIP(""192.168.1.100"")

5. TEST:
   • Test-Etikett über LAGA drucken
   • ZPL-Dateien im Backup-Verzeichnis prüfen

FEHLERBEHEBUNG:
• Drucker-Name in Windows prüfen
• USB-Verbindung testen
• Zebra Setup Utilities verwenden
• Drucker-Status-LED überprüfen";

            MessageBox.Show(anleitung, "Drucker-Konfiguration Anleitung",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Konfiguriert den Zebra-Drucker über ein einfaches Dialog
        /// </summary>
        public static void KonfiguriereDrucker()
        {
            try
            {
                // Einfacher Dialog zur Drucker-Konfiguration
                var konfigDialog = new ZebraDruckerKonfigDialog();
                konfigDialog.Owner = Application.Current.MainWindow;
                konfigDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Drucker-Konfiguration:\n{ex.Message}",
                    "Konfigurationsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Einfacher Dialog zur Zebra-Drucker-Konfiguration
    /// </summary>
    public partial class ZebraDruckerKonfigDialog : Window
    {
        public ZebraDruckerKonfigDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Einfacher Dialog - kann später erweitert werden
            Title = "Zebra-Drucker Konfiguration";
            Width = 450;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.Margin = new Thickness(20);

            var textBlock = new TextBlock
            {
                Text = "Zebra-Drucker Konfiguration:\n\n" +
                       "1. USB-Verbindung prüfen\n" +
                       "2. Treiber installieren\n" +
                       "3. Test-Etikett drucken\n\n" +
                       "Weitere Optionen werden in zukünftigen Versionen hinzugefügt.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 100,
                Height = 35,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 20, 0, 0)
            };

            okButton.Click += (s, e) => Close();

            grid.Children.Add(textBlock);
            grid.Children.Add(okButton);
            Content = grid;
        }
    }
}