namespace LAGA
{
    /// <summary>
    /// Data Transfer Object für die Artikel-Anzeige mit Join-Daten
    /// Enthält alle Bezeichnungen statt nur IDs für bessere Benutzerfreundlichkeit
    /// </summary>
    public class ArtikelAnzeigeDto
    {
        /// <summary>
        /// ID des Artikels (für interne Verwendung)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bezeichnung des Artikels
        /// </summary>
        public string Bezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Bezeichnung der Kostenstelle (statt KostenstelleId)
        /// </summary>
        public string KostenstelleBezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Bezeichnung des Lagerortes (statt LagerortId)
        /// </summary>
        public string LagerortBezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Bezeichnung des Lieferanten (statt LieferantId)
        /// </summary>
        public string LieferantBezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Bezeichnung des Herstellers (statt HerstellerId)
        /// </summary>
        public string HerstellerBezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Ursprünglicher Artikel für Bearbeitung/Löschen
        /// </summary>
        public Artikel OriginalArtikel { get; set; } = new Artikel();
    }
}