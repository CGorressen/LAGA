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
    /// Modales Fenster zur Anzeige aller Lagerorte mit Lösch- und Hinzufügefunktion
    /// </summary>
    public partial class LagerorteAnzeigen : Window
    {
        /// <summary>
        /// Observable Collection für die Lagerorte (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<Lagerort> _lagerorte;

        /// <summary>
        /// CollectionView für Sortierung
        /// </summary>
        private ICollectionView _lagerorteView;

        public LagerorteAnzeigen()
        {
            InitializeComponent();
            _lagerorte = new ObservableCollection<Lagerort>();

            // CollectionView für Sortierung erstellen
            _lagerorteView = CollectionViewSource.GetDefaultView(_lagerorte);

            // Alphabetische Sortierung nach Bezeichnung
            _lagerorteView.SortDescriptions.Add(new SortDescription("Bezeichnung", ListSortDirection.Ascending));

            // DataGrid an CollectionView binden
            dgLagerorte.ItemsSource = _lagerorteView;

            // Daten beim Laden asynchron abrufen
            LoadLagerorteAsync();
        }

        /// <summary>
        /// Lädt alle Lagerorte asynchron aus der Datenbank
        /// </summary>
        private async void LoadLagerorteAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle Lagerorte asynchron laden
                    var lagerorte = await context.Lagerorte.ToListAsync();

                    // ObservableCollection aktualisieren (UI wird automatisch aktualisiert)
                    _lagerorte.Clear();
                    foreach (var lagerort in lagerorte)
                    {
                        _lagerorte.Add(lagerort);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Lagerorte: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Behandelt Rechtsklick auf DataGrid-Zeilen
        /// </summary>
        private void DgLagerorte_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Finde das geklickte Element
            var hitTest = dgLagerorte.InputHitTest(e.GetPosition(dgLagerorte));

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
                dgLagerorte.SelectedItem = row.Item;

                // ContextMenu ist bereits im XAML definiert, daher automatisch verfügbar
            }
        }

        /// <summary>
        /// Löscht den ausgewählten Lagerort nach Bestätigung
        /// </summary>
        private async void MenuItemLoeschen_Click(object sender, RoutedEventArgs e)
        {
            if (dgLagerorte.SelectedItem is Lagerort selectedLagerort)
            {
                // Bestätigungsdialog anzeigen
                var result = MessageBox.Show(
                    "Sind Sie sicher, dass der Lagerort gelöscht werden soll?",
                    "Lagerort löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Lagerort asynchron aus Datenbank löschen
                        using (var context = new LagerContext())
                        {
                            // Lagerort anhand der ID finden und löschen
                            var lagerortToDelete = await context.Lagerorte
                                .FirstOrDefaultAsync(l => l.Id == selectedLagerort.Id);

                            if (lagerortToDelete != null)
                            {
                                context.Lagerorte.Remove(lagerortToDelete);
                                await context.SaveChangesAsync();

                                // Erfolgsmeldung
                                MessageBox.Show("Lagerort wurde erfolgreich gelöscht.",
                                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                                // Daten neu laden
                                LoadLagerorteAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen des Lagerortes: {ex.Message}",
                            "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Öffnet das Hinzufügen-Fenster für einen neuen Lagerort
        /// </summary>
        private void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Lagerort-Hinzufügen-Fenster als modalen Dialog öffnen
            var hinzufuegenFenster = new LagerortHinzufuegen();
            hinzufuegenFenster.Owner = this; // Dieses Fenster als Owner setzen

            // Nach dem Schließen des Hinzufügen-Fensters prüfen ob erfolgreich gespeichert
            if (hinzufuegenFenster.ShowDialog() == true)
            {
                // Daten neu laden um neuen Lagerort anzuzeigen
                LoadLagerorteAsync();
            }
        }

        /// <summary>
        /// Schließt das Lagerorte-Anzeige-Fenster
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}