namespace LAGA
{
    /// <summary>
    /// Data Transfer Object für die Barcode-Anzeige mit Selektion
    /// Ermöglicht die Anzeige und Auswahl von Barcodes zum Neudruck
    /// </summary>
    public class BarcodeAnzeigeDto
    {
        /// <summary>
        /// Gibt an, ob dieser Barcode für den Neudruck ausgewählt ist
        /// </summary>
        public bool IstAusgewaehlt { get; set; }

        /// <summary>
        /// Erstellungsdatum formatiert als "dd.MM.yyyy | HH:mm"
        /// </summary>
        public string ErstellungsDatumFormatiert { get; set; } = string.Empty;

        /// <summary>
        /// Der 10-stellige Barcode
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// Originale ArtikelEinheit für Druck-Funktionen
        /// </summary>
        public ArtikelEinheit OriginalEinheit { get; set; } = new ArtikelEinheit();

        /// <summary>
        /// Ursprüngliches ErstellungsDatum für Vergleiche und Sortierung
        /// </summary>
        public DateTime ErstellungsDatum { get; set; }
    }
}