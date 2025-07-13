using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace LAGA
{
    /// <summary>
    /// Service für die Generierung und den Druck von Etiketten
    /// Verwendet den vom Nutzer ausgewählten Drucker (flexibel für Zebra, TSC und andere ZPL-kompatible Drucker)
    /// Unterstützt sowohl direkte ZPL-Übertragung als auch Standard-Windows-Druck
    /// </summary>
    public static class ZebraEtikettService
    {
        /// <summary>
        /// Verzeichnis für die ZPL-Dateien (als Backup/Debug)
        /// </summary>
        private static readonly string EtikettenVerzeichnis = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ZPL_Etiketten");

        /// <summary>
        /// Standard-Port für Netzwerkdruck (falls verwendet)
        /// </summary>
        private static int _netzwerkPort = 9100;

        /// <summary>
        /// Enum für verfügbare Druck-Methoden
        /// </summary>
        public enum DruckMethode
        {
            WindowsDrucker,      // Standard Windows-Drucker (ZPL als RAW-Daten)
            Netzwerk,           // Direkter Netzwerkdruck über TCP
            StandardGrafik      // Fallback: Grafik-Druck für nicht-ZPL-Drucker
        }

        /// <summary>
        /// Druckt bestehende Barcodes erneut (für Neudruck-Funktionalität)
        /// Verwendet den vom Nutzer konfigurierten Drucker
        /// </summary>
        /// <param name="ausgewaehlteEinheiten">Liste der ArtikelEinheiten die neu gedruckt werden sollen</param>
        /// <param name="artikel">Der zugehörige Artikel mit Bezeichnung</param>
        /// <returns>True wenn erfolgreich gedruckt, false bei Fehlern</returns>
        public static async Task<bool> DruckeBestehendeBarcodes(
            List<ArtikelEinheit> ausgewaehlteEinheiten, Artikel artikel)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Neudruck von {ausgewaehlteEinheiten.Count} bestehenden Barcode(s)");

                // Für Neudruck die gleiche Logik wie für neuen Druck verwenden
                // Das stellt sicher dass der konfigurierte Drucker verwendet wird
                return await ErstelleUndDruckeEtikettenAsync(ausgewaehlteEinheiten, artikel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Neudruck der Barcodes: {ex.Message}");
                throw new Exception($"Fehler beim Neudruck der Barcodes: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Statischer Konstruktor - erstellt ZPL-Verzeichnis
        /// </summary>
        static ZebraEtikettService()
        {
            Directory.CreateDirectory(EtikettenVerzeichnis);
        }

        /// <summary>
        /// Erstellt und druckt Etiketten für eine Liste von ArtikelEinheiten
        /// Verwendet den vom Nutzer in den Einstellungen konfigurierten Drucker
        /// </summary>
        /// <param name="artikelEinheiten">Liste der ArtikelEinheiten für die Etiketten erstellt werden sollen</param>
        /// <param name="artikel">Der zugehörige Artikel mit Bezeichnung</param>
        /// <returns>True wenn erfolgreich gedruckt, false bei Fehlern</returns>
        public static async Task<bool> ErstelleUndDruckeEtikettenAsync(
            List<ArtikelEinheit> artikelEinheiten, Artikel artikel)
        {
            try
            {
                // Zuerst prüfen ob ein Drucker konfiguriert ist
                var druckerEinstellungen = await DruckerEinstellungsService.EinstellungenLadenAsync();

                if (druckerEinstellungen == null || string.IsNullOrEmpty(druckerEinstellungen.AusgewaehlterDrucker))
                {
                    MessageBox.Show(
                        "Es wurde noch kein Drucker für den Etikettendruck konfiguriert.\n\n" +
                        "Bitte gehen Sie zu 'Einstellungen → Drucker einrichten' und wählen Sie einen Drucker aus.",
                        "Kein Drucker konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                // Prüfen ob der konfigurierte Drucker noch verfügbar ist
                if (!DruckerEinstellungsService.IstDruckerVerfuegbar(druckerEinstellungen.AusgewaehlterDrucker))
                {
                    MessageBox.Show(
                        $"Der konfigurierte Drucker '{druckerEinstellungen.AusgewaehlterDrucker}' ist nicht mehr verfügbar.\n\n" +
                        "Bitte gehen Sie zu 'Einstellungen → Drucker einrichten' und wählen Sie einen anderen Drucker aus.",
                        "Drucker nicht verfügbar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"🖨️ Verwende konfigurierten Drucker: {druckerEinstellungen.AusgewaehlterDrucker}");

                // ZPL-Etiketten für alle ArtikelEinheiten erstellen
                var erfolgreicheEtiketten = new List<string>();

                foreach (var einheit in artikelEinheiten)
                {
                    string zplCode = ErstelleZPLEtikett(einheit, artikel);

                    if (!string.IsNullOrEmpty(zplCode))
                    {
                        // ZPL-Code als Datei speichern (für Debug/Backup)
                        await SpeichereZPLDateiAsync(einheit, zplCode);
                        erfolgreicheEtiketten.Add(zplCode);
                    }
                }

                if (erfolgreicheEtiketten.Count == 0)
                {
                    MessageBox.Show("Keine Etiketten konnten erstellt werden.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Etiketten an den konfigurierten Drucker senden
                return await DruckeEtikettenAsync(erfolgreicheEtiketten, druckerEinstellungen.AusgewaehlterDrucker);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Erstellen und Drucken der Etiketten:\n\n{ex.Message}",
                    "Druckfehler", MessageBoxButton.OK, MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"❌ Druck-Fehler: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Druckt die ZPL-Etiketten über den angegebenen Drucker
        /// Versucht verschiedene Druck-Methoden automatisch
        /// </summary>
        /// <param name="zplEtiketten">Liste der ZPL-Codes zum Drucken</param>
        /// <param name="druckerName">Name des zu verwendenden Druckers</param>
        /// <returns>True wenn erfolgreich gedruckt</returns>
        private static async Task<bool> DruckeEtikettenAsync(List<string> zplEtiketten, string druckerName)
        {
            try
            {
                // Alle ZPL-Codes zu einem Druckauftrag kombinieren
                string kombiniertesZPL = string.Join("\n", zplEtiketten);

                // Über Windows-Drucker drucken (Standard-Methode)
                bool erfolg = await DruckeUeberWindowsDruckerAsync(kombiniertesZPL, druckerName);

                if (erfolg)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ {zplEtiketten.Count} Etikett(en) erfolgreich gedruckt");
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        $"Fehler beim Drucken über Drucker '{druckerName}'.\n\n" +
                        "Mögliche Ursachen:\n" +
                        "• Drucker ist offline oder nicht bereit\n" +
                        "• Drucker unterstützt keine ZPL-Befehle\n" +
                        "• Verbindungsfehler zum Drucker",
                        "Druckfehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Drucken: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Druckt ZPL-Code über den Windows-Drucker (für alle ZPL-kompatiblen Drucker)
        /// Funktioniert mit Zebra, TSC und anderen ZPL-kompatiblen Druckern
        /// </summary>
        /// <param name="zplCode">Der zu druckende ZPL-Code</param>
        /// <param name="druckerName">Name des Druckers</param>
        /// <returns>True wenn erfolgreich gedruckt</returns>
        private static async Task<bool> DruckeUeberWindowsDruckerAsync(string zplCode, string druckerName)
        {
            try
            {
                return await Task.Run(() =>
                {
                    IntPtr hPrinter = IntPtr.Zero;

                    try
                    {
                        // Drucker öffnen
                        if (!OpenPrinter(druckerName, out hPrinter, IntPtr.Zero))
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Konnte Drucker '{druckerName}' nicht öffnen");
                            return false;
                        }

                        // Druckauftrag starten
                        DOC_INFO_1 docInfo = new DOC_INFO_1
                        {
                            pDocName = "LAGA Etikett",
                            pOutputFile = null,
                            pDataType = "RAW"  // Wichtig: RAW-Modus für ZPL-Befehle
                        };

                        if (StartDocPrinter(hPrinter, 1, ref docInfo) == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("❌ Konnte Druckauftrag nicht starten");
                            return false;
                        }

                        if (!StartPagePrinter(hPrinter))
                        {
                            System.Diagnostics.Debug.WriteLine("❌ Konnte Druckseite nicht starten");
                            EndDocPrinter(hPrinter);
                            return false;
                        }

                        // ZPL-Daten als UTF-8 Bytes konvertieren und senden
                        byte[] zplBytes = Encoding.UTF8.GetBytes(zplCode);
                        uint bytesWritten = 0;

                        bool success = WritePrinter(hPrinter, zplBytes, (uint)zplBytes.Length, out bytesWritten);

                        // Druckauftrag ordnungsgemäß beenden
                        EndPagePrinter(hPrinter);
                        EndDocPrinter(hPrinter);

                        if (success && bytesWritten == zplBytes.Length)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ ZPL erfolgreich an Drucker '{druckerName}' gesendet ({bytesWritten} Bytes)");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ ZPL-Übertragung fehlgeschlagen. Erwartet: {zplBytes.Length}, Geschrieben: {bytesWritten}");
                            return false;
                        }
                    }
                    finally
                    {
                        // Drucker-Handle schließen
                        if (hPrinter != IntPtr.Zero)
                        {
                            ClosePrinter(hPrinter);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Windows-Druck: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Erstellt den ZPL-Code für ein einzelnes Etikett
        /// ORIGINAL FUNKTIONIERENDER CODE - exakt wie im GitHub Repository
        /// </summary>
        /// <param name="einheit">Die ArtikelEinheit für die das Etikett erstellt wird</param>
        /// <param name="artikel">Der zugehörige Artikel</param>
        /// <returns>Fertiger ZPL-Code für das Etikett</returns>
        private static string ErstelleZPLEtikett(ArtikelEinheit einheit, Artikel artikel)
        {
            try
            {
                // Artikelbezeichnung kürzen (max. 40 Zeichen für bessere Lesbarkeit)
                
                string kurzeBezeichnung = artikel.Bezeichnung.Length > 40
                    ? artikel.Bezeichnung.Substring(0, 15) + "..."
                    : artikel.Bezeichnung;

                var zpl = new StringBuilder();

                // ZPL-Einstellungen 
                zpl.AppendLine("^XA");                          // Etikett-Start

                // Etikettgröße: 57mm x 24mm → 456 x 192 dots (bei 203 DPI)
                zpl.AppendLine("^PW456");                       
                zpl.AppendLine("^LL192");                       

                // Druckgeschwindigkeit & Dunkelheit
                zpl.AppendLine("^PR4");                         // Druckgeschwindigkeit
                zpl.AppendLine("^MD15");                        // Druckdunkelheit

                // Zeichensatz UTF-8 (für Sonderzeichen)
                zpl.AppendLine("^CI28");

                // Artikelbezeichnung oben, zentriert mit FB-Parameter
                zpl.AppendLine("^FO0,35^FB456,1,0,C^A0N,20,20^FH^FD" + kurzeBezeichnung + "^FS");

                // Barcode mit BY-Einstellungen und genauer Positionierung
                zpl.AppendLine("^BY2,2,80");                    // Barcode-Parameter
                zpl.AppendLine("^FO85,60^BCN,80,Y,N,N^FD" + einheit.Barcode + "^FS");

                zpl.AppendLine("^XZ");                          // Etikett-Ende

                return zpl.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Erstellen des ZPL-Etiketts: {ex.Message}");
                throw new Exception($"Fehler beim Erstellen des ZPL-Codes für Barcode {einheit.Barcode}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Speichert den ZPL-Code als Datei (für Backup und Debugging)
        /// </summary>
        /// <param name="einheit">Die ArtikelEinheit</param>
        /// <param name="zplCode">Der ZPL-Code</param>
        private static async Task SpeichereZPLDateiAsync(ArtikelEinheit einheit, string zplCode)
        {
            try
            {
                string dateiName = $"Etikett_{einheit.Barcode}_{DateTime.Now:yyyyMMdd_HHmmss}.zpl";
                string dateiPfad = Path.Combine(EtikettenVerzeichnis, dateiName);

                await File.WriteAllTextAsync(dateiPfad, zplCode);

                System.Diagnostics.Debug.WriteLine($"💾 ZPL-Datei gespeichert: {dateiName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Speichern der ZPL-Datei: {ex.Message}");
                // Fehler beim Speichern ist nicht kritisch für den Druckvorgang
            }
        }

        /// <summary>
        /// Erstellt ein Test-Etikett für Drucker-Tests
        /// </summary>
        /// <returns>True wenn Test erfolgreich</returns>
        public static async Task<bool> DruckeTestEtikettAsync()
        {
            try
            {
                var testEinheit = new ArtikelEinheit
                {
                    ArtikelId = 999,
                    Barcode = "TEST123456",
                    ErstellungsDatum = DateTime.Now
                };

                var testArtikel = new Artikel
                {
                    Bezeichnung = "TEST-ARTIKEL für Drucker-Konfiguration"
                };

                var testEinheiten = new List<ArtikelEinheit> { testEinheit };

                return await ErstelleUndDruckeEtikettenAsync(testEinheiten, testArtikel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Test-Etikett:\n\n{ex.Message}",
                    "Test fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Gibt Informationen über die aktuelle Drucker-Konfiguration zurück
        /// </summary>
        /// <returns>String mit Drucker-Informationen</returns>
        public static async Task<string> GetDruckerInfoAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("LAGA Etikett-Drucker Informationen:");
            sb.AppendLine();

            try
            {
                var einstellungen = await DruckerEinstellungsService.EinstellungenLadenAsync();

                if (einstellungen != null && !string.IsNullOrEmpty(einstellungen.AusgewaehlterDrucker))
                {
                    sb.AppendLine($"Konfigurierter Drucker: {einstellungen.AusgewaehlterDrucker}");
                    sb.AppendLine($"Letzte Änderung: {einstellungen.LetzteAenderung:dd.MM.yyyy HH:mm}");

                    bool verfuegbar = DruckerEinstellungsService.IstDruckerVerfuegbar(einstellungen.AusgewaehlterDrucker);
                    sb.AppendLine($"Status: {(verfuegbar ? "✅ Verfügbar" : "❌ Nicht verfügbar")}");
                }
                else
                {
                    sb.AppendLine("❌ Kein Drucker konfiguriert");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"❌ Fehler beim Laden der Einstellungen: {ex.Message}");
            }

            sb.AppendLine();
            sb.AppendLine("Format: ZPL (57×24mm)");
            sb.AppendLine("Unterstützte Drucker: Zebra, TSC, und andere ZPL-kompatible Drucker");
            sb.AppendLine();
            sb.AppendLine("Alle verfügbaren Drucker im System:");

            var verfuegbareDrucker = DruckerEinstellungsService.VerfuegbareDruckerHolen();
            foreach (string drucker in verfuegbareDrucker)
            {
                sb.AppendLine($"- {drucker}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Löscht alte ZPL-Dateien (älter als 30 Tage)
        /// </summary>
        public static void BereinigeAlteEtiketten()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-30);
                var dateien = Directory.GetFiles(EtikettenVerzeichnis, "*.zpl");

                int geloeschteAnzahl = 0;
                foreach (string datei in dateien)
                {
                    var fileInfo = new FileInfo(datei);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(datei);
                        geloeschteAnzahl++;
                    }
                }

                if (geloeschteAnzahl > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🗑️ {geloeschteAnzahl} alte ZPL-Dateien bereinigt");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Bereinigen alter Etiketten: {ex.Message}");
            }
        }

        #region P/Invoke für Windows-Drucker-API

        /// <summary>
        /// Struktur für Dokument-Informationen beim Drucken
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DOC_INFO_1
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode)]
        public static extern bool OpenPrinter(string printerName, out IntPtr hPrinter, IntPtr pDefault);

        [DllImport("winspool.drv")]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode)]
        public static extern uint StartDocPrinter(IntPtr hPrinter, uint level, ref DOC_INFO_1 pDocInfo);

        [DllImport("winspool.drv")]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv")]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv")]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv")]
        public static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, uint dwCount, out uint dwWritten);

        #endregion
    }
}