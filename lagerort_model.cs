using System.ComponentModel.DataAnnotations;

namespace LAGA
{
    /// <summary>
    /// Datenmodell für einen Lagerort mit Bezeichnung
    /// </summary>
    public class Lagerort
    {
        /// <summary>
        /// Eindeutige ID des Lagerortes (Primärschlüssel, Auto-Inkrement)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Bezeichnung des Lagerortes (Pflichtfeld)
        /// </summary>
        [Required]
        public string Bezeichnung { get; set; } = string.Empty;
    }
}