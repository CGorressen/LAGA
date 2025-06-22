using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Entity Framework Datenbank-Context für die LAGA SQLite-Datenbank
    /// Verwendet jetzt portable Pfade für die Datenbankdatei
    /// Erweitert um Warnsystem-Funktionalität
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
        /// Erweitert um Warnsystem-Felder (LetzteWarnungVersendet, IstWarnungAktiv)
        /// </summary>
        public DbSet<Artikel> Artikel { get; set; }

        /// <summary>
        /// Tabelle für ArtikelEinheiten in der Datenbank
        /// Jeder Eintrag repräsentiert eine physische Einheit eines Artikels mit eindeutigem Barcode
        /// </summary>
        public DbSet<ArtikelEinheit> ArtikelEinheiten { get; set; }

        /// <summary>
        /// Tabelle für E-Mail-Empfänger des Warnsystems
        /// Speichert die E-Mail-Adressen für Test-E-Mails und Warn-E-Mails
        /// </summary>
        public DbSet<Empfaenger> Empfaenger { get; set; }

        /// <summary>
        /// Konfiguriert die Datenbankverbindung zur SQLite-Datei im Datenbank-Ordner
        /// Verwendet jetzt PathHelper für portable Pfade
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite-Datenbankdatei wird im Datenbank-Unterordner der Anwendung erstellt
            // Verwendet PathHelper.DatabaseFilePath für den vollständigen, portablen Pfad
            optionsBuilder.UseSqlite($"Data Source={PathHelper.DatabaseFilePath}");
        }

        /// <summary>
        /// Konfiguriert die Datenbank-Modelle und Beziehungen
        /// Erweitert um Warnsystem-Konfiguration
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

            // Konfiguration für Artikel-Tabelle (erweitert um Warnsystem-Felder)
            modelBuilder.Entity<Artikel>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).ValueGeneratedOnAdd();

                // Alle Textfelder sind erforderlich und haben maximale Längen
                entity.Property(a => a.Bezeichnung).IsRequired().HasMaxLength(200);
                entity.Property(a => a.ExterneArtikelIdLieferant).IsRequired().HasMaxLength(100);
                entity.Property(a => a.ExterneArtikelIdHersteller).IsRequired().HasMaxLength(100);

                // Numerische Felder mit Validierung
                entity.Property(a => a.Lieferzeit).IsRequired();
                entity.Property(a => a.Mindestbestand).IsRequired();
                entity.Property(a => a.Maximalbestand).IsRequired();
                entity.Property(a => a.IstEinzelteil).IsRequired();

                // Warnsystem-Felder (neu hinzugefügt)
                entity.Property(a => a.LetzteWarnungVersendet).IsRequired(false); // Optional (nullable)
                entity.Property(a => a.IstWarnungAktiv).IsRequired().HasDefaultValue(false); // Standard: false

                // Fremdschlüssel-Beziehungen
                entity.Property(a => a.LieferantId).IsRequired();
                entity.Property(a => a.HerstellerId).IsRequired();
                entity.Property(a => a.KostenstelleId).IsRequired();
                entity.Property(a => a.LagerortId).IsRequired();

                // Navigation Properties konfigurieren
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

                // Eindeutige Artikel-Bezeichnung (UNIQUE Constraint)
                entity.HasIndex(a => a.Bezeichnung).IsUnique();

                // Tabellenname in der Datenbank
                entity.ToTable("Artikel");
            });

            // Konfiguration für ArtikelEinheit-Tabelle
            modelBuilder.Entity<ArtikelEinheit>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(ae => ae.Id);
                entity.Property(ae => ae.Id).ValueGeneratedOnAdd();

                // Barcode ist erforderlich und hat maximale Länge
                entity.Property(ae => ae.Barcode).IsRequired().HasMaxLength(50);

                // ErstellungsDatum ist erforderlich
                entity.Property(ae => ae.ErstellungsDatum).IsRequired();

                // Fremdschlüssel zur Artikel-Tabelle
                entity.Property(ae => ae.ArtikelId).IsRequired();

                // Navigation Property konfigurieren mit ON DELETE RESTRICT
                entity.HasOne(ae => ae.Artikel)
                      .WithMany()
                      .HasForeignKey(ae => ae.ArtikelId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Eindeutiger Barcode (UNIQUE Constraint)
                entity.HasIndex(ae => ae.Barcode).IsUnique();

                // Tabellenname in der Datenbank
                entity.ToTable("ArtikelEinheiten");
            });

            // Konfiguration für Empfaenger-Tabelle
            modelBuilder.Entity<Empfaenger>(entity =>
            {
                // Primärschlüssel mit Auto-Inkrement
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // E-Mail ist erforderlich und hat maximale Länge
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);

                // Eindeutige E-Mail-Adresse (UNIQUE Constraint)
                entity.HasIndex(e => e.Email).IsUnique();

                // Tabellenname in der Datenbank
                entity.ToTable("Empfaenger");
            });
        }
    }
}