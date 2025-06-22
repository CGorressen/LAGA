using System.ComponentModel.DataAnnotations;

namespace LAGA
{
    /// <summary>
    /// Datenmodell für einen E-Mail-Empfänger im Warnsystem
    /// Speichert die E-Mail-Adressen für Test-E-Mails und später Warn-E-Mails
    /// </summary>
    public class Empfaenger
    {
        /// <summary>
        /// Eindeutige ID des Empfängers (Primärschlüssel, Auto-Inkrement)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// E-Mail-Adresse des Empfängers (Pflichtfeld, muss eindeutig sein)
        /// </summary>
        [Required]
        public string Email { get; set; } = string.Empty;
    }
}