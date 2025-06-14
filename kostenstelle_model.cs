using System.ComponentModel.DataAnnotations;

namespace LAGA
{
    /// <summary>
    /// Datenmodell für eine Kostenstelle mit Bezeichnung
    /// </summary>
    public class Kostenstelle
    {
        /// <summary>
        /// Eindeutige ID der Kostenstelle (Primärschlüssel, Auto-Inkrement)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Bezeichnung der Kostenstelle (Pflichtfeld)
        /// </summary>
        [Required]
        public string Bezeichnung { get; set; } = string.Empty;
    }
}