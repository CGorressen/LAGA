using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace LAGA
{
    /// <summary>
    /// Service für die Verwaltung der Backup-Einstellungen
    /// Speichert und lädt die Backup-Konfiguration in einer JSON-Datei im Einstellungen-Ordner
    /// Ermöglicht benutzerdefinierte Backup-Pfade oder Standard-Backup-Ordner
    /// </summary>
    public static class BackupEinstellungsService
    {
        /// <summary>
        /// Vollständiger Pfad zur JSON-Datei mit den Backup-Einstellungen
        /// </summary>
        private static readonly string EinstellungenDatei = Path.Combine(PathHelper.EinstellungsDirectory, "backup_einstellungen.json");

        /// <summary>
        /// Modell für die Backup-Einstellungen die in der JSON-Datei gespeichert werden
        /// </summary>
        public class BackupEinstellungen
        {
            /// <summary>
            /// Benutzerdefinierter Backup-Pfad (null = Standard-Pfad verwenden)
            /// </summary>
            public string? BenutzerdefiniertePfad { get; set; }

            /// <summary>
            /// Zeitstempel der letzten Änderung
            /// </summary>
            public DateTime LetzteAenderung { get; set; }

            /// <summary>
            /// Konstruktor - setzt Standard-Werte
            /// </summary>
            public BackupEinstellungen()
            {
                LetzteAenderung = DateTime.Now;
            }
        }

        /// <summary>
        /// Lädt die Backup-Einstellungen aus der JSON-Datei
        /// Gibt null zurück wenn keine Einstellungen gefunden werden (= Standard-Pfad verwenden)
        /// </summary>
        /// <returns>BackupEinstellungen oder null wenn keine Datei vorhanden</returns>
        public static async Task<BackupEinstellungen?> EinstellungenLadenAsync()
        {
            try
            {
                // Prüfen ob die JSON-Datei existiert
                if (!File.Exists(EinstellungenDatei))
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Keine Backup-Einstellungen gefunden - Standard-Pfad wird verwendet");
                    return null;
                }

                // JSON-Datei lesen und deserialisieren
                string jsonInhalt = await File.ReadAllTextAsync(EinstellungenDatei);
                var einstellungen = JsonSerializer.Deserialize<BackupEinstellungen>(jsonInhalt);

                System.Diagnostics.Debug.WriteLine($"✅ Backup-Einstellungen geladen: {einstellungen?.BenutzerdefiniertePfad ?? "Standard-Pfad"}");
                return einstellungen;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Laden der Backup-Einstellungen: {ex.Message}");
                return null; // Bei Fehler Standard-Pfad verwenden
            }
        }

        /// <summary>
        /// Speichert die Backup-Einstellungen in der JSON-Datei
        /// </summary>
        /// <param name="einstellungen">Zu speichernde Backup-Einstellungen</param>
        /// <returns>True wenn erfolgreich gespeichert</returns>
        public static async Task<bool> EinstellungenSpeichernAsync(BackupEinstellungen einstellungen)
        {
            try
            {
                // Zeitstempel aktualisieren
                einstellungen.LetzteAenderung = DateTime.Now;

                // JSON-Optionen für lesbares Format
                var jsonOptionen = new JsonSerializerOptions
                {
                    WriteIndented = true // Schöne Formatierung
                };

                // Objekt zu JSON serialisieren
                string jsonInhalt = JsonSerializer.Serialize(einstellungen, jsonOptionen);

                // In Datei schreiben
                await File.WriteAllTextAsync(EinstellungenDatei, jsonInhalt);

                System.Diagnostics.Debug.WriteLine($"✅ Backup-Einstellungen gespeichert: {einstellungen.BenutzerdefiniertePfad ?? "Standard-Pfad"}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Speichern der Backup-Einstellungen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gibt den aktuell konfigurierten Backup-Pfad zurück (Standard oder benutzerdefiniert)
        /// </summary>
        /// <returns>Vollständiger Pfad zum Backup-Ordner</returns>
        public static async Task<string> AktuellenBackupPfadHolenAsync()
        {
            try
            {
                var einstellungen = await EinstellungenLadenAsync();
                
                // Wenn benutzerdefinierter Pfad vorhanden, diesen verwenden
                if (einstellungen?.BenutzerdefiniertePfad != null && 
                    Directory.Exists(einstellungen.BenutzerdefiniertePfad))
                {
                    // Benutzer-Pfad mit "LAGA Backup" Unterordner
                    return Path.Combine(einstellungen.BenutzerdefiniertePfad, "LAGA Backup");
                }
                
                // Sonst Standard-Backup-Ordner verwenden
                return PathHelper.BackupDirectory;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Ermitteln des Backup-Pfads: {ex.Message}");
                // Bei Fehler immer Standard-Pfad zurückgeben
                return PathHelper.BackupDirectory;
            }
        }

        /// <summary>
        /// Prüft ob ein Pfad für Backups gültig ist (Pfad existiert und ist beschreibbar)
        /// </summary>
        /// <param name="pfad">Zu prüfender Pfad</param>
        /// <returns>True wenn der Pfad für Backups verwendbar ist</returns>
        public static bool IstPfadGueltig(string pfad)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pfad))
                    return false;

                // Prüfen ob Pfad existiert
                if (!Directory.Exists(pfad))
                    return false;

                // Prüfen ob in den Pfad geschrieben werden kann (Test-Datei erstellen)
                string testDatei = Path.Combine(pfad, $"laga_test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testDatei, "test");
                File.Delete(testDatei);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Pfad nicht gültig ({pfad}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Löscht die Backup-Einstellungen (zurücksetzen auf Standard-Pfad)
        /// </summary>
        /// <returns>True wenn erfolgreich gelöscht oder Datei existierte nicht</returns>
        public static async Task<bool> EinstellungenZuruecksetzenAsync()
        {
            try
            {
                if (File.Exists(EinstellungenDatei))
                {
                    await Task.Run(() => File.Delete(EinstellungenDatei));
                    System.Diagnostics.Debug.WriteLine("✅ Backup-Einstellungen zurückgesetzt - Standard-Pfad wird verwendet");
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Zurücksetzen der Backup-Einstellungen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gibt den Pfad zur Einstellungen-Datei zurück (für Debugging)
        /// </summary>
        /// <returns>Vollständiger Pfad zur JSON-Datei</returns>
        public static string GetEinstellungenDateiPfad()
        {
            return EinstellungenDatei;
        }
    }
}