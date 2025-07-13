using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster für die Drucker-Einrichtung
    /// Ermöglicht die Auswahl eines Druckers aus allen verfügbaren Druckern (lokal und Netzwerk)
    /// Speichert die Einstellungen automatisch in einer JSON-Datei im AppData-Ordner
    /// </summary>
    public partial class DruckerEinrichten : Window
    {
        /// <summary>
        /// Gibt an ob die Einstellungen erfolgreich gespeichert wurden
        /// </summary>
        public bool EinstellungenGespeichert { get; private set; } = false;

        /// <summary>
        /// Der ausgewählte Drucker-Name
        /// </summary>
        public string? AusgewaehlterDrucker { get; private set; }

        /// <summary>
        /// Konstruktor - Initialisiert das Fenster und lädt die verfügbaren Drucker
        /// </summary>
        public DruckerEinrichten()
        {
            InitializeComponent();

            // Fenster-Eigenschaften setzen
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Verfügbare Drucker laden (asynchron)
            this.Loaded += async (sender, e) => await DruckerLadenAsync();
        }

        /// <summary>
        /// Lädt alle verfügbaren Drucker und die gespeicherten Einstellungen
        /// </summary>
        private async System.Threading.Tasks.Task DruckerLadenAsync()
        {
            try
            {
                // Status anzeigen dass Drucker geladen werden
                ZeigeStatus("Verfügbare Drucker werden geladen...", StatusTyp.Info);

                // Alle verfügbaren Drucker aus dem System holen
                var verfuegbareDrucker = DruckerEinstellungsService.VerfuegbareDruckerHolen();

                if (verfuegbareDrucker.Count == 0)
                {
                    // Keine Drucker gefunden
                    ZeigeStatus("⚠️ Keine Drucker im System gefunden. Bitte prüfen Sie die Drucker-Installation.", StatusTyp.Fehler);
                    cmbDrucker.IsEnabled = false;
                    return;
                }

                // Drucker zur ComboBox hinzufügen
                cmbDrucker.Items.Clear();
                foreach (string drucker in verfuegbareDrucker)
                {
                    cmbDrucker.Items.Add(drucker);
                }

                // Gespeicherte Einstellungen laden
                var gespeicherteEinstellungen = await DruckerEinstellungsService.EinstellungenLadenAsync();

                if (gespeicherteEinstellungen != null && !string.IsNullOrEmpty(gespeicherteEinstellungen.AusgewaehlterDrucker))
                {
                    // Prüfen ob der gespeicherte Drucker noch verfügbar ist
                    if (DruckerEinstellungsService.IstDruckerVerfuegbar(gespeicherteEinstellungen.AusgewaehlterDrucker))
                    {
                        // Gespeicherten Drucker auswählen
                        cmbDrucker.SelectedItem = gespeicherteEinstellungen.AusgewaehlterDrucker;
                        AusgewaehlterDrucker = gespeicherteEinstellungen.AusgewaehlterDrucker;

                        ZeigeStatus($"✅ Aktuell ausgewählt: {gespeicherteEinstellungen.AusgewaehlterDrucker}", StatusTyp.Erfolg);
                        btnSpeichern.IsEnabled = true;
                    }
                    else
                    {
                        // Gespeicherter Drucker ist nicht mehr verfügbar
                        ZeigeStatus($"⚠️ Der zuvor ausgewählte Drucker '{gespeicherteEinstellungen.AusgewaehlterDrucker}' ist nicht mehr verfügbar.", StatusTyp.Warnung);
                    }
                }
                else
                {
                    // Keine gespeicherten Einstellungen
                    ZeigeStatus($"ℹ️ {verfuegbareDrucker.Count} Drucker gefunden. Bitte wählen Sie einen Drucker aus.", StatusTyp.Info);
                }
            }
            catch (Exception ex)
            {
                ZeigeStatus($"❌ Fehler beim Laden der Drucker: {ex.Message}", StatusTyp.Fehler);
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Laden der Drucker: {ex.Message}");
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn sich die Drucker-Auswahl ändert
        /// Aktiviert den Speichern-Button und zeigt den ausgewählten Drucker an
        /// </summary>
        private void CmbDrucker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbDrucker.SelectedItem != null)
            {
                // Ausgewählten Drucker speichern
                AusgewaehlterDrucker = cmbDrucker.SelectedItem.ToString();

                // Speichern-Button aktivieren
                btnSpeichern.IsEnabled = true;

                // Status anzeigen
                ZeigeStatus($"✅ Drucker ausgewählt: {AusgewaehlterDrucker}", StatusTyp.Erfolg);
            }
            else
            {
                // Kein Drucker ausgewählt
                AusgewaehlterDrucker = null;
                btnSpeichern.IsEnabled = false;

                ZeigeStatus("Bitte wählen Sie einen Drucker aus.", StatusTyp.Info);
            }
        }

        /// <summary>
        /// Speichert die Drucker-Einstellungen und schließt das Fenster
        /// </summary>
        private async void BtnSpeichern_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(AusgewaehlterDrucker))
                {
                    ZeigeStatus("⚠️ Bitte wählen Sie zuerst einen Drucker aus.", StatusTyp.Warnung);
                    return;
                }

                // Speichern-Button deaktivieren um Doppelklicks zu vermeiden
                btnSpeichern.IsEnabled = false;
                ZeigeStatus("Einstellungen werden gespeichert...", StatusTyp.Info);

                // Einstellungen speichern
                bool erfolgreich = await DruckerEinstellungsService.EinstellungenSpeichernAsync(AusgewaehlterDrucker);

                if (erfolgreich)
                {
                    EinstellungenGespeichert = true;
                    ZeigeStatus($"✅ Drucker '{AusgewaehlterDrucker}' erfolgreich gespeichert!", StatusTyp.Erfolg);

                    // Kurz warten damit der Nutzer die Erfolgsmeldung sieht
                    await System.Threading.Tasks.Task.Delay(1000);

                    // Fenster mit DialogResult = true schließen
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ZeigeStatus("❌ Fehler beim Speichern der Einstellungen.", StatusTyp.Fehler);
                    btnSpeichern.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ZeigeStatus($"❌ Fehler beim Speichern: {ex.Message}", StatusTyp.Fehler);
                btnSpeichern.IsEnabled = true;
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Speichern der Drucker-Einstellungen: {ex.Message}");
            }
        }

        /// <summary>
        /// Schließt das Fenster ohne zu speichern
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            // Fenster mit DialogResult = false schließen
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Enum für verschiedene Status-Typen
        /// </summary>
        private enum StatusTyp
        {
            Info,
            Erfolg,
            Warnung,
            Fehler
        }

        /// <summary>
        /// Zeigt eine Status-Nachricht mit entsprechender Farbe an
        /// </summary>
        /// <param name="nachricht">Die anzuzeigende Nachricht</param>
        /// <param name="typ">Der Typ der Nachricht (bestimmt die Farbe)</param>
        private void ZeigeStatus(string nachricht, StatusTyp typ)
        {
            try
            {
                // Status-Text setzen
                txtStatus.Text = nachricht;

                // Farben je nach Status-Typ setzen
                switch (typ)
                {
                    case StatusTyp.Info:
                        statusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));  // Blau
                        statusBorder.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));   // Helles Blau
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(13, 71, 161));        // Dunkles Blau
                        break;

                    case StatusTyp.Erfolg:
                        statusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));    // Grün
                        statusBorder.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));   // Helles Grün
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));        // Dunkles Grün
                        break;

                    case StatusTyp.Warnung:
                        statusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0));    // Orange
                        statusBorder.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));   // Helles Orange
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));         // Dunkles Orange
                        break;

                    case StatusTyp.Fehler:
                        statusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));    // Rot
                        statusBorder.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));   // Helles Rot
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));        // Dunkles Rot
                        break;
                }

                // Status-Border sichtbar machen
                statusBorder.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Anzeigen des Status: {ex.Message}");
            }
        }
    }
}