using System.ComponentModel.DataAnnotations;

namespace LAGA
{
    /// <summary>
    /// Datenmodell für eine Lieferquelle mit allen erforderlichen Eigenschaften
    /// </summary>
    public class Lieferquelle
    {
        /// <summary>
        /// Eindeutige ID der Lieferquelle (Primärschlüssel, Auto-Inkrement)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Bezeichnung/Name der Lieferquelle (Pflichtfeld)
        /// </summary>
        [Required]
        public string Bezeichnung { get; set; } = string.Empty;

        /// <summary>
        /// Webseite der Lieferquelle (Pflichtfeld)
        /// </summary>
        [Required]
        public string Webseite { get; set; } = string.Empty;

        /// <summary>
        /// E-Mail-Adresse der Lieferquelle (Pflichtfeld)
        /// </summary>
        [Required]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Telefonnummer der Lieferquelle (Pflichtfeld)
        /// </summary>
        [Required]
        public string Telefon { get; set; } = string.Empty;
    }
}