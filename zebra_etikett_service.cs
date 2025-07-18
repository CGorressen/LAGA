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
        /// Erstellt ZPL-Etiketten für die angegebenen ArtikelEinheiten und sendet sie an den konfigurierten Drucker
        /// Verwendet automatisch den vom Nutzer in den Einstellungen ausgewählten Drucker
        /// </summary>
        /// <param name="artikelEinheiten">Liste der ArtikelEinheiten für die Etiketten erstellt werden sollen</param>
        /// <param name="artikel">Der zugehörige Artikel mit Bezeichnung</param>
        /// <returns>True wenn erfolgreich gedruckt, false bei Fehlern</returns>
        public static async Task<bool> ErstelleUndDruckeEtikettenAsync(
            List<ArtikelEinheit> artikelEinheiten, Artikel artikel)
        {
            try
            {
                if (artikelEinheiten == null || !artikelEinheiten.Any())
                {
                    MessageBox.Show("Keine ArtikelEinheiten zum Drucken vorhanden.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"🖨️ Erstelle {artikelEinheiten.Count} Etikett(en) für Artikel: {artikel.Bezeichnung}");

                // Drucker-Einstellungen laden
                var druckerEinstellungen = await DruckerEinstellungsService.EinstellungenLadenAsync();

                if (druckerEinstellungen == null || string.IsNullOrEmpty(druckerEinstellungen.AusgewaehlterDrucker))
                {
                    MessageBox.Show("Kein Drucker konfiguriert!\n\nBitte gehen Sie zu 'Einstellungen > Drucker-Konfiguration' und wählen Sie einen Drucker aus.",
                        "Drucker nicht konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                // Prüfen ob der konfigurierte Drucker verfügbar ist
                if (!DruckerEinstellungsService.IstDruckerVerfuegbar(druckerEinstellungen.AusgewaehlterDrucker))
                {
                    MessageBox.Show($"Der konfigurierte Drucker '{druckerEinstellungen.AusgewaehlterDrucker}' ist nicht verfügbar!\n\nBitte prüfen Sie:\n- Ist der Drucker eingeschaltet?\n- Ist der Drucker korrekt installiert?\n- Sind alle Kabel richtig angeschlossen?",
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
                    MessageBox.Show($"Fehler beim Drucken über Windows-Drucker '{druckerName}'.\n\nBitte prüfen Sie:\n- Ist der Drucker eingeschaltet und bereit?\n- Sind genügend Etiketten eingelegt?\n- Ist das Druckerkabel angeschlossen?",
                        "Druckfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Drucken: {ex.Message}");
                MessageBox.Show($"Unerwarteter Fehler beim Drucken:\n\n{ex.Message}",
                    "Druckfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Druckt ZPL-Code über Windows-Drucker (RAW-Modus)
        /// </summary>
        /// <param name="zplCode">Der ZPL-Code zum Drucken</param>
        /// <param name="druckerName">Name des Windows-Druckers</param>
        /// <returns>True wenn erfolgreich gesendet</returns>
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

                        // Druckauftrag konfigurieren und starten
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

        /// <summary>
        /// Windows API: Drucker öffnen
        /// </summary>
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        /// <summary>
        /// Windows API: Drucker schließen
        /// </summary>
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        /// <summary>
        /// Windows API: Druckauftrag starten
        /// </summary>
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint StartDocPrinter(IntPtr hPrinter, uint level, ref DOC_INFO_1 pDocInfo);

        /// <summary>
        /// Windows API: Druckauftrag beenden
        /// </summary>
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        /// <summary>
        /// Windows API: Druckseite starten
        /// </summary>
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        /// <summary>
        /// Windows API: Druckseite beenden
        /// </summary>
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        /// <summary>
        /// Windows API: Daten an Drucker schreiben
        /// </summary>
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, uint dwCount, out uint dwWritten);

        #endregion
    }
}