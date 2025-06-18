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
        /// Tabelle für Artikel in der Datenbank
        /// </summary>
        public DbSet<Artikel> Artikel { get; set; }

        /// <summary>
        /// Tabelle für ArtikelEinheiten in der Datenbank
        /// Jeder Eintrag repräsentiert eine physische Einheit eines Artikels mit eindeutigem Barcode
        /// </summary>
        public DbSet<ArtikelEinheit> ArtikelEinheiten { get; set; }

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

                // Eindeutige Lieferquellen-Bezeichnung (UNIQUE Constraint)
                entity.HasIndex(l => l.Bezeichnung).IsUnique();

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

                // Eindeutige Kostenstellen-Bezeichnung (UNIQUE Constraint)
                entity.HasIndex(k => k.Bezeichnung).IsUnique();

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

                // Eindeutige Lagerort-Bezeichnung (UNIQUE Constraint)
                entity.HasIndex(l => l.Bezeichnung).IsUnique();

                // Tabellenname in der Datenbank
                entity.ToTable("Lagerorte");
            });

            // Konfiguration für Artikel-Tabelle
            modelBuilder.Entity<Artikel>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).ValueGeneratedOnAdd();

                // Textfelder mit Längen-Begrenzungen
                entity.Property(a => a.Bezeichnung).IsRequired().HasMaxLength(200);
                entity.Property(a => a.ExterneArtikelIdLieferant).IsRequired().HasMaxLength(100);
                entity.Property(a => a.ExterneArtikelIdHersteller).IsRequired().HasMaxLength(100);

                // Eindeutige Artikelbezeichnung (UNIQUE Constraint)
                entity.HasIndex(a => a.Bezeichnung).IsUnique();

                // Fremdschlüssel-Beziehungen mit ON DELETE RESTRICT
                entity.HasOne(a => a.Lieferant)
                    .WithMany()
                    .HasForeignKey(a => a.LieferantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Hersteller)
                    .WithMany()
                    .HasForeignKey(a => a.HerstellerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Kostenstelle)
                    .WithMany()
                    .HasForeignKey(a => a.KostenstelleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Lagerort)
                    .WithMany()
                    .HasForeignKey(a => a.LagerortId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Tabellenname in der Datenbank
                entity.ToTable("Artikel");
            });

            // Konfiguration für ArtikelEinheit-Tabelle
            modelBuilder.Entity<ArtikelEinheit>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(ae => ae.Id);
                entity.Property(ae => ae.Id).ValueGeneratedOnAdd();

                // Barcode ist erforderlich, hat maximale Länge und muss eindeutig sein
                entity.Property(ae => ae.Barcode).IsRequired().HasMaxLength(10);
                entity.HasIndex(ae => ae.Barcode).IsUnique();

                // ErstellungsDatum ist erforderlich
                entity.Property(ae => ae.ErstellungsDatum).IsRequired();

                // Fremdschlüssel-Beziehung zu Artikel mit ON DELETE RESTRICT
                // Verhindert das Löschen von Artikeln, solange noch Einheiten im Lager sind
                entity.HasOne(ae => ae.Artikel)
                    .WithMany()
                    .HasForeignKey(ae => ae.ArtikelId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Tabellenname in der Datenbank
                entity.ToTable("ArtikelEinheiten");
            });
        }
    }
}