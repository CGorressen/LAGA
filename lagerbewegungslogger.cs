using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LAGA
{
    /// <summary>
    /// Service-Klasse für das Logging aller Lagerbewegungen (Ein- und Auslagerungen)
    /// Erstellt automatisch eine Log-Datei im Lagerbewegung-Ordner mit detaillierten Informationen
    /// </summary>
    public static class LagerbewegungsLogger
    {
        /// <summary>
        /// Semaphore für thread-sichere Dateizugriffe
        /// Verhindert gleichzeitige Schreibvorgänge in die Log-Datei
        /// </summary>
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Name der Log-Datei im Lagerbewegung-Ordner
        /// </summary>
        private static readonly string LogFileName = "lagerbewegungen.txt";

        /// <summary>
        /// Vollständiger Pfad zur Log-Datei
        /// </summary>
        private static string LogFilePath => Path.Combine(PathHelper.LagerbewegungDirectory, LogFileName);

        /// <summary>
        /// Loggt eine Einlagerung von Artikeln
        /// </summary>
        /// <param name="artikel">Der eingelagerte Artikel</param>
        /// <param name="menge">Anzahl der eingelagerten Einheiten</param>
        /// <param name="bestandVorher">Bestand vor der Einlagerung</param>
        /// <param name="bestandNachher">Bestand nach der Einlagerung</param>
        /// <param name="barcodes">Liste der generierten Barcodes für die neuen Einheiten</param>
        public static async Task LoggeEinlagerungAsync(string artikelBezeichnung, int menge, int bestandVorher, int bestandNachher, List<string> barcodes)
        {
            try
            {
                // Log-Eintrag erstellen
                var logEintrag = ErstelleLogEintrag(
                    artikelBezeichnung: artikelBezeichnung,
                    aktion: "Einlagern",
                    menge: menge,
                    bestandVorher: bestandVorher,
                    bestandNachher: bestandNachher,
                    barcodes: barcodes
                );

                // Asynchron in Datei schreiben
                await SchreibeLogEintragAsync(logEintrag);

                // Debug-Information für Entwicklung
                System.Diagnostics.Debug.WriteLine($"✅ Einlagerung geloggt: {artikelBezeichnung} - {menge} Stück");
            }
            catch (Exception ex)
            {
                // Fehler beim Logging sollten die Hauptfunktion nicht beeinträchtigen
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Loggen der Einlagerung: {ex.Message}");
            }
        }

        /// <summary>
        /// Loggt eine Auslagerung von Artikeln
        /// </summary>
        /// <param name="artikelBezeichnung">Bezeichnung des ausgelagerten Artikels</param>
        /// <param name="menge">Anzahl der ausgelagerten Einheiten</param>
        /// <param name="bestandVorher">Bestand vor der Auslagerung</param>
        /// <param name="bestandNachher">Bestand nach der Auslagerung</param>
        /// <param name="barcodes">Liste der Barcodes der ausgelagerten Einheiten</param>
        public static async Task LoggeAuslagerungAsync(string artikelBezeichnung, int menge, int bestandVorher, int bestandNachher, List<string> barcodes)
        {
            try
            {
                // Log-Eintrag erstellen
                var logEintrag = ErstelleLogEintrag(
                    artikelBezeichnung: artikelBezeichnung,
                    aktion: "Auslagern",
                    menge: menge,
                    bestandVorher: bestandVorher,
                    bestandNachher: bestandNachher,
                    barcodes: barcodes
                );

                // Asynchron in Datei schreiben
                await SchreibeLogEintragAsync(logEintrag);

                // Debug-Information für Entwicklung
                System.Diagnostics.Debug.WriteLine($"✅ Auslagerung geloggt: {artikelBezeichnung} - {menge} Stück");
            }
            catch (Exception ex)
            {
                // Fehler beim Logging sollten die Hauptfunktion nicht beeinträchtigen
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Loggen der Auslagerung: {ex.Message}");
            }
        }

        /// <summary>
        /// Erstellt einen formatierten Log-Eintrag im gewünschten Format
        /// Format: Artikel: [Name] | Aktion: [Aktion] | Datum: [Datum] | Menge: [Anzahl] | Bestand vorher: [Zahl] | Bestand nachher: [Zahl] | Barcodes: [Liste]
        /// </summary>
        /// <param name="artikelBezeichnung">Name des Artikels</param>
        /// <param name="aktion">Art der Bewegung (Einlagern/Auslagern)</param>
        /// <param name="menge">Anzahl der bewegten Einheiten</param>
        /// <param name="bestandVorher">Bestand vor der Bewegung</param>
        /// <param name="bestandNachher">Bestand nach der Bewegung</param>
        /// <param name="barcodes">Liste der betroffenen Barcodes</param>
        /// <returns>Formatierter Log-Eintrag als String</returns>
        private static string ErstelleLogEintrag(string artikelBezeichnung, string aktion, int menge, int bestandVorher, int bestandNachher, List<string> barcodes)
        {
            // Aktuelles Datum und Uhrzeit im gewünschten Format
            var zeitstempel = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            // Barcodes als kommagetrennte Liste in eckigen Klammern formatieren
            var barcodeString = barcodes != null && barcodes.Any() 
                ? $"[{string.Join(", ", barcodes)}]" 
                : "[]";

            // Log-Eintrag nach dem gewünschten Format zusammenbauen
            var logEintrag = $"Artikel: {artikelBezeichnung} | " +
                           $"Aktion: {aktion} | " +
                           $"Datum: {zeitstempel} | " +
                           $"Menge: {menge} | " +
                           $"Bestand vorher: {bestandVorher} | " +
                           $"Bestand nachher: {bestandNachher} | " +
                           $"Barcodes: {barcodeString}";

            return logEintrag;
        }

        /// <summary>
        /// Schreibt einen Log-Eintrag thread-sicher in die Log-Datei
        /// Erstellt die Datei automatisch falls sie nicht existiert
        /// </summary>
        /// <param name="logEintrag">Der zu schreibende Log-Eintrag</param>
        private static async Task SchreibeLogEintragAsync(string logEintrag)
        {
            // Thread-sicheren Zugriff auf die Datei gewährleisten
            await _fileLock.WaitAsync();
            
            try
            {
                // Sicherstellen dass der Lagerbewegung-Ordner existiert
                Directory.CreateDirectory(PathHelper.LagerbewegungDirectory);

                // Log-Eintrag mit Zeilenumbruch in die Datei anhängen
                // Verwendet UTF-8 Encoding für korrekte Darstellung von Sonderzeichen
                await File.AppendAllTextAsync(LogFilePath, logEintrag + Environment.NewLine, Encoding.UTF8);
            }
            finally
            {
                // Semaphore wieder freigeben
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Gibt Informationen über die Log-Datei für Debugging zurück
        /// </summary>
        /// <returns>String mit Informationen über den aktuellen Log-Status</returns>
        public static string GetLogInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Lagerbewegungslogger Informationen:");
            sb.AppendLine($"Log-Datei: {LogFilePath}");
            sb.AppendLine($"Lagerbewegung-Ordner: {PathHelper.LagerbewegungDirectory}");
            
            if (File.Exists(LogFilePath))
            {
                var fileInfo = new FileInfo(LogFilePath);
                sb.AppendLine($"Datei existiert: Ja");
                sb.AppendLine($"Dateigröße: {fileInfo.Length} Bytes");
                sb.AppendLine($"Letzte Änderung: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm:ss}");
                
                // Anzahl der Log-Einträge zählen (grobe Schätzung über Zeilenanzahl)
                try
                {
                    var lines = File.ReadAllLines(LogFilePath);
                    sb.AppendLine($"Geschätzte Anzahl Log-Einträge: {lines.Length}");
                }
                catch
                {
                    sb.AppendLine("Anzahl Log-Einträge: Konnte nicht ermittelt werden");
                }
            }
            else
            {
                sb.AppendLine("Datei existiert: Nein (wird beim ersten Log-Eintrag erstellt)");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Liest die letzten X Log-Einträge aus der Datei (für Debug-Zwecke)
        /// </summary>
        /// <param name="anzahl">Anzahl der letzten Einträge die gelesen werden sollen</param>
        /// <returns>Liste der letzten Log-Einträge</returns>
        public static async Task<List<string>> GetLetzteLogEintraegeAsync(int anzahl = 10)
        {
            var eintraege = new List<string>();

            if (!File.Exists(LogFilePath))
            {
                return eintraege; // Leere Liste wenn Datei nicht existiert
            }

            try
            {
                // Alle Zeilen aus der Datei lesen
                var alleZeilen = await File.ReadAllLinesAsync(LogFilePath, Encoding.UTF8);
                
                // Die letzten X Zeilen auswählen
                eintraege = alleZeilen
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .TakeLast(anzahl)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Lesen der Log-Einträge: {ex.Message}");
            }

            return eintraege;
        }
    }
}