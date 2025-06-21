using System;
using System.IO;

namespace LAGA
{
    /// <summary>
    /// Zentrale Klasse für die Verwaltung aller Anwendungspfade
    /// Stellt sicher, dass alle Dateien relativ zum Anwendungsverzeichnis gespeichert werden
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Basisverzeichnis der Anwendung (Ordner mit der .exe-Datei)
        /// Verwendet AppDomain.CurrentDomain.BaseDirectory für portable Anwendungen
        /// </summary>
        public static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Pfad zum Datenbank-Ordner
        /// </summary>
        public static string DatabaseDirectory => Path.Combine(ApplicationDirectory, "Datenbank");

        /// <summary>
        /// Pfad zum Credentials-Ordner (für zukünftige Nutzung)
        /// </summary>
        public static string CredentialsDirectory => Path.Combine(ApplicationDirectory, "Credentials");

        /// <summary>
        /// Pfad zum Log-Ordner (für zukünftige Nutzung)
        /// </summary>
        public static string LogDirectory => Path.Combine(ApplicationDirectory, "Log");

        /// <summary>
        /// Pfad zum Lagerbewegung-Ordner (für zukünftige Nutzung)
        /// </summary>
        public static string LagerbewegungDirectory => Path.Combine(ApplicationDirectory, "Lagerbewegung");

        /// <summary>
        /// Vollständiger Pfad zur SQLite-Datenbankdatei
        /// </summary>
        public static string DatabaseFilePath => Path.Combine(DatabaseDirectory, "Lager.db");

        /// <summary>
        /// Erstellt alle benötigten Anwendungsordner, falls sie nicht existieren
        /// Diese Methode sollte beim Programmstart aufgerufen werden
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            try
            {
                // Erstelle alle benötigten Unterordner
                Directory.CreateDirectory(DatabaseDirectory);
                Directory.CreateDirectory(CredentialsDirectory);
                Directory.CreateDirectory(LogDirectory);
                Directory.CreateDirectory(LagerbewegungDirectory);
            }
            catch (Exception ex)
            {
                // Fehler beim Erstellen der Ordner - diese Exception sollte behandelt werden
                throw new InvalidOperationException($"Fehler beim Erstellen der Anwendungsordner: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Überprüft, ob alle erforderlichen Ordner existieren
        /// Nützlich für Debugging und Fehlerdiagnose
        /// </summary>
        /// <returns>True, wenn alle Ordner existieren</returns>
        public static bool AllDirectoriesExist()
        {
            return Directory.Exists(DatabaseDirectory) &&
                   Directory.Exists(CredentialsDirectory) &&
                   Directory.Exists(LogDirectory) &&
                   Directory.Exists(LagerbewegungDirectory);
        }

        /// <summary>
        /// Gibt Informationen über die aktuellen Pfade für Debugging zurück
        /// </summary>
        /// <returns>String mit allen wichtigen Pfadinformationen</returns>
        public static string GetPathInformation()
        {
            return $"Anwendungsverzeichnis: {ApplicationDirectory}\n" +
                   $"Datenbank-Ordner: {DatabaseDirectory}\n" +
                   $"Credentials-Ordner: {CredentialsDirectory}\n" +
                   $"Log-Ordner: {LogDirectory}\n" +
                   $"Lagerbewegung-Ordner: {LagerbewegungDirectory}\n" +
                   $"Datenbankdatei: {DatabaseFilePath}\n" +
                   $"Alle Ordner existieren: {AllDirectoriesExist()}";
        }
    }
}