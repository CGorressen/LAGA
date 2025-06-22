using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster zur Anzeige aller E-Mail-Empfänger mit Lösch- und Test-E-Mail-Funktionen
    /// Orientiert sich an der Struktur von KostenstellenAnzeigen
    /// </summary>
    public partial class EmpfängerAnzeigen : Window
    {
        /// <summary>
        /// Observable Collection für die Empfänger (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<Empfaenger> _empfaenger;

        /// <summary>
        /// CollectionView für Sortierung
        /// </summary>
        private ICollectionView _empfaengerView;

        public EmpfängerAnzeigen()
        {
            InitializeComponent();
            _empfaenger = new ObservableCollection<Empfaenger>();

            // CollectionView für Sortierung erstellen
            _empfaengerView = CollectionViewSource.GetDefaultView(_empfaenger);

            // Alphabetische Sortierung nach E-Mail-Adresse
            _empfaengerView.SortDescriptions.Add(new SortDescription("Email", ListSortDirection.Ascending));

            // DataGrid an CollectionView binden
            dgEmpfaenger.ItemsSource = _empfaengerView;

            // Daten beim Laden asynchron abrufen
            LoadEmpfaengerAsync();
        }

        /// <summary>
        /// Lädt alle E-Mail-Empfänger asynchron aus der Datenbank
        /// </summary>
        private async void LoadEmpfaengerAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle Empfänger asynchron laden
                    var empfaenger = await context.Empfaenger.ToListAsync();

                    // ObservableCollection aktualisieren (UI wird automatisch aktualisiert)
                    _empfaenger.Clear();
                    foreach (var empfaengerItem in empfaenger)
                    {
                        _empfaenger.Add(empfaengerItem);
                    }
                }

                // Button-Status basierend auf verfügbaren Empfängern aktualisieren
                UpdateButtonStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Empfänger: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aktualisiert den Status des Test-E-Mail-Buttons basierend auf verfügbaren Empfängern
        /// </summary>
        private void UpdateButtonStatus()
        {
            // Test-E-Mail Button nur aktivieren wenn Empfänger vorhanden sind
            btnTestEmailVersenden.IsEnabled = _empfaenger.Count > 0;
        }

        /// <summary>
        /// Behandelt Rechtsklick auf DataGrid-Zeilen für Kontextmenü
        /// </summary>
        private void DgEmpfaenger_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Finde das geklickte Element
            var hitTest = dgEmpfaenger.InputHitTest(e.GetPosition(dgEmpfaenger));

            // Gehe den visuellen Baum hoch um die DataGridRow zu finden
            DependencyObject dep = (DependencyObject)hitTest;
            while (dep != null && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridRow row)
            {
                // Zeile auswählen wenn sie noch nicht ausgewählt ist
                row.IsSelected = true;
                dgEmpfaenger.SelectedItem = row.Item;

                // ContextMenu ist bereits im XAML definiert, daher automatisch verfügbar
            }
        }

        /// <summary>
        /// Löscht den ausgewählten Empfänger nach Bestätigung
        /// </summary>
        private async void MenuItemLoeschen_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmpfaenger.SelectedItem is Empfaenger selectedEmpfaenger)
            {
                // Bestätigungsdialog anzeigen mit spezifischer E-Mail-Adresse
                var result = MessageBox.Show(
                    $"{selectedEmpfaenger.Email}\n\n" +
                    "Sind Sie sicher, dass dieser Empfänger gelöscht werden soll?",
                    "Empfänger löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Empfänger asynchron aus Datenbank löschen
                        using (var context = new LagerContext())
                        {
                            // Empfänger anhand der ID finden und löschen
                            var empfaengerToDelete = await context.Empfaenger
                                .FirstOrDefaultAsync(e => e.Id == selectedEmpfaenger.Id);

                            if (empfaengerToDelete != null)
                            {
                                context.Empfaenger.Remove(empfaengerToDelete);
                                await context.SaveChangesAsync();

                                // Erfolgsmeldung
                                MessageBox.Show("Empfänger wurde erfolgreich gelöscht.",
                                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                                // Daten neu laden
                                LoadEmpfaengerAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen des Empfängers: {ex.Message}",
                            "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Sendet Test-E-Mail an alle registrierten Empfänger über Gmail API
        /// </summary>
        private async void BtnTestEmailVersenden_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Button während des Sendens deaktivieren
                btnTestEmailVersenden.IsEnabled = false;
                btnTestEmailVersenden.Content = "Sendet...";

                // Test-E-Mail über Gmail Service senden
                bool erfolg = await GmailEmailService.SendeTestEmailAsync();

                // Button-Text und Status zurücksetzen
                btnTestEmailVersenden.Content = "Test-E-Mail versenden";
                UpdateButtonStatus();

                // Zusätzliche Erfolgsmeldung nur bei Fehlern, da GmailService bereits Meldungen anzeigt
                // Die Erfolgsmeldungen werden bereits im GmailService angezeigt
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unerwarteter Fehler beim Senden der Test-E-Mail: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button immer wieder aktivieren
                btnTestEmailVersenden.Content = "Test-E-Mail versenden";
                UpdateButtonStatus();
            }
        }

        /// <summary>
        /// Öffnet das Hinzufügen-Fenster für einen neuen Empfänger
        /// </summary>
        private void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Empfänger-Hinzufügen-Fenster als modalen Dialog öffnen
            var hinzufuegenFenster = new EmpfängerHinzufuegen();
            hinzufuegenFenster.Owner = this; // Dieses Fenster als Owner setzen

            // Nach dem Schließen des Hinzufügen-Fensters prüfen ob erfolgreich gespeichert
            // DialogResult wird automatisch auf true gesetzt wenn erfolgreich gespeichert wurde
            if (hinzufuegenFenster.ShowDialog() == true)
            {
                // Daten neu laden um neuen Empfänger anzuzeigen
                LoadEmpfaengerAsync();
            }
        }

        /// <summary>
        /// Schließt das Empfänger-Anzeige-Fenster
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}