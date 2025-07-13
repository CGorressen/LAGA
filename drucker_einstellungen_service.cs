using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Linq;

namespace LAGA
{
    /// <summary>
    /// Service für die Verwaltung der Drucker-Einstellungen
    /// Speichert und lädt die Drucker-Konfiguration in einer JSON-Datei im AppData-Ordner
    /// Jeder Rechner hat individuelle Einstellungen
    /// </summary>
    public static class DruckerEinstellungsService
    {
        /// <summary>
        /// Pfad zum LAGA-Ordner im AppData-Verzeichnis (LOCAL - nicht roaming!)
        /// Jeder PC hat seine eigenen Drucker-Einstellungen
        /// </summary>
        private static readonly string AppDataOrdner = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LAGA");

        /// <summary>
        /// Vollständiger Pfad zur JSON-Datei mit den Drucker-Einstellungen
        /// </summary>
        private static readonly string EinstellungenDatei = Path.Combine(AppDataOrdner, "drucker_einstellungen.json");

        /// <summary>
        /// Modell für die Drucker-Einstellungen die in der JSON-Datei gespeichert werden
        /// </summary>
        public class DruckerEinstellungen
        {
            /// <summary>
            /// Name des ausgewählten Druckers (wie er im System angezeigt wird)
            /// </summary>
            public string? AusgewaehlterDrucker { get; set; }

            /// <summary>
            /// Zeitstempel der letzten Änderung
            /// </summary>
            public DateTime LetzteAenderung { get; set; }

            /// <summary>
            /// Konstruktor - setzt Standard-Werte
            /// </summary>
            public DruckerEinstellungen()
            {
                LetzteAenderung = DateTime.Now;
            }
        }

        /// <summary>
        /// Statischer Konstruktor - erstellt den LAGA-Ordner falls er nicht existiert
        /// </summary>
        static DruckerEinstellungsService()
        {
            try
            {
                // LAGA-Ordner im AppData erstellen falls nicht vorhanden
                if (!Directory.Exists(AppDataOrdner))
                {
                    Directory.CreateDirectory(AppDataOrdner);
                    System.Diagnostics.Debug.WriteLine($"📁 LAGA-Ordner erstellt: {AppDataOrdner}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Erstellen des LAGA-Ordners: {ex.Message}");
            }
        }

        /// <summary>
        /// Lädt die Drucker-Einstellungen aus der JSON-Datei
        /// Gibt null zurück wenn keine Einstellungen gefunden werden
        /// </summary>
        /// <returns>DruckerEinstellungen oder null wenn keine Datei vorhanden</returns>
        public static async Task<DruckerEinstellungen?> EinstellungenLadenAsync()
        {
            try
            {
                // Prüfen ob die JSON-Datei existiert
                if (!File.Exists(EinstellungenDatei))
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ Keine Drucker-Einstellungen gefunden - erste Verwendung");
                    return null;
                }

                // JSON-Datei lesen und deserialisieren
                string jsonInhalt = await File.ReadAllTextAsync(EinstellungenDatei);

                if (string.IsNullOrWhiteSpace(jsonInhalt))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Drucker-Einstellungen Datei ist leer");
                    return null;
                }

                var einstellungen = JsonSerializer.Deserialize<DruckerEinstellungen>(jsonInhalt);

                System.Diagnostics.Debug.WriteLine($"✅ Drucker-Einstellungen geladen: {einstellungen?.AusgewaehlterDrucker}");
                return einstellungen;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Laden der Drucker-Einstellungen: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Speichert die Drucker-Einstellungen in der JSON-Datei
        /// Überschreibt vorhandene Einstellungen
        /// </summary>
        /// <param name="druckerName">Name des ausgewählten Druckers</param>
        /// <returns>True wenn erfolgreich gespeichert, false bei Fehlern</returns>
        public static async Task<bool> EinstellungenSpeichernAsync(string druckerName)
        {
            try
            {
                // Neue Einstellungen erstellen
                var einstellungen = new DruckerEinstellungen
                {
                    AusgewaehlterDrucker = druckerName,
                    LetzteAenderung = DateTime.Now
                };

                // JSON-Optionen für lesbare Formatierung
                var jsonOptionen = new JsonSerializerOptions
                {
                    WriteIndented = true  // Macht die JSON-Datei lesbar
                };

                // Zu JSON serialisieren
                string jsonInhalt = JsonSerializer.Serialize(einstellungen, jsonOptionen);

                // In Datei schreiben
                await File.WriteAllTextAsync(EinstellungenDatei, jsonInhalt);

                System.Diagnostics.Debug.WriteLine($"✅ Drucker-Einstellungen gespeichert: {druckerName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Speichern der Drucker-Einstellungen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gibt alle verfügbaren Drucker im System zurück (lokal und Netzwerk)
        /// </summary>
        /// <returns>Liste aller verfügbaren Drucker-Namen</returns>
        public static List<string> VerfuegbareDruckerHolen()
        {
            try
            {
                var drucker = new List<string>();

                // Alle installierten Drucker durchgehen
                foreach (string druckerName in PrinterSettings.InstalledPrinters)
                {
                    drucker.Add(druckerName);
                }

                System.Diagnostics.Debug.WriteLine($"🖨️ {drucker.Count} Drucker gefunden");
                return drucker.OrderBy(d => d).ToList(); // Alphabetisch sortieren
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Abrufen der verfügbaren Drucker: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Prüft ob ein Drucker mit dem angegebenen Namen im System verfügbar ist
        /// </summary>
        /// <param name="druckerName">Name des zu prüfenden Druckers</param>
        /// <returns>True wenn Drucker verfügbar, false wenn nicht</returns>
        public static bool IstDruckerVerfuegbar(string druckerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(druckerName))
                    return false;

                // Prüfen ob der Drucker in der Liste der installierten Drucker enthalten ist
                foreach (string verfuegbarerDrucker in PrinterSettings.InstalledPrinters)
                {
                    if (string.Equals(verfuegbarerDrucker, druckerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Prüfen der Drucker-Verfügbarkeit: {ex.Message}");
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

        /// <summary>
        /// Löscht die Drucker-Einstellungen (zurücksetzen auf Standard)
        /// </summary>
        /// <returns>True wenn erfolgreich gelöscht oder Datei existierte nicht</returns>
        public static async Task<bool> EinstellungenZuruecksetzenAsync()
        {
            try
            {
                if (File.Exists(EinstellungenDatei))
                {
                    await Task.Run(() => File.Delete(EinstellungenDatei));
                    System.Diagnostics.Debug.WriteLine("✅ Drucker-Einstellungen zurückgesetzt");
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Zurücksetzen der Drucker-Einstellungen: {ex.Message}");
                return false;
            }
        }
    }
}