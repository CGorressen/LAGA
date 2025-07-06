using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// UserControl zur Anzeige aller WarnArtikel (Artikel mit aktivem Warnsystem)
    /// Zeigt eine Übersicht aller Artikel an, die den Mindestbestand erreicht haben
    /// NEU: Erweitert um E-Mail Retry-Funktionalität für nicht versendete Warnungen
    /// </summary>
    public partial class WarnArtikelAnzeigen : UserControl
    {
        /// <summary>
        /// Observable Collection für die WarnArtikel-Anzeige-DTOs (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<WarnArtikelAnzeigeDto> _warnArtikel;

        /// <summary>
        /// CollectionView für Sortierung
        /// </summary>
        private ICollectionView _warnArtikelView;

        public WarnArtikelAnzeigen()
        {
            InitializeComponent();
            _warnArtikel = new ObservableCollection<WarnArtikelAnzeigeDto>();

            // CollectionView für Sortierung erstellen
            _warnArtikelView = CollectionViewSource.GetDefaultView(_warnArtikel);

            // Sortierung nach Datum (neueste Warnungen zuerst)
            _warnArtikelView.SortDescriptions.Add(new SortDescription("LetzteWarnungVersendet", ListSortDirection.Descending));

            // DataGrid an CollectionView binden
            dgWarnArtikel.ItemsSource = _warnArtikelView;

            // Daten beim Laden asynchron abrufen
            LoadWarnArtikelAsync();
        }

        /// <summary>
        /// Lädt alle WarnArtikel asynchron und aktualisiert die UI
        /// </summary>
        private async void LoadWarnArtikelAsync()
        {
            try
            {
                // WarnArtikel über BestandsMonitor abrufen
                var warnArtikelListe = await BestandsMonitor.GetWarnArtikelAsync();

                // ObservableCollection aktualisieren
                _warnArtikel.Clear();
                foreach (var dto in warnArtikelListe)
                {
                    _warnArtikel.Add(dto);
                }

                // Button-Status basierend auf verfügbaren Daten aktualisieren
                UpdateEmailRetryButtonStatus();

                System.Diagnostics.Debug.WriteLine($"✅ {warnArtikelListe.Count} WarnArtikel geladen");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der WarnArtikel: {ex.Message}",
                    "Datenfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Laden der WarnArtikel: {ex.Message}");
            }
        }

        /// <summary>
        /// Schließt die WarnArtikel-Ansicht und kehrt zum StartFenster zurück
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ClearMainContent();
        }

        /// <summary>
        /// Öffentliche Methode zum Aktualisieren der WarnArtikel-Anzeige
        /// Wird aufgerufen wenn sich der Warnungs-Status von Artikeln geändert hat
        /// </summary>
        public void RefreshWarnArtikel()
        {
            LoadWarnArtikelAsync();
        }

        /// <summary>
        /// Prüft alle Artikel im System und aktualisiert die Warnungs-Status
        /// Nützlich für initiale Synchronisation oder nach System-Updates
        /// </summary>
        public async void SynchronisiereWarnungen()
        {
            try
            {
                // Vollständige Prüfung aller Artikel über BestandsMonitor
                int aktivierteWarnungen = await BestandsMonitor.PruefeAlleArtikelAsync();

                // Anzeige aktualisieren
                LoadWarnArtikelAsync();

                System.Diagnostics.Debug.WriteLine($"Warnungs-Synchronisation abgeschlossen: {aktivierteWarnungen} neue Warnungen aktiviert");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Warnungs-Synchronisation: {ex.Message}",
                    "Synchronisationsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===============================================
        // NEU: E-MAIL RETRY FUNKTIONALITÄT
        // ===============================================

        /// <summary>
        /// NEU: Aktualisiert den Status des "E-Mails erneut senden" Buttons
        /// Button ist nur aktiv wenn nicht versendete Warnungen vorhanden sind
        /// </summary>
        private void UpdateEmailRetryButtonStatus()
        {
            try
            {
                // Anzahl nicht versendeter E-Mails ermitteln
                var nichtVersendeteCount = _warnArtikel.Count(w => !w.IstBenachrichtigungErfolgreich);

                // Button-Status und Text aktualisieren
                if (nichtVersendeteCount > 0)
                {
                    btnEmailsErneutSenden.IsEnabled = true;
                    btnEmailsErneutSenden.Content = nichtVersendeteCount == 1
                        ? "1 E-Mail erneut senden"
                        : $"{nichtVersendeteCount} E-Mails erneut senden";
                    btnEmailsErneutSenden.ToolTip = nichtVersendeteCount == 1
                        ? "Sendet die nicht versendete Warn-E-Mail erneut"
                        : $"Sendet alle {nichtVersendeteCount} nicht versendeten Warn-E-Mails erneut";
                }
                else
                {
                    btnEmailsErneutSenden.IsEnabled = false;
                    btnEmailsErneutSenden.Content = "Alle E-Mails versendet";
                    btnEmailsErneutSenden.ToolTip = "Alle Warn-E-Mails wurden erfolgreich versendet";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Aktualisieren des Button-Status: {ex.Message}");
            }
        }

        /// <summary>
        /// NEU: Behandelt den Klick auf "Alle E-Mails erneut senden"
        /// Sendet alle nicht versendeten Warn-E-Mails erneut und aktualisiert den Status
        /// </summary>
        private async void BtnEmailsErneutSenden_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Button während des Sendevorgangs deaktivieren
                btnEmailsErneutSenden.IsEnabled = false;
                btnEmailsErneutSenden.Content = "E-Mails werden gesendet...";

                // Alle nicht versendeten Warnungen ermitteln
                var nichtVersendeteWarnungen = _warnArtikel
                    .Where(w => !w.IstBenachrichtigungErfolgreich)
                    .ToList();

                if (nichtVersendeteWarnungen.Count == 0)
                {
                    MessageBox.Show("Alle E-Mails wurden bereits erfolgreich versendet.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Bestätigung vom Benutzer einholen
                var result = MessageBox.Show(
                    $"Möchten Sie wirklich {nichtVersendeteWarnungen.Count} Warn-E-Mail(s) erneut senden?\n\n" +
                    "Dies sendet für alle rot markierten Artikel eine neue Warn-E-Mail.",
                    "E-Mails erneut senden",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    // Benutzer hat abgebrochen
                    UpdateEmailRetryButtonStatus();
                    return;
                }

                // E-Mails senden
                int erfolgreichGesendet = 0;
                int fehlerBeiSendung = 0;
                var detailFehler = new List<string>();

                foreach (var warnung in nichtVersendeteWarnungen)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"🔄 Versuche E-Mail zu senden für: '{warnung.Artikelbezeichnung}'");

                        // KORRIGIERT: Direkt E-Mail senden über GmailEmailService
                        // Lade den vollständigen Artikel mit allen Navigation Properties
                        using (var context = new LagerContext())
                        {
                            var vollstaendigerArtikel = await context.Artikel
                                .Include(a => a.Lieferant)
                                .Include(a => a.Hersteller)
                                .Include(a => a.Kostenstelle)
                                .Include(a => a.Lagerort)
                                .FirstOrDefaultAsync(a => a.Id == warnung.Id);

                            if (vollstaendigerArtikel != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"📄 Artikel geladen: '{vollstaendigerArtikel.Bezeichnung}'");

                                // Prüfe ob Empfänger existieren
                                var empfaengerAnzahl = await context.Empfaenger.CountAsync();
                                System.Diagnostics.Debug.WriteLine($"👥 Anzahl Empfänger in Datenbank: {empfaengerAnzahl}");

                                if (empfaengerAnzahl == 0)
                                {
                                    detailFehler.Add($"'{warnung.Artikelbezeichnung}': Keine E-Mail-Empfänger konfiguriert");
                                    fehlerBeiSendung++;
                                    continue;
                                }

                                // E-Mail FORCE-senden - umgeht die IstWarnungAktiv Prüfung
                                System.Diagnostics.Debug.WriteLine($"📧 Sende E-Mail DIREKT (Retry-Modus)...");

                                // Temporär IstWarnungAktiv auf false setzen um E-Mail-Versand zu erzwingen
                                bool originalWarnungAktiv = vollstaendigerArtikel.IstWarnungAktiv;
                                vollstaendigerArtikel.IstWarnungAktiv = false;

                                bool emailErfolg = await GmailEmailService.SendeWarnEmailAsync(vollstaendigerArtikel, warnung.Bestand);

                                // IstWarnungAktiv wieder auf ursprünglichen Wert setzen
                                vollstaendigerArtikel.IstWarnungAktiv = originalWarnungAktiv;

                                System.Diagnostics.Debug.WriteLine($"📧 E-Mail-Versand (FORCE) Ergebnis: {emailErfolg}");

                                if (emailErfolg)
                                {
                                    // Datenbank-Status auf erfolgreich aktualisieren
                                    vollstaendigerArtikel.LetzteWarnungVersendet = DateTime.Now;
                                    await context.SaveChangesAsync();

                                    erfolgreichGesendet++;
                                    System.Diagnostics.Debug.WriteLine($"✅ E-Mail für '{warnung.Artikelbezeichnung}' erfolgreich versendet und DB aktualisiert");
                                }
                                else
                                {
                                    detailFehler.Add($"'{warnung.Artikelbezeichnung}': Gmail-Versand fehlgeschlagen");
                                    fehlerBeiSendung++;
                                    System.Diagnostics.Debug.WriteLine($"❌ Gmail-Service meldete Fehler für '{warnung.Artikelbezeichnung}'");
                                }
                            }
                            else
                            {
                                detailFehler.Add($"'{warnung.Artikelbezeichnung}': Artikel nicht in Datenbank gefunden");
                                fehlerBeiSendung++;
                                System.Diagnostics.Debug.WriteLine($"❌ Artikel '{warnung.Artikelbezeichnung}' nicht in Datenbank gefunden");
                            }
                        }

                        // Kurze Pause zwischen E-Mails um Server nicht zu überlasten
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        detailFehler.Add($"'{warnung.Artikelbezeichnung}': {ex.Message}");
                        fehlerBeiSendung++;
                        System.Diagnostics.Debug.WriteLine($"❌ Ausnahme beim Senden der E-Mail für '{warnung.Artikelbezeichnung}': {ex.Message}");
                    }
                }

                // Anzeige aktualisieren
                LoadWarnArtikelAsync();

                // Detaillierte Erfolgsmeldung anzeigen
                string nachricht = $"E-Mail-Versand abgeschlossen:\n\n" +
                                  $"✅ Erfolgreich gesendet: {erfolgreichGesendet}\n" +
                                  $"❌ Fehler beim Senden: {fehlerBeiSendung}";

                if (detailFehler.Count > 0)
                {
                    nachricht += "\n\nDetaillierte Fehler:\n" + string.Join("\n", detailFehler);

                    if (detailFehler.Any(f => f.Contains("Keine E-Mail-Empfänger")))
                    {
                        nachricht += "\n\n💡 Tipp: Fügen Sie E-Mail-Empfänger unter 'Einstellungen → Warnsystem → Empfänger' hinzu.";
                    }

                    if (detailFehler.Any(f => f.Contains("Gmail-Versand fehlgeschlagen")))
                    {
                        nachricht += "\n\n💡 Tipp: Prüfen Sie Ihre Gmail-Credentials und Internetverbindung.";
                    }
                }

                MessageBox.Show(nachricht,
                    fehlerBeiSendung > 0 ? "E-Mail-Versand mit Fehlern" : "E-Mail-Versand erfolgreich",
                    MessageBoxButton.OK,
                    fehlerBeiSendung > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                System.Diagnostics.Debug.WriteLine($"📧 E-Mail Retry abgeschlossen: {erfolgreichGesendet} erfolgreich, {fehlerBeiSendung} Fehler");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unerwarteter Fehler beim E-Mail-Versand: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Unerwarteter Fehler beim E-Mail Retry: {ex.Message}");
            }
            finally
            {
                // Button-Status wiederherstellen
                UpdateEmailRetryButtonStatus();
            }
        }
    }
}