using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Zentraler Service für die Überwachung von Lagerbeständen und automatische Warnung bei Mindestbestand
    /// Wird bei jeder Lagerbewegung (Ein-/Auslagerung) aufgerufen
    /// FINALE LÖSUNG: Trennt sauber zwischen Warnung-Erstellung und E-Mail-Erfolg
    /// </summary>
    public static class BestandsMonitor
    {
        /// <summary>
        /// Prüft den Bestand eines Artikels nach einer Lagerbewegung und sendet ggf. Warnungen
        /// Diese Methode soll nach jeder Ein- oder Auslagerung aufgerufen werden
        /// </summary>
        /// <param name="artikelId">ID des Artikels, dessen Bestand geprüft werden soll</param>
        /// <returns>True wenn Prüfung erfolgreich durchgeführt wurde</returns>
        public static async Task<bool> PruefeBestandNachAenderungAsync(int artikelId)
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Artikel mit allen Navigation Properties laden
                    var artikel = await context.Artikel
                        .Include(a => a.Lieferant)
                        .Include(a => a.Hersteller)
                        .Include(a => a.Kostenstelle)
                        .Include(a => a.Lagerort)
                        .FirstOrDefaultAsync(a => a.Id == artikelId);

                    if (artikel == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Artikel mit ID {artikelId} nicht gefunden");
                        return false;
                    }

                    // Aktuellen Bestand berechnen (Anzahl ArtikelEinheiten)
                    int aktuellerBestand = await context.ArtikelEinheiten
                        .CountAsync(ae => ae.ArtikelId == artikelId);

                    // Bestandsprüfung durchführen
                    await PruefeUndBehandleBestandAsync(context, artikel, aktuellerBestand);

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Bestandsprüfung für Artikel {artikelId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Führt die eigentliche Bestandsprüfung durch und behandelt Warnungen entsprechend
        /// </summary>
        /// <param name="context">Datenbank-Context</param>
        /// <param name="artikel">Der zu prüfende Artikel</param>
        /// <param name="aktuellerBestand">Der aktuell berechnete Bestand</param>
        private static async Task PruefeUndBehandleBestandAsync(LagerContext context, Artikel artikel, int aktuellerBestand)
        {
            // Situation 1: Bestand ist kleiner oder gleich Mindestbestand
            if (aktuellerBestand <= artikel.Mindestbestand)
            {
                await BehandleNiedrigenBestandAsync(context, artikel, aktuellerBestand);
            }
            // Situation 2: Bestand ist größer als Mindestbestand
            else
            {
                await BehandleAusreichendenBestandAsync(context, artikel);
            }
        }

        /// <summary>
        /// Behandelt Artikel mit niedrigem Bestand (kleiner/gleich Mindestbestand)
        /// FINALE LÖSUNG: Trennt Warnung-Erstellung von E-Mail-Erfolg
        /// </summary>
        /// <param name="context">Datenbank-Context</param>
        /// <param name="artikel">Der betroffene Artikel</param>
        /// <param name="aktuellerBestand">Der aktuelle Bestand</param>
        private static async Task BehandleNiedrigenBestandAsync(LagerContext context, Artikel artikel, int aktuellerBestand)
        {
            // Prüfen ob bereits eine Warnung aktiv ist
            if (!artikel.IstWarnungAktiv)
            {
                // Warnung erstellen (IMMER, unabhängig vom E-Mail-Erfolg)
                DateTime warnungsDatum = DateTime.Now;
                artikel.IstWarnungAktiv = true;
                artikel.WarnungErstelltAm = warnungsDatum;

                // E-Mail senden versuchen
                bool emailErfolgreich = await SendeWarnEmailAsync(artikel, aktuellerBestand);

                // E-Mail-Status separat verfolgen
                if (emailErfolgreich)
                {
                    // E-Mail erfolgreich versendet
                    artikel.LetzteWarnungVersendet = warnungsDatum;
                    System.Diagnostics.Debug.WriteLine($"✅ Warnung für '{artikel.Bezeichnung}' erstellt und E-Mail erfolgreich versendet");
                }
                else
                {
                    // E-Mail fehlgeschlagen - LetzteWarnungVersendet bleibt null
                    artikel.LetzteWarnungVersendet = null;
                    System.Diagnostics.Debug.WriteLine($"⚠️ Warnung für '{artikel.Bezeichnung}' erstellt am {warnungsDatum:dd.MM.yyyy} - E-Mail konnte nicht versendet werden");
                }

                // Änderungen in Datenbank speichern
                await context.SaveChangesAsync();
            }
            else
            {
                // Warnung bereits aktiv - nur Debug-Info, keine Änderungen
                System.Diagnostics.Debug.WriteLine($"ℹ️ Warnung für '{artikel.Bezeichnung}' bereits aktiv - keine neue E-Mail");
            }
        }

        /// <summary>
        /// Behandelt Artikel mit ausreichendem Bestand (über Mindestbestand)
        /// </summary>
        /// <param name="context">Datenbank-Context</param>
        /// <param name="artikel">Der betroffene Artikel</param>
        private static async Task BehandleAusreichendenBestandAsync(LagerContext context, Artikel artikel)
        {
            // Prüfen ob eine Warnung aktiv war und diese jetzt deaktiviert werden kann
            if (artikel.IstWarnungAktiv)
            {
                // Warnung deaktivieren und alle Warnung-Felder zurücksetzen
                artikel.IstWarnungAktiv = false;
                artikel.WarnungErstelltAm = null;
                artikel.LetzteWarnungVersendet = null;

                System.Diagnostics.Debug.WriteLine($"✅ Warnung für '{artikel.Bezeichnung}' deaktiviert - Bestand wieder ausreichend");

                // Änderungen in Datenbank speichern
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Sendet eine Warn-E-Mail für einen Artikel mit niedrigem Bestand
        /// </summary>
        /// <param name="artikel">Der betroffene Artikel</param>
        /// <param name="aktuellerBestand">Der aktuelle Bestand</param>
        /// <returns>True wenn E-Mail erfolgreich versendet wurde</returns>
        private static async Task<bool> SendeWarnEmailAsync(Artikel artikel, int aktuellerBestand)
        {
            try
            {
                // Warn-E-Mail über GmailEmailService senden
                bool erfolg = await GmailEmailService.SendeWarnEmailAsync(artikel, aktuellerBestand);

                return erfolg;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Senden der Warn-E-Mail für '{artikel.Bezeichnung}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Holt alle Artikel mit aktiven Warnungen für die WarnArtikel-Anzeige
        /// FINALE LÖSUNG: Verwendet separate Felder für perfekte Anzeige
        /// </summary>
        /// <returns>Liste der WarnArtikelAnzeigeDto für die UI</returns>
        public static async Task<List<WarnArtikelAnzeigeDto>> GetWarnArtikelAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle Artikel mit aktiven Warnungen laden
                    var warnArtikel = await context.Artikel
                        .Where(a => a.IstWarnungAktiv)
                        .OrderByDescending(a => a.WarnungErstelltAm)
                        .ToListAsync();

                    var warnArtikelDtos = new List<WarnArtikelAnzeigeDto>();

                    foreach (var artikel in warnArtikel)
                    {
                        // Aktuellen Bestand berechnen
                        int aktuellerBestand = await context.ArtikelEinheiten
                            .CountAsync(ae => ae.ArtikelId == artikel.Id);

                        // DTO erstellen
                        var dto = new WarnArtikelAnzeigeDto
                        {
                            Id = artikel.Id,
                            Artikelbezeichnung = artikel.Bezeichnung,
                            Bestand = aktuellerBestand,
                            Mindestbestand = artikel.Mindestbestand,
                            Lieferzeit = artikel.Lieferzeit,
                            LetzteWarnungVersendet = artikel.LetzteWarnungVersendet,
                            OriginalArtikel = artikel
                        };

                        // DATUM-SPALTE: Basiert auf WarnungErstelltAm (immer verfügbar)
                        if (artikel.WarnungErstelltAm.HasValue)
                        {
                            dto.Datum = artikel.WarnungErstelltAm.Value.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            dto.Datum = "Unbekannt"; // Sollte nie passieren bei aktiven Warnungen
                        }

                        // LIEFERTERMIN: Basiert auf WarnungErstelltAm + Lieferzeit
                        if (artikel.WarnungErstelltAm.HasValue)
                        {
                            var liefertermin = artikel.WarnungErstelltAm.Value.AddDays(artikel.Lieferzeit);
                            dto.Liefertermin = liefertermin.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            dto.Liefertermin = "Unbekannt";
                        }

                        // BENACHRICHTIGUNG-SPALTE: Basiert auf LetzteWarnungVersendet (E-Mail-Erfolg)
                        if (artikel.LetzteWarnungVersendet.HasValue)
                        {
                            // E-Mail wurde erfolgreich versendet - grünes Datum
                            dto.Benachrichtigung = artikel.LetzteWarnungVersendet.Value.ToString("dd.MM.yyyy");
                            dto.IstBenachrichtigungErfolgreich = true;
                        }
                        else
                        {
                            // E-Mail konnte nicht versendet werden - rote "Nicht versendet" Meldung
                            dto.Benachrichtigung = "Nicht versendet";
                            dto.IstBenachrichtigungErfolgreich = false;
                        }

                        warnArtikelDtos.Add(dto);
                    }

                    return warnArtikelDtos;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Laden der WarnArtikel: {ex.Message}");
                return new List<WarnArtikelAnzeigeDto>();
            }
        }

        /// <summary>
        /// Prüft alle Artikel im System und identifiziert die mit aktiven Warnungen
        /// Kann für initiale Synchronisation oder Debugging verwendet werden
        /// </summary>
        /// <returns>Anzahl der Artikel mit aktiven Warnungen</returns>
        public static async Task<int> PruefeAlleArtikelAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    var alleArtikel = await context.Artikel.ToListAsync();

                    int warnungenAktiviert = 0;

                    foreach (var artikel in alleArtikel)
                    {
                        // Aktuellen Bestand berechnen
                        int bestand = await context.ArtikelEinheiten
                            .CountAsync(ae => ae.ArtikelId == artikel.Id);

                        // Warnung prüfen ohne E-Mail zu senden (nur Status-Update)
                        if (bestand <= artikel.Mindestbestand && !artikel.IstWarnungAktiv)
                        {
                            artikel.IstWarnungAktiv = true;
                            artikel.WarnungErstelltAm = DateTime.Now;
                            // LetzteWarnungVersendet bleibt null (keine E-Mail versucht)
                            warnungenAktiviert++;
                        }
                        else if (bestand > artikel.Mindestbestand && artikel.IstWarnungAktiv)
                        {
                            artikel.IstWarnungAktiv = false;
                            artikel.WarnungErstelltAm = null;
                            artikel.LetzteWarnungVersendet = null;
                        }
                    }

                    await context.SaveChangesAsync();
                    return warnungenAktiviert;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler bei der Artikel-Vollprüfung: {ex.Message}");
                return 0;
            }
        }
    }
}