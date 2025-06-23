using System.ComponentModel.DataAnnotations;

namespace LAGA
{
    /// <summary>
    /// Datenmodell für einen Artikel mit allen erforderlichen Eigenschaften und Fremdschlüssel-Beziehungen
    /// Erweitert um Warnsystem-Funktionalität mit separaten Feldern für Warnung-Erstellung und E-Mail-Erfolg
    /// </summary>
    public class Artikel
    {
        /// <summary>
        /// Eindeutige ID des Artikels (Primärschlüssel, Auto-Inkrement)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Bezeichnung des Artikels (Pflichtfeld, muss eindeutig sein)
        /// </summary>
        [Required]
        public string Bezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Fremdschlüssel zur Lieferquellen-Tabelle (Lieferant)
        /// </summary>
        [Required]
        public int LieferantId { get; set; }

        /// <summary>
        /// Fremdschlüssel zur Lieferquellen-Tabelle (Hersteller)
        /// </summary>
        [Required]
        public int HerstellerId { get; set; }

        /// <summary>
        /// Lieferzeit in Tagen (1-10)
        /// </summary>
        [Required]
        public int Lieferzeit { get; set; }

        /// <summary>
        /// Externe Artikel-ID beim Lieferanten
        /// </summary>
        [Required]
        public string ExterneArtikelIdLieferant { get; set; } = string.Empty;

        /// <summary>
        /// Externe Artikel-ID beim Hersteller
        /// </summary>
        [Required]
        public string ExterneArtikelIdHersteller { get; set; } = string.Empty;

        /// <summary>
        /// Fremdschlüssel zur Kostenstellen-Tabelle
        /// </summary>
        [Required]
        public int KostenstelleId { get; set; }

        /// <summary>
        /// Fremdschlüssel zur Lagerorte-Tabelle
        /// </summary>
        [Required]
        public int LagerortId { get; set; }

        /// <summary>
        /// Einheit: true = Einzelteil, false = Karton mit mehreren Einzelteilen
        /// </summary>
        [Required]
        public bool IstEinzelteil { get; set; }

        /// <summary>
        /// Mindestbestand (muss >= 0 und <= Maximalbestand sein)
        /// </summary>
        [Required]
        public int Mindestbestand { get; set; }

        /// <summary>
        /// Maximalbestand (muss >= Mindestbestand sein)
        /// </summary>
        [Required]
        public int Maximalbestand { get; set; }

        // ===== WARNSYSTEM-FELDER =====

        /// <summary>
        /// Datum und Uhrzeit als die Warnung für diesen Artikel erstellt wurde
        /// NULL = Keine aktive Warnung
        /// Wird immer gesetzt wenn der Mindestbestand erreicht wird (unabhängig vom E-Mail-Erfolg)
        /// Für Datum-Spalte und Liefertermin-Berechnung in der UI
        /// </summary>
        public DateTime? WarnungErstelltAm { get; set; }

        /// <summary>
        /// Datum und Uhrzeit der letzten ERFOLGREICH versendeten Warnung für diesen Artikel
        /// NULL = E-Mail wurde noch nie erfolgreich versendet ODER beim letzten Versuch ist sie fehlgeschlagen
        /// Nur bei erfolgreichem E-Mail-Versand gesetzt
        /// Für Benachrichtigung-Spalte in der UI (grün/rot)
        /// </summary>
        public DateTime? LetzteWarnungVersendet { get; set; }

        /// <summary>
        /// Gibt an, ob für diesen Artikel aktuell eine Warnung aktiv ist
        /// True = Artikel hat Mindestbestand erreicht und Warnung ist aktiv
        /// False = Artikel ist über Mindestbestand oder Warnung wurde zurückgesetzt
        /// </summary>
        public bool IstWarnungAktiv { get; set; }

        // Navigation Properties für Entity Framework
        /// <summary>
        /// Navigation Property zum Lieferanten
        /// </summary>
        public virtual Lieferquelle? Lieferant { get; set; }

        /// <summary>
        /// Navigation Property zum Hersteller
        /// </summary>
        public virtual Lieferquelle? Hersteller { get; set; }

        /// <summary>
        /// Navigation Property zur Kostenstelle
        /// </summary>
        public virtual Kostenstelle? Kostenstelle { get; set; }

        /// <summary>
        /// Navigation Property zum Lagerort
        /// </summary>
        public virtual Lagerort? Lagerort { get; set; }
    }
}