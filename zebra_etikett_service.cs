using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace LAGA
{
    /// <summary>
    /// Service für die Generierung und den Druck von Zebra ZPL-Etiketten
    /// Erstellt ZPL-Etiketten im Format 57×24mm mit Barcode und Artikelinformationen
    /// für den Zebra GX420t Drucker
    /// </summary>
    public static class ZebraEtikettService
    {
        /// <summary>
        /// Verzeichnis für die ZPL-Dateien (als Backup/Debug)
        /// </summary>
        private static readonly string EtikettenVerzeichnis = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ZPL_Etiketten");

        /// <summary>
        /// Standard-Druckername für Zebra GX420t (kann angepasst werden)
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
        /// Statischer Konstruktor - erstellt ZPL-Verzeichnis
        /// </summary>
        static ZebraEtikettService()
        {
            // ZPL-Verzeichnis erstellen falls es nicht existiert
            Directory.CreateDirectory(EtikettenVerzeichnis);
        }

        /// <summary>
        /// Erstellt und druckt ZPL-Etiketten für eine Liste von ArtikelEinheiten
        /// </summary>
        /// <param name="artikelEinheiten">Liste der ArtikelEinheiten für die Etiketten erstellt werden sollen</param>
        /// <param name="artikel">Der zugehörige Artikel mit Bezeichnung</param>
        public static async Task<bool> ErstelleUndDruckeEtikettenAsync(
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
                throw new Exception($"Fehler beim Erstellen der ZPL-Etiketten: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt den ZPL-Code für ein einzelnes Etikett
        /// Format: 57×24mm (ca. 456×192 Dots bei 203 DPI - Zebra Standard)
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

                // Barcode manuell zentriert: falls der barcode versetzt ist, einfach mal mit dem wert F085 spielen.
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
        private static async Task SpeichereZPLDateiAsync(ArtikelEinheit einheit, string zplCode)
        {
            try
            {
                string dateiname = $"{einheit.ArtikelId}_{einheit.Barcode}.zpl";
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
        /// Druckt die ZPL-Etiketten über verschiedene Methoden
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
        /// Druckt ZPL über den Windows-Drucker (USB/Seriell)
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
                            // Fallback: Versuche "Generic / Text Only" Drucker zu finden
                            var druckerListe = PrinterSettings.InstalledPrinters;
                            foreach (string drucker in druckerListe)
                            {
                                if (drucker.ToLower().Contains("zebra") || drucker.ToLower().Contains("gx420"))
                                {
                                    _zebraDruckerName = drucker;
                                    if (OpenPrinter(_zebraDruckerName, out hPrinter, IntPtr.Zero))
                                        break;
                                }
                            }

                            if (hPrinter == IntPtr.Zero)
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
        /// Druckt ZPL über Netzwerk (TCP/IP)
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
        /// </summary>
        public static async Task<bool> DruckeTestEtikettAsync()
        {
            try
            {
                var testEinheit = new ArtikelEinheit
                {
                    ArtikelId = 999,
                    Barcode = "1234567890"
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

        #region P/Invoke für Windows-Drucker-API

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