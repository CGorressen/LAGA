using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Entity Framework Datenbank-Context für die LAGA SQLite-Datenbank
    /// </summary>
    public class LagerContext : DbContext
    {
        /// <summary>
        /// Tabelle für Lieferquellen in der Datenbank
        /// </summary>
        public DbSet<Lieferquelle> Lieferquellen { get; set; }

        /// <summary>
        /// Tabelle für Kostenstellen in der Datenbank
        /// </summary>
        public DbSet<Kostenstelle> Kostenstellen { get; set; }

        /// <summary>
        /// Tabelle für Lagerorte in der Datenbank
        /// </summary>
        public DbSet<Lagerort> Lagerorte { get; set; }

        /// <summary>
        /// Konfiguriert die Datenbankverbindung zur SQLite-Datei "Lager.db"
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite-Datenbankdatei wird im Anwendungsverzeichnis erstellt
            optionsBuilder.UseSqlite("Data Source=Lager.db");
        }

        /// <summary>
        /// Konfiguriert die Datenbank-Modelle und Beziehungen
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguration für Lieferquelle-Tabelle
            modelBuilder.Entity<Lieferquelle>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Id).ValueGeneratedOnAdd();

                // Alle Textfelder sind erforderlich und haben maximale Längen
                entity.Property(l => l.Bezeichnung).IsRequired().HasMaxLength(200);
                entity.Property(l => l.Webseite).IsRequired().HasMaxLength(300);
                entity.Property(l => l.Email).IsRequired().HasMaxLength(200);
                entity.Property(l => l.Telefon).IsRequired().HasMaxLength(50);

                // Tabellenname in der Datenbank
                entity.ToTable("Lieferquellen");
            });

            // Konfiguration für Kostenstelle-Tabelle
            modelBuilder.Entity<Kostenstelle>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(k => k.Id);
                entity.Property(k => k.Id).ValueGeneratedOnAdd();

                // Bezeichnung ist erforderlich und hat maximale Länge
                entity.Property(k => k.Bezeichnung).IsRequired().HasMaxLength(200);

                // Tabellenname in der Datenbank
                entity.ToTable("Kostenstellen");
            });

            // Konfiguration für Lagerort-Tabelle
            modelBuilder.Entity<Lagerort>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Id).ValueGeneratedOnAdd();

                // Bezeichnung ist erforderlich und hat maximale Länge
                entity.Property(l => l.Bezeichnung).IsRequired().HasMaxLength(200);

                // Tabellenname in der Datenbank
                entity.ToTable("Lagerorte");
            });
        }
    }
}