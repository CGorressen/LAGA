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
    /// Unterstützt Zebra ZPL-Drucker und Standard-Windows-Drucker automatisch
    /// Erweitert um automatische Drucker-Erkennung für Multi-Standort-Nutzung
    /// </summary>
    public static class ZebraEtikettService
    {
        /// <summary>
        /// Verzeichnis für die ZPL-Dateien (als Backup/Debug)
        /// </summary>
        private static readonly string EtikettenVerzeichnis = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ZPL_Etiketten");

        /// <summary>
        /// Standard-Druckername für Zebra GX420t (wird automatisch gesucht)
        /// </summary>
        private static string _zebraDruckerName = "ZDesigner GX420t";

        /// <summary>
        /// IP-Adresse des Zebra-Druckers (falls Netzwerkdruck verwendet wird)
        /// </summary>
        private static string _zebraDruckerIP = "192.168.1.100"; // Beispiel-IP, muss angepasst werden

        /// <summary>
        /// Port für Netzwerkdruck (Standard für Zebra-Drucker)
        /// </summary>
        private static int _zebraDruckerPort = 9100;

        /// <summary>
        /// Enum für verfügbare Drucker-Typen
        /// </summary>
        public enum DruckerTyp
        {
            Zebra,
            StandardWindows
        }

        /// <summary>
        /// Statischer Konstruktor - erstellt ZPL-Verzeichnis
        /// </summary>
        static ZebraEtikettService()
        {
            // ZPL-Verzeichnis erstellen falls es nicht existiert
            Directory.CreateDirectory(EtikettenVerzeichnis);
        }

        /// <summary>
        /// Erstellt und druckt Etiketten für eine Liste von ArtikelEinheiten (Wareneingang)
        /// Automatische Drucker-Erkennung: Zebra bevorzugt, Standard-Drucker als Fallback
        /// </summary>
        /// <param name="artikelEinheiten">Liste der ArtikelEinheiten für die Etiketten erstellt werden sollen</param>
        /// <param name="artikel">Der zugehörige Artikel mit Bezeichnung</param>
        public static async Task<bool> ErstelleUndDruckeEtikettenAsync(
            List<ArtikelEinheit> artikelEinheiten, Artikel artikel)
        {
            try
            {
                // Verfügbaren Drucker-Typ ermitteln
                DruckerTyp verfuegbarerDrucker = ErmittleVerfuegbarenDrucker();

                System.Diagnostics.Debug.WriteLine($"🖨️ Erkannter Drucker-Typ: {verfuegbarerDrucker}");

                bool druckErfolgreich = false;

                // Je nach Drucker-Typ entsprechend drucken
                switch (verfuegbarerDrucker)
                {
                    case DruckerTyp.Zebra:
                        druckErfolgreich = await DruckeZebraEtikettenAsync(artikelEinheiten, artikel);
                        break;

                    case DruckerTyp.StandardWindows:
                        druckErfolgreich = await DruckeStandardEtikettenAsync(artikelEinheiten, artikel);
                        break;
                }

                return druckErfolgreich;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Etiketten: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Druckt bestehende Barcodes erneut (für Neudruck-Funktionalität)
        /// Automatische Drucker-Erkennung für Neudruck
        /// </summary>
        /// <param name="ausgewaehlteEinheiten">Liste der ArtikelEinheiten die neu gedruckt werden sollen</param>
        /// <param name="artikel">Der zugehörige Artikel mit Bezeichnung</param>
        public static async Task<bool> DruckeBestehendeBarcodes(
            List<ArtikelEinheit> ausgewaehlteEinheiten, Artikel artikel)
        {
            try
            {
                // Für Neudruck gleiche Logik wie für neuen Druck verwenden
                return await ErstelleUndDruckeEtikettenAsync(ausgewaehlteEinheiten, artikel);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Neudruck der Barcodes: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Ermittelt automatisch welcher Drucker-Typ verfügbar ist
        /// Priorisierung: 1. Zebra-Drucker, 2. Standard-Windows-Drucker
        /// </summary>
        /// <returns>Verfügbarer Drucker-Typ</returns>
        private static DruckerTyp ErmittleVerfuegbarenDrucker()
        {
            try
            {
                // Alle installierten Drucker durchsuchen
                var druckerListe = PrinterSettings.InstalledPrinters;

                foreach (string drucker in druckerListe)
                {
                    string druckerLower = drucker.ToLower();

                    // Zebra-Drucker suchen (verschiedene Modelle)
                    if (druckerLower.Contains("zebra") ||
                        druckerLower.Contains("gx420") ||
                        druckerLower.Contains("zdesigner") ||
                        druckerLower.Contains("zpl"))
                    {
                        _zebraDruckerName = drucker; // Exakten Namen speichern
                        System.Diagnostics.Debug.WriteLine($"✅ Zebra-Drucker gefunden: {drucker}");
                        return DruckerTyp.Zebra;
                    }
                }

                // Kein Zebra gefunden → Standard-Drucker verwenden
                System.Diagnostics.Debug.WriteLine($"ℹ️ Kein Zebra-Drucker gefunden, verwende Standard-Drucker");
                return DruckerTyp.StandardWindows;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler bei Drucker-Erkennung: {ex.Message}");
                return DruckerTyp.StandardWindows; // Fallback auf Standard
            }
        }

        /// <summary>
        /// Druckt Etiketten über Zebra-Drucker (ZPL-Format)
        /// Bisherige Zebra-Funktionalität unverändert
        /// </summary>
        private static async Task<bool> DruckeZebraEtikettenAsync(
            List<ArtikelEinheit> artikelEinheiten, Artikel artikel)
        {
            try
            {
                var erfolgreicheEtiketten = new List<string>();

                foreach (var einheit in artikelEinheiten)
                {
                    // ZPL-Etikett für jede Einheit erstellen
                    string zplCode = ErstelleZPLEtikett(einheit, artikel);

                    if (!string.IsNullOrEmpty(zplCode))
                    {
                        // ZPL-Code als Datei speichern (für Debug/Backup)
                        await SpeichereZPLDateiAsync(einheit, zplCode);
                        erfolgreicheEtiketten.Add(zplCode);
                    }
                }

                // Alle erstellten Etiketten drucken
                if (erfolgreicheEtiketten.Count > 0)
                {
                    await DruckeZPLEtikettenAsync(erfolgreicheEtiketten);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Zebra-Druck Fehler: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Druckt Etiketten über Standard-Windows-Drucker (Grafik-Format)
        /// NEU: Für Nicht-Zebra-Drucker (Brother, Canon, HP, etc.)
        /// </summary>
        private static async Task<bool> DruckeStandardEtikettenAsync(
            List<ArtikelEinheit> artikelEinheiten, Artikel artikel)
        {
            try
            {
                return await Task.Run(() =>
                {
                    bool alleErfolgreich = true;

                    foreach (var einheit in artikelEinheiten)
                    {
                        try
                        {
                            // Etikett als Grafik erstellen und drucken
                            bool einzelErfolg = DruckeStandardEtikett(einheit, artikel);
                            if (!einzelErfolg)
                            {
                                alleErfolgreich = false;
                            }

                            // Kurze Pause zwischen Etiketten
                            System.Threading.Thread.Sleep(200);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Standard-Druck Fehler für {einheit.Barcode}: {ex.Message}");
                            alleErfolgreich = false;
                        }
                    }

                    return alleErfolgreich;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Standard-Druck Fehler: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Druckt ein einzelnes Etikett über Standard-Windows-Drucker
        /// Erstellt eine Grafik und sendet sie an den Drucker
        /// </summary>
        private static bool DruckeStandardEtikett(ArtikelEinheit einheit, Artikel artikel)
        {
            try
            {
                // PrintDocument für Standard-Druck erstellen
                using (var printDocument = new PrintDocument())
                {
                    // Standard-Drucker verwenden
                    // printDocument.PrinterSettings.PrinterName bleibt leer = Standard-Drucker

                    // Etikett-Größe konfigurieren (57x24mm)
                    printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                    // Paper size für Etikett (57x24mm in Hundertstel Zoll)
                    var paperSize = new PaperSize("Etikett 57x24mm", 224, 94); // 57mm = 224/100 inch, 24mm = 94/100 inch
                    printDocument.DefaultPageSettings.PaperSize = paperSize;

                    // Print-Event Handler
                    printDocument.PrintPage += (sender, e) =>
                    {
                        // Null-Check für Graphics-Objekt
                        if (e.Graphics != null)
                        {
                            DruckeEtikettGrafik(e.Graphics, einheit, artikel);
                        }
                    };

                    // Drucken
                    printDocument.Print();

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Standard-Etikett-Druck: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Zeichnet das Etikett als Grafik für Standard-Drucker
        /// Layout: Artikelbezeichnung oben, Barcode unten
        /// </summary>
        private static void DruckeEtikettGrafik(Graphics graphics, ArtikelEinheit einheit, Artikel artikel)
        {
            try
            {
                // Etikett-Abmessungen (57x24mm in Pixel bei 203 DPI)
                int etikettBreite = 456; // 57mm * 8 Pixel/mm
                int etikettHoehe = 192;  // 24mm * 8 Pixel/mm

                // Hintergrund weiß
                graphics.Clear(Color.White);

                // Artikelbezeichnung kürzen für bessere Darstellung
                string kurzeBezeichnung = artikel.Bezeichnung.Length > 30
                    ? artikel.Bezeichnung.Substring(0, 27) + "..."
                    : artikel.Bezeichnung;

                // Font für Bezeichnung - explizit System.Drawing.FontStyle verwenden
                using (var fontBezeichnung = new Font(new FontFamily("Arial"), 8, System.Drawing.FontStyle.Bold))
                {
                    using (var textBrush = new SolidBrush(Color.Black))
                    {
                        var textFormat = new StringFormat()
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };

                        // Bezeichnung oben zentriert
                        var textBereich = new RectangleF(0, 5, etikettBreite, 40);
                        graphics.DrawString(kurzeBezeichnung, fontBezeichnung, textBrush, textBereich, textFormat);
                    }
                }

                // Barcode als Text (für einfache Implementierung)
                // In einer erweiterten Version könnte hier ein echter Barcode gezeichnet werden
                using (var fontBarcode = new Font(new FontFamily("Consolas"), 12, System.Drawing.FontStyle.Bold))
                {
                    using (var barcodeBrush = new SolidBrush(Color.Black))
                    {
                        var barcodeFormat = new StringFormat()
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };

                        // Barcode-Bereich
                        var barcodeBereich = new RectangleF(0, 50, etikettBreite, 80);

                        // Barcode-Striche simulieren (vereinfacht)
                        using (var strichFont = new Font(new FontFamily("Arial"), 6))
                        {
                            var strichText = "||||| " + einheit.Barcode + " |||||";
                            graphics.DrawString(strichText, strichFont, barcodeBrush, barcodeBereich, barcodeFormat);
                        }

                        // Barcode-Nummer unter den Strichen
                        var nummernBereich = new RectangleF(0, 130, etikettBreite, 30);
                        graphics.DrawString(einheit.Barcode, fontBarcode, barcodeBrush, nummernBereich, barcodeFormat);
                    }
                }

                // Rahmen um das Etikett (optional, für bessere Sichtbarkeit)
                using (var rahmenPen = new Pen(Color.Black, 1))
                {
                    graphics.DrawRectangle(rahmenPen, 0, 0, etikettBreite - 1, etikettHoehe - 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Zeichnen der Etikett-Grafik: {ex.Message}");
            }
        }

        /// <summary>
        /// Erstellt den ZPL-Code für ein einzelnes Etikett (für Zebra-Drucker)
        /// Unverändert von der ursprünglichen Implementierung
        /// </summary>
        private static string ErstelleZPLEtikett(ArtikelEinheit einheit, Artikel artikel)
        {
            try
            {
                // Artikelbezeichnung kürzen (max. 40 Zeichen für bessere Lesbarkeit)
                string kurzeBezeichnung = artikel.Bezeichnung.Length > 40
                    ? artikel.Bezeichnung.Substring(0, 15) + "..."
                    : artikel.Bezeichnung;

                var zpl = new StringBuilder();

                // Etikett-Start
                zpl.AppendLine("^XA");

                // Etikettgröße: 57mm x 24mm → 456 x 192 dots (bei 203 DPI)
                zpl.AppendLine("^PW456"); // Breite
                zpl.AppendLine("^LL192"); // Länge

                // Druckgeschwindigkeit & Dunkelheit
                zpl.AppendLine("^PR4");
                zpl.AppendLine("^MD15");

                // Zeichensatz UTF-8 (optional, für Sonderzeichen)
                zpl.AppendLine("^CI28");

                // Artikelbezeichnung oben, zentriert
                zpl.AppendLine("^FO0,35^FB456,1,0,C^A0N,20,20^FH^FD" + kurzeBezeichnung + "^FS");

                // Barcode manuell zentriert
                zpl.AppendLine("^BY2,2,80");
                zpl.AppendLine("^FO85,60^BCN,80,Y,N,N^FD" + einheit.Barcode + "^FS");

                // Etikett-Ende
                zpl.AppendLine("^XZ");

                return zpl.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des ZPL-Codes für Barcode {einheit.Barcode}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Speichert den ZPL-Code als Datei (für Debug/Backup)
        /// </summary>
        private static async Task SpeichereZPLDateiAsync(ArtikelEinheit einheit, string zplCode, string prefix = "")
        {
            try
            {
                string dateiname = string.IsNullOrEmpty(prefix)
                    ? $"{einheit.ArtikelId}_{einheit.Barcode}.zpl"
                    : $"{prefix}_{einheit.ArtikelId}_{einheit.Barcode}_{DateTime.Now:yyyyMMdd_HHmmss}.zpl";

                string vollstaendigerPfad = Path.Combine(EtikettenVerzeichnis, dateiname);

                await File.WriteAllTextAsync(vollstaendigerPfad, zplCode, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Fehler beim Speichern nicht als kritisch behandeln
                System.Diagnostics.Debug.WriteLine($"Warnung: ZPL-Datei konnte nicht gespeichert werden: {ex.Message}");
            }
        }

        /// <summary>
        /// Druckt die ZPL-Etiketten über verschiedene Methoden (für Zebra-Drucker)
        /// Unverändert von der ursprünglichen Implementierung
        /// </summary>
        private static async Task DruckeZPLEtikettenAsync(List<string> zplCodes)
        {
            bool druckErfolgreich = false;

            foreach (string zplCode in zplCodes)
            {
                try
                {
                    // Methode 1: Direkter USB/Serieller Druck über Windows-Drucker
                    if (await DruckeUeberWindowsDrucker(zplCode))
                    {
                        druckErfolgreich = true;
                        continue;
                    }

                    // Methode 2: Netzwerk-Druck (falls IP konfiguriert)
                    if (!string.IsNullOrEmpty(_zebraDruckerIP))
                    {
                        if (await DruckeUeberNetzwerk(zplCode))
                        {
                            druckErfolgreich = true;
                            continue;
                        }
                    }

                    // Pause zwischen den Druckaufträgen
                    await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Drucken eines Etiketts: {ex.Message}");
                }
            }

            if (!druckErfolgreich)
            {
                throw new Exception("Keine der Druck-Methoden war erfolgreich. Bitte prüfen Sie die Drucker-Konfiguration.");
            }
        }

        /// <summary>
        /// Druckt ZPL über den Windows-Drucker (USB/Seriell) - für Zebra-Drucker
        /// Unverändert von der ursprünglichen Implementierung
        /// </summary>
        private static async Task<bool> DruckeUeberWindowsDrucker(string zplCode)
        {
            try
            {
                return await Task.Run(() =>
                {
                    // P/Invoke für direkten Drucker-Zugriff
                    IntPtr hPrinter = IntPtr.Zero;

                    try
                    {
                        // Drucker öffnen
                        if (!OpenPrinter(_zebraDruckerName, out hPrinter, IntPtr.Zero))
                        {
                            return false;
                        }

                        // Druckauftrag starten
                        DOC_INFO_1 docInfo = new DOC_INFO_1
                        {
                            pDocName = "LAGA Etikett",
                            pOutputFile = null,
                            pDataType = "RAW"
                        };

                        if (StartDocPrinter(hPrinter, 1, ref docInfo) == 0)
                            return false;

                        if (!StartPagePrinter(hPrinter))
                            return false;

                        // ZPL-Daten senden
                        byte[] zplBytes = Encoding.UTF8.GetBytes(zplCode);
                        uint bytesWritten = 0;

                        bool success = WritePrinter(hPrinter, zplBytes, (uint)zplBytes.Length, out bytesWritten);

                        // Druckauftrag beenden
                        EndPagePrinter(hPrinter);
                        EndDocPrinter(hPrinter);

                        return success && bytesWritten == zplBytes.Length;
                    }
                    finally
                    {
                        if (hPrinter != IntPtr.Zero)
                            ClosePrinter(hPrinter);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Windows-Drucker Fehler: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Druckt ZPL über Netzwerk (TCP/IP) - für Zebra-Drucker
        /// Unverändert von der ursprünglichen Implementierung
        /// </summary>
        private static async Task<bool> DruckeUeberNetzwerk(string zplCode)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    // Verbindung zum Drucker
                    await tcpClient.ConnectAsync(_zebraDruckerIP, _zebraDruckerPort);

                    using (var stream = tcpClient.GetStream())
                    {
                        byte[] zplBytes = Encoding.UTF8.GetBytes(zplCode);
                        await stream.WriteAsync(zplBytes, 0, zplBytes.Length);
                        await stream.FlushAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Netzwerk-Druck Fehler: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gibt den Pfad zum ZPL-Verzeichnis zurück
        /// </summary>
        public static string GetEtikettenVerzeichnis()
        {
            return EtikettenVerzeichnis;
        }

        /// <summary>
        /// Konfiguriert den Zebra-Drucker Namen
        /// </summary>
        public static void SetZebraDruckerName(string druckerName)
        {
            _zebraDruckerName = druckerName;
        }

        /// <summary>
        /// Konfiguriert die Zebra-Drucker IP für Netzwerk-Druck
        /// </summary>
        public static void SetZebraDruckerIP(string ipAdresse, int port = 9100)
        {
            _zebraDruckerIP = ipAdresse;
            _zebraDruckerPort = port;
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

                foreach (string datei in dateien)
                {
                    var fileInfo = new FileInfo(datei);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(datei);
                    }
                }
            }
            catch (Exception)
            {
                // Fehler beim Bereinigen ignorieren - nicht kritisch
            }
        }

        /// <summary>
        /// Erstellt ein Test-Etikett für Drucker-Konfiguration
        /// Funktioniert mit beiden Drucker-Typen
        /// </summary>
        public static async Task<bool> DruckeTestEtikettAsync()
        {
            try
            {
                var testEinheit = new ArtikelEinheit
                {
                    ArtikelId = 999,
                    Barcode = "1234567890",
                    ErstellungsDatum = DateTime.Now
                };

                var testArtikel = new Artikel
                {
                    Bezeichnung = "TEST-ARTIKEL"
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
        /// Gibt Informationen über den erkannten Drucker zurück
        /// NEU: Für Debugging und Status-Anzeige
        /// </summary>
        public static string GetDruckerInfo()
        {
            DruckerTyp drucker = ErmittleVerfuegbarenDrucker();

            var sb = new StringBuilder();
            sb.AppendLine("LAGA Etikett-Drucker Informationen:");
            sb.AppendLine($"Erkannter Drucker-Typ: {drucker}");

            if (drucker == DruckerTyp.Zebra)
            {
                sb.AppendLine($"Zebra-Drucker: {_zebraDruckerName}");
                sb.AppendLine("Format: ZPL (57×24mm)");
                sb.AppendLine("Druck-Methoden: USB/Seriell + Netzwerk");
            }
            else
            {
                sb.AppendLine("Standard-Windows-Drucker wird verwendet");
                sb.AppendLine("Format: Grafik (57×24mm)");
                sb.AppendLine("Druck-Methode: Windows-Druckertreiber");
            }

            sb.AppendLine();
            sb.AppendLine("Verfügbare Drucker im System:");
            foreach (string drucker_name in PrinterSettings.InstalledPrinters)
            {
                sb.AppendLine($"- {drucker_name}");
            }

            return sb.ToString();
        }

        #region P/Invoke für Windows-Drucker-API (unverändert)

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