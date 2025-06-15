using System.ComponentModel.DataAnnotations;

namespace LAGA
{
    /// <summary>
    /// Datenmodell für eine physische Einheit eines Artikels mit eindeutigem Barcode
    /// Jede ArtikelEinheit repräsentiert ein einzelnes, physisches Objekt im Lager
    /// </summary>
    public class ArtikelEinheit
    {
        /// <summary>
        /// Eindeutige ID der ArtikelEinheit (Primärschlüssel, Auto-Inkrement)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Fremdschlüssel zur Artikel-Tabelle
        /// Verknüpft diese Einheit mit dem entsprechenden Artikel
        /// </summary>
        [Required]
        public int ArtikelId { get; set; }

        /// <summary>
        /// Eindeutiger 10-stelliger Barcode für diese spezifische Einheit
        /// Wird für Etiketten und Identifikation verwendet
        /// </summary>
        [Required]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// Navigation Property zum verknüpften Artikel
        /// Ermöglicht den Zugriff auf Artikel-Informationen über Entity Framework
        /// </summary>
        public virtual Artikel? Artikel { get; set; }
    }
}