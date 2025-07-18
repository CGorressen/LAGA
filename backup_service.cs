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
    /// </summary>
    public static class BackupService
    {
        /// <summary>
        /// Führt ein automatisches Backup der Datenbank durch
        /// Wird beim Programmstart aufgerufen
        /// </summary>
        /// <returns>True wenn Backup erfolgreich erstellt wurde</returns>
        public static async Task<bool> AutomatischesBackupAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Starte automatisches Datenbank-Backup...");

                // Prüfen ob Datenbankdatei existiert
                if (!File.Exists(PathHelper.DatabaseFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Keine Datenbank gefunden - Backup übersprungen");
                    return true; // Kein Fehler, nur keine Datenbank vorhanden
                }

                // Aktuellen Backup-Pfad ermitteln (Standard oder benutzerdefiniert)
                string backupBasisPfad = await BackupEinstellungsService.AktuellenBackupPfadHolenAsync();

                // Backup durchführen
                bool erfolg = await BackupErstellenAsync(backupBasisPfad);

                if (erfolg)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Automatisches Backup erfolgreich erstellt");
                    
                    // Alte Backups bereinigen (nur die letzten 3 Tage behalten)
                    await AlteBackupsBereinigenAsync(backupBasisPfad);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Automatisches Backup fehlgeschlagen");
                }

                return erfolg;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim automatischen Backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Erstellt ein Backup der Datenbank im angegebenen Pfad
        /// Format: [Backup-Pfad]/[ttmmjjjj-hhmm]/Lager.db
        /// </summary>
        /// <param name="backupBasisPfad">Basis-Pfad für das Backup</param>
        /// <returns>True wenn Backup erfolgreich erstellt wurde</returns>
        public static async Task<bool> BackupErstellenAsync(string backupBasisPfad)
        {
            try
            {
                // Zeitstempel für Backup-Ordner erstellen (ttmmjjjj-hhmm)
                string zeitstempel = DateTime.Now.ToString("ddMMyyyy-HHmm");
                
                // Backup-Zielordner erstellen
                string backupOrdner = Path.Combine(backupBasisPfad, zeitstempel);
                Directory.CreateDirectory(backupOrdner);

                // Ziel-Dateiname für das Backup
                string backupDateiPfad = Path.Combine(backupOrdner, "Lager.db");

                // Sichere Dateikopie durchführen
                await Task.Run(() => File.Copy(PathHelper.DatabaseFilePath, backupDateiPfad, true));

                // Backup-Info für Debugging
                FileInfo backupInfo = new FileInfo(backupDateiPfad);
                System.Diagnostics.Debug.WriteLine($"✅ Backup erstellt: {backupDateiPfad} ({backupInfo.Length} Bytes)");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Erstellen des Backups: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bereinigt alte Backup-Ordner und behält nur die letzten 3 Tage
        /// </summary>
        /// <param name="backupBasisPfad">Basis-Pfad der Backups</param>
        /// <returns>Anzahl der gelöschten Backup-Ordner</returns>
        public static async Task<int> AlteBackupsBereinigenAsync(string backupBasisPfad)
        {
            try
            {
                if (!Directory.Exists(backupBasisPfad))
                {
                    return 0; // Kein Backup-Ordner vorhanden
                }

                // Schwellenwert: 3 Tage vor heute
                DateTime schwellenwert = DateTime.Now.AddDays(-3);
                int geloeschteOrdner = 0;

                // Alle Unterordner im Backup-Pfad durchgehen
                var backupOrdner = Directory.GetDirectories(backupBasisPfad);

                foreach (string ordner in backupOrdner)
                {
                    try
                    {
                        string ordnerName = Path.GetFileName(ordner);
                        
                        // Versuche Datum aus Ordnername zu extrahieren (ttmmjjjj-hhmm)
                        if (TryParseDateFromFolderName(ordnerName, out DateTime ordnerDatum))
                        {
                            // Wenn Ordner älter als 3 Tage, löschen
                            if (ordnerDatum < schwellenwert)
                            {
                                await Task.Run(() => Directory.Delete(ordner, true));
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

                return geloeschteOrdner;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Bereinigen alter Backups: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Versucht ein Datum aus einem Backup-Ordnernamen zu extrahieren
        /// Format: ttmmjjjj-hhmm
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
        /// Gibt eine Liste aller vorhandenen Backups im angegebenen Pfad zurück
        /// </summary>
        /// <param name="backupBasisPfad">Basis-Pfad der Backups</param>
        /// <returns>Array mit Backup-Informationen (Ordnername, Datum, Größe)</returns>
        public static async Task<BackupInfo[]> BackupListeHolenAsync(string backupBasisPfad)
        {
            try
            {
                if (!Directory.Exists(backupBasisPfad))
                {
                    return new BackupInfo[0];
                }

                var backups = new List<BackupInfo>();
                var ordner = Directory.GetDirectories(backupBasisPfad);

                foreach (string ordnerPfad in ordner)
                {
                    try
                    {
                        string ordnerName = Path.GetFileName(ordnerPfad);
                        string datenbankPfad = Path.Combine(ordnerPfad, "Lager.db");

                        if (File.Exists(datenbankPfad))
                        {
                            FileInfo dateiInfo = new FileInfo(datenbankPfad);
                            TryParseDateFromFolderName(ordnerName, out DateTime backupDatum);

                            backups.Add(new BackupInfo
                            {
                                OrdnerName = ordnerName,
                                BackupDatum = backupDatum,
                                DateiGroesse = dateiInfo.Length,
                                VollstaendigerPfad = ordnerPfad
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Analysieren von Backup {ordnerPfad}: {ex.Message}");
                    }
                }

                // Nach Datum sortieren (neueste zuerst)
                return backups.OrderByDescending(b => b.BackupDatum).ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Abrufen der Backup-Liste: {ex.Message}");
                return new BackupInfo[0];
            }
        }

        /// <summary>
        /// Informationen über ein Backup
        /// </summary>
        public class BackupInfo
        {
            public string OrdnerName { get; set; } = "";
            public DateTime BackupDatum { get; set; }
            public long DateiGroesse { get; set; }
            public string VollstaendigerPfad { get; set; } = "";
        }
    }
}