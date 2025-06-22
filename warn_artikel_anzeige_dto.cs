namespace LAGA
{
    /// <summary>
    /// Data Transfer Object für die WarnArtikel-Anzeige
    /// Kombiniert Artikel-Daten mit dynamisch berechnetem Bestand und Warninformationen
    /// </summary>
    public class WarnArtikelAnzeigeDto
    {
        /// <summary>
        /// ID des Artikels (für interne Verwendung)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Datum der letzten Warnung formatiert als "dd.MM.yyyy"
        /// Entspricht dem "Datum" in der Dokumentation
        /// </summary>
        public string Datum { get; set; } = string.Empty;

        /// <summary>
        /// Bezeichnung des Artikels
        /// </summary>
        public string Artikelbezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Aktueller Bestand (dynamisch berechnet über ArtikelEinheiten)
        /// </summary>
        public int Bestand { get; set; }

        /// <summary>
        /// Mindestbestand des Artikels
        /// </summary>
        public int Mindestbestand { get; set; }

        /// <summary>
        /// Status der Benachrichtigung als formatierter Text
        /// Grün: Datum wenn erfolgreich versendet
        /// Rot: "Nicht versendet" wenn fehlgeschlagen
        /// </summary>
        public string Benachrichtigung { get; set; } = string.Empty;

        /// <summary>
        /// Theoretischer Liefertermin (LetzteWarnung + Lieferzeit)
        /// Formatiert als "dd.MM.yyyy"
        /// </summary>
        public string Liefertermin { get; set; } = string.Empty;

        /// <summary>
        /// Ursprüngliches LetzteWarnungVersendet für Berechnungen
        /// </summary>
        public DateTime? LetzteWarnungVersendet { get; set; }

        /// <summary>
        /// Lieferzeit in Tagen für Liefertermin-Berechnung
        /// </summary>
        public int Lieferzeit { get; set; }

        /// <summary>
        /// Gibt an, ob der Bestand kritisch niedrig ist (unter Mindestbestand)
        /// Für rote Schriftfarbe in der UI
        /// </summary>
        public bool IstBestandKritisch => Bestand < Mindestbestand;

        /// <summary>
        /// Gibt an, ob der Bestand eine Warnung darstellt (gleich Mindestbestand)
        /// Für orange Schriftfarbe in der UI
        /// </summary>
        public bool IstBestandWarnung => Bestand == Mindestbestand;

        /// <summary>
        /// Gibt an, ob die Benachrichtigung erfolgreich versendet wurde
        /// Für grüne/rote Schriftfarbe der Benachrichtigung-Spalte
        /// </summary>
        public bool IstBenachrichtigungErfolgreich { get; set; }

        /// <summary>
        /// Ursprünglicher Artikel für weitere Funktionen
        /// </summary>
        public Artikel OriginalArtikel { get; set; } = new Artikel();
    }
}