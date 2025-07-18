using System;
using System.Windows;
using System.Windows.Controls;

namespace LAGA
{
    /// <summary>
    /// Hauptfenster der LAGA-Anwendung mit dynamisch austauschbarem Content-Bereich
    /// VEREINFACHT: Initialisierung erfolgt jetzt im LoadingSplashWindow
    /// Dieses Fenster wird erst angezeigt wenn alles bereit ist
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // StartFenster als Standard-Ansicht laden
            // (Initialisierung ist bereits im LoadingSplashWindow erfolgt)
            LoadStartFenster();
        }

        /// <summary>
        /// Lädt das StartFenster - Scanner ist sofort verfügbar
        /// </summary>
        private void LoadStartFenster()
        {
            var startFenster = new StartFenster();
            MainContentArea.Content = startFenster;
        }

        /// <summary>
        /// Öffentliche Methode zum Leeren des Content-Bereichs
        /// Lädt automatisch das StartFenster als Standard-Ansicht
        /// </summary>
        public void ClearMainContent()
        {
            LoadStartFenster();
        }

        // ===============================
        // MENÜ EVENT-HANDLER
        // ===============================

        /// <summary>
        /// Zeigt die Lieferquellen-Verwaltung im Hauptbereich an
        /// </summary>
        private void LieferquellenAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lieferquellenFenster = new LieferquellenAnzeigen();
                MainContentArea.Content = lieferquellenFenster;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Lieferquellen-Verwaltung: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Öffnet das Kostenstellen-Verwaltungsfenster als modales Popup
        /// </summary>
        private void KostenstellenAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var kostenstellenFenster = new KostenstellenAnzeigen();
                kostenstellenFenster.Owner = this;
                kostenstellenFenster.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Kostenstellen-Verwaltung: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Öffnet das Lagerorte-Verwaltungsfenster als modales Popup
        /// </summary>
        private void LagerorteAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lagerorteFenster = new LagerorteAnzeigen();
                lagerorteFenster.Owner = this;
                lagerorteFenster.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Lagerorte-Verwaltung: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt die Artikel-Verwaltung im Hauptbereich an
        /// </summary>
        private void ArtikelAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var artikelFenster = new ArtikelAnzeigen();
                MainContentArea.Content = artikelFenster;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Artikel-Verwaltung: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt die Lagerbestand-Übersicht im Hauptbereich an
        /// </summary>
        private void LagerbestandAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lagerbestandFenster = new LagerBestandAnzeigen();
                MainContentArea.Content = lagerbestandFenster;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Lagerbestand-Übersicht: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt die Warnungen-Übersicht im Hauptbereich an
        /// </summary>
        private void WarnungenAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var warnungenFenster = new WarnArtikelAnzeigen();
                MainContentArea.Content = warnungenFenster;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Warnungen-Übersicht: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zeigt die E-Mail-Empfänger-Verwaltung im Hauptbereich an
        /// </summary>
        private void EmpfaengerAnzeigen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var empfaengerAnzeige = new EmpfängerAnzeigen();
                empfaengerAnzeige.Owner = this;
                empfaengerAnzeige.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Empfänger-Anzeige-Fensters: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Öffnet das Datenbanksicherung-Einstellungen-Fenster als modales Popup
        /// </summary>
        private void DatenbanksicherungEinstellungen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupEinstellungenFenster = new DatenbanksicherungEinstellungen();
                backupEinstellungenFenster.Owner = this;
                backupEinstellungenFenster.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Datenbanksicherung-Einstellungen: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        /// <summary>
        /// Öffnet das Drucker-Einrichtungsfenster als modales Popup
        /// </summary>
        private void DruckerEinrichten_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var druckerFenster = new DruckerEinrichten();
                druckerFenster.Owner = this;
                var result = druckerFenster.ShowDialog();

                // Optional: Erfolgsmeldung anzeigen wenn Drucker konfiguriert wurde
                if (result == true && druckerFenster.EinstellungenGespeichert)
                {
                    MessageBox.Show($"Drucker '{druckerFenster.AusgewaehlterDrucker}' wurde erfolgreich konfiguriert.",
                        "Erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Drucker-Einrichtung: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}