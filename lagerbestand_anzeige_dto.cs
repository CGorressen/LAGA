namespace LAGA
{
    /// <summary>
    /// Data Transfer Object für die Lagerbestand-Anzeige
    /// Kombiniert Artikel-Daten mit dynamisch berechnetem Bestand
    /// </summary>
    public class LagerbestandAnzeigeDto
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
        /// Aktueller Bestand (dynamisch berechnet über ArtikelEinheiten)
        /// </summary>
        public int Bestand { get; set; }

        /// <summary>
        /// Bezeichnung des Lagerortes
        /// </summary>
        public string LagerortBezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Text-Darstellung der Einheit (Einzelteil oder Karton mit Einzelteilen)
        /// </summary>
        public string EinheitText { get; set; } = string.Empty;

        /// <summary>
        /// Mindestbestand für Warnungen
        /// </summary>
        public int Mindestbestand { get; set; }

        /// <summary>
        /// Maximalbestand für Übersicht
        /// </summary>
        public int Maximalbestand { get; set; }

        /// <summary>
        /// Gibt an, ob der aktuelle Bestand unter dem Mindestbestand liegt
        /// Wird für visuelle Warnungen verwendet
        /// </summary>
        public bool IstBestandNiedrig => Bestand < Mindestbestand;

        /// <summary>
        /// Ursprünglicher Artikel für Wareneingang-Funktionen
        /// </summary>
        public Artikel OriginalArtikel { get; set; } = new Artikel();
    }
}