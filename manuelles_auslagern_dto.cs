namespace LAGA
{
    /// <summary>
    /// Data Transfer Object für die manuelle Auslagerung von Barcodes
    /// EXAKT identisch mit BarcodeAnzeigeDto, nur ohne automatische Selektion der neuesten Barcodes
    /// </summary>
    public class ManuellesAuslagernDto
    {
        /// <summary>
        /// Gibt an, ob dieser Barcode für die Auslagerung ausgewählt ist
        /// Standardmäßig FALSE (keine automatische Selektion wie bei BarcodeAnzeigeDto)
        /// </summary>
        public bool IstAusgewaehlt { get; set; }

        /// <summary>
        /// Erstellungsdatum formatiert als "dd.MM.yyyy | HH:mm"
        /// Identisch mit BarcodeAnzeigeDto
        /// </summary>
        public string ErstellungsDatumFormatiert { get; set; } = string.Empty;

        /// <summary>
        /// Der 10-stellige Barcode
        /// Identisch mit BarcodeAnzeigeDto
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// Originale ArtikelEinheit für Auslagerungs-Funktionen
        /// Identisch mit BarcodeAnzeigeDto
        /// </summary>
        public ArtikelEinheit OriginalEinheit { get; set; } = new ArtikelEinheit();

        /// <summary>
        /// Ursprüngliches ErstellungsDatum für Vergleiche und Sortierung
        /// Identisch mit BarcodeAnzeigeDto
        /// </summary>
        public DateTime ErstellungsDatum { get; set; }
    }
}