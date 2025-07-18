using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace LAGA
{
    /// <summary>
    /// Zentraler Service für alle Datenbank-Backup-Operationen
    /// Führt automatische Backups durch und verwaltet die Backup-Ordner
    /// Bereinigt automatisch alte Backups (älter als 3 Tage)
    /// ERWEITERT: Unterstützt jetzt echte Progress-Callbacks für Loading-Fenster
    /// </summary>
    public static class BackupService
    {
        /// <summary>
        /// Führt ein automatisches Backup der Datenbank durch mit Progress-Callbacks
        /// Wird beim Programmstart aufgerufen
        /// </summary>
        /// <param name="progressCallback">Callback für Fortschritts-Updates (Prozent, Status-Text) - kann null sein</param>
        /// <returns>True wenn Backup erfolgreich erstellt wurde</returns>
        public static async Task<bool> AutomatischesBackupAsync(IProgress<(int percent, string status)>? progressCallback = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Starte automatisches Datenbank-Backup...");
                progressCallback?.Report((20, "Prüfe Datenbank..."));

                // Prüfen ob Datenbankdatei existiert
                if (!File.Exists(PathHelper.DatabaseFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Keine Datenbank gefunden - Backup übersprungen");
                    progressCallback?.Report((80, "Keine Datenbank vorhanden"));
                    return true; // Kein Fehler, nur keine Datenbank vorhanden
                }

                progressCallback?.Report((25, "Ermittle Backup-Pfad..."));

                // Aktuellen Backup-Pfad ermitteln (Standard oder benutzerdefiniert)
                string backupBasisPfad = await BackupEinstellungsService.AktuellenBackupPfadHolenAsync();

                progressCallback?.Report((30, "Starte Datei-Kopierung..."));

                // Backup durchführen mit echtem Progress-Tracking
                bool erfolg = await BackupErstellenMitProgressAsync(backupBasisPfad, progressCallback);

                if (erfolg)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Automatisches Backup erfolgreich erstellt");
                    progressCallback?.Report((75, "Backup erfolgreich erstellt"));

                    // Alte Backups bereinigen (nur die letzten 3 Tage behalten)
                    progressCallback?.Report((78, "Bereinige alte Backups..."));
                    await AlteBackupsBereinigenAsync(backupBasisPfad);
                    progressCallback?.Report((80, "Backup-Bereinigung abgeschlossen"));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Automatisches Backup fehlgeschlagen");
                    progressCallback?.Report((80, "Backup fehlgeschlagen"));
                }

                return erfolg;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim automatischen Backup: {ex.Message}");
                progressCallback?.Report((80, "Backup-Fehler (nicht kritisch)"));
                return false;
            }
        }

        /// <summary>
        /// Erstellt ein Backup mit echtem dateibasiertem Fortschritts-Tracking
        /// ORIGINALE STRUKTUR: [Backup-Pfad]/[ttmmjjjj-hhmm]/Lager.db
        /// </summary>
        /// <param name="backupBasisPfad">Ziel-Ordner für das Backup</param>
        /// <param name="progressCallback">Callback für Fortschritts-Updates - kann null sein</param>
        /// <returns>True wenn erfolgreich</returns>
        private static async Task<bool> BackupErstellenMitProgressAsync(string backupBasisPfad, IProgress<(int percent, string status)>? progressCallback)
        {
            try
            {
                progressCallback?.Report((30, "Erstelle Backup-Ordner..."));

                // Zeitstempel für Backup-Ordner erstellen (ttmmjjjj-hhmm) - ORIGINALES FORMAT
                string zeitstempel = DateTime.Now.ToString("ddMMyyyy-HHmm");

                // Backup-Zielordner erstellen - ORIGINALE STRUKTUR
                string backupOrdner = Path.Combine(backupBasisPfad, zeitstempel);
                Directory.CreateDirectory(backupOrdner);

                // Ziel-Dateiname für das Backup - ORIGINAL: "Lager.db"
                string backupDateiPfad = Path.Combine(backupOrdner, "Lager.db");

                progressCallback?.Report((35, "Ermittle Dateigröße..."));

                // Größe der Quelldatei ermitteln für Progress-Berechnung
                var quellInfo = new FileInfo(PathHelper.DatabaseFilePath);
                long gesamtGroesse = quellInfo.Length;

                progressCallback?.Report((40, $"Kopiere {FormatFileSize(gesamtGroesse)}..."));

                // Datei in Blöcken kopieren mit Progress-Updates
                const int pufferGroesse = 64 * 1024; // 64KB Blöcke
                byte[] puffer = new byte[pufferGroesse];
                long kopierteBytes = 0;

                using (var quellStream = new FileStream(PathHelper.DatabaseFilePath, FileMode.Open, FileAccess.Read))
                using (var zielStream = new FileStream(backupDateiPfad, FileMode.Create, FileAccess.Write))
                {
                    int gelesenBytes;
                    while ((gelesenBytes = await quellStream.ReadAsync(puffer, 0, pufferGroesse)) > 0)
                    {
                        await zielStream.WriteAsync(puffer, 0, gelesenBytes);
                        kopierteBytes += gelesenBytes;

                        // Progress berechnen (40% bis 70% des Gesamtprogress)
                        int fortschrittsProzent = 40 + (int)((kopierteBytes * 30) / gesamtGroesse);
                        string status = $"Kopiere... {FormatFileSize(kopierteBytes)}/{FormatFileSize(gesamtGroesse)}";

                        progressCallback?.Report((fortschrittsProzent, status));

                        // Kleine Pause für UI-Responsiveness bei sehr großen Dateien
                        if (kopierteBytes % (1024 * 1024) == 0) // Jede MB
                        {
                            await Task.Delay(1);
                        }
                    }
                }

                progressCallback?.Report((70, "Backup-Datei erstellt"));

                // Integrität prüfen
                progressCallback?.Report((72, "Prüfe Backup-Integrität..."));
                var zielInfo = new FileInfo(backupDateiPfad);

                if (zielInfo.Length != gesamtGroesse)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Backup-Größe stimmt nicht überein! Quelle: {gesamtGroesse}, Ziel: {zielInfo.Length}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Backup erfolgreich erstellt: {backupOrdner}/Lager.db ({FormatFileSize(gesamtGroesse)})");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Backup erstellen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Führt ein manuelles Backup der Datenbank durch (aus dem MainWindow heraus)
        /// Verwendet dieselbe Progress-Logik wie das automatische Backup
        /// </summary>
        /// <param name="progressCallback">Callback für Fortschritts-Updates - kann null sein</param>
        /// <returns>True wenn Backup erfolgreich erstellt wurde</returns>
        public static async Task<bool> ManuellesBackupAsync(IProgress<(int percent, string status)>? progressCallback = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Starte manuelles Datenbank-Backup...");

                // Aktuellen Backup-Pfad ermitteln
                string backupBasisPfad = await BackupEinstellungsService.AktuellenBackupPfadHolenAsync();

                // Backup mit Progress erstellen
                return await BackupErstellenMitProgressAsync(backupBasisPfad, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim manuellen Backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bereinigt alte Backup-Dateien (älter als 3 Tage)
        /// ORIGINALE STRUKTUR: Löscht komplette Zeitstempel-Ordner
        /// </summary>
        /// <param name="backupPfad">Backup-Ordner zum Bereinigen</param>
        private static async Task AlteBackupsBereinigenAsync(string backupPfad)
        {
            try
            {
                // Auf Hintergrundthread ausführen da Datei-Operationen CPU-intensiv sein können
                await Task.Run(() =>
                {
                    if (!Directory.Exists(backupPfad))
                        return;

                    // Schwellenwert: 3 Tage vor heute
                    DateTime schwellenwert = DateTime.Now.AddDays(-3);
                    int geloeschteOrdner = 0;

                    // Alle Unterordner im Backup-Pfad durchgehen
                    var backupOrdner = Directory.GetDirectories(backupPfad);

                    foreach (string ordner in backupOrdner)
                    {
                        try
                        {
                            string ordnerName = Path.GetFileName(ordner);

                            // Versuche Datum aus Ordnername zu extrahieren (ttmmjjjj-hhmm)
                            if (TryParseDateFromFolderName(ordnerName, out DateTime ordnerDatum))
                            {
                                // Wenn Ordner älter als 3 Tage, kompletten Ordner löschen
                                if (ordnerDatum < schwellenwert)
                                {
                                    Directory.Delete(ordner, true);
                                    geloeschteOrdner++;
                                    System.Diagnostics.Debug.WriteLine($"🗑️ Altes Backup gelöscht: {ordnerName}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Löschen von Backup-Ordner {ordner}: {ex.Message}");
                        }
                    }

                    if (geloeschteOrdner > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ {geloeschteOrdner} alte Backup-Ordner bereinigt");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Backup-Bereinigung abgeschlossen - keine alten Ordner gefunden");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler bei Backup-Bereinigung: {ex.Message}");
            }
        }

        /// <summary>
        /// Versucht ein Datum aus einem Backup-Ordnernamen zu extrahieren
        /// Format: ttmmjjjj-hhmm (z.B. "18072025-1430")
        /// </summary>
        /// <param name="ordnerName">Name des Backup-Ordners</param>
        /// <param name="datum">Extrahiertes Datum</param>
        /// <returns>True wenn Datum erfolgreich extrahiert wurde</returns>
        private static bool TryParseDateFromFolderName(string ordnerName, out DateTime datum)
        {
            datum = DateTime.MinValue;

            try
            {
                // Format prüfen: ttmmjjjj-hhmm (13 Zeichen mit Bindestrich an Position 8)
                if (ordnerName.Length != 13 || ordnerName[8] != '-')
                    return false;

                // Datum-Teil: ttmmjjjj
                string datumTeil = ordnerName.Substring(0, 8);
                string zeitTeil = ordnerName.Substring(9, 4);

                // Tag, Monat, Jahr extrahieren
                int tag = int.Parse(datumTeil.Substring(0, 2));
                int monat = int.Parse(datumTeil.Substring(2, 2));
                int jahr = int.Parse(datumTeil.Substring(4, 4));

                // Stunde, Minute extrahieren
                int stunde = int.Parse(zeitTeil.Substring(0, 2));
                int minute = int.Parse(zeitTeil.Substring(2, 2));

                // DateTime erstellen
                datum = new DateTime(jahr, monat, tag, stunde, minute, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Formatiert Dateigrößen in lesbarer Form (Bytes, KB, MB)
        /// </summary>
        /// <param name="bytes">Größe in Bytes</param>
        /// <returns>Formatierte Größe als String</returns>
        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} Bytes";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024:F1} KB";
            else
                return $"{bytes / (1024 * 1024):F1} MB";
        }
    }

}
