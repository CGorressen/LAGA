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
    /// Modales Fenster zur Anzeige aller Kostenstellen mit Lösch- und Hinzufügefunktion
    /// </summary>
    public partial class KostenstellenAnzeigen : Window
    {
        /// <summary>
        /// Observable Collection für die Kostenstellen (automatisches UI-Update)
        /// </summary>
        private ObservableCollection<Kostenstelle> _kostenstellen;

        /// <summary>
        /// CollectionView für Sortierung
        /// </summary>
        private ICollectionView _kostenstellenView;

        public KostenstellenAnzeigen()
        {
            InitializeComponent();
            _kostenstellen = new ObservableCollection<Kostenstelle>();

            // CollectionView für Sortierung erstellen
            _kostenstellenView = CollectionViewSource.GetDefaultView(_kostenstellen);

            // Alphabetische Sortierung nach Bezeichnung
            _kostenstellenView.SortDescriptions.Add(new SortDescription("Bezeichnung", ListSortDirection.Ascending));

            // DataGrid an CollectionView binden
            dgKostenstellen.ItemsSource = _kostenstellenView;

            // Daten beim Laden asynchron abrufen
            LoadKostenstellenAsync();
        }

        /// <summary>
        /// Lädt alle Kostenstellen asynchron aus der Datenbank
        /// </summary>
        private async void LoadKostenstellenAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Alle Kostenstellen asynchron laden
                    var kostenstellen = await context.Kostenstellen.ToListAsync();

                    // ObservableCollection aktualisieren (UI wird automatisch aktualisiert)
                    _kostenstellen.Clear();
                    foreach (var kostenstelle in kostenstellen)
                    {
                        _kostenstellen.Add(kostenstelle);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Kostenstellen: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Behandelt Rechtsklick auf DataGrid-Zeilen
        /// </summary>
        private void DgKostenstellen_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Finde das geklickte Element
            var hitTest = dgKostenstellen.InputHitTest(e.GetPosition(dgKostenstellen));

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
                dgKostenstellen.SelectedItem = row.Item;

                // ContextMenu ist bereits im XAML definiert, daher automatisch verfügbar
            }
        }

        /// <summary>
        /// Löscht die ausgewählte Kostenstelle nach Bestätigung
        /// </summary>
        private async void MenuItemLoeschen_Click(object sender, RoutedEventArgs e)
        {
            if (dgKostenstellen.SelectedItem is Kostenstelle selectedKostenstelle)
            {
                // Bestätigungsdialog anzeigen
                var result = MessageBox.Show(
                    "Sind Sie sicher, dass die Kostenstelle gelöscht werden soll?",
                    "Kostenstelle löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Kostenstelle asynchron aus Datenbank löschen
                        using (var context = new LagerContext())
                        {
                            // Kostenstelle anhand der ID finden und löschen
                            var kostenstelleToDelete = await context.Kostenstellen
                                .FirstOrDefaultAsync(k => k.Id == selectedKostenstelle.Id);

                            if (kostenstelleToDelete != null)
                            {
                                context.Kostenstellen.Remove(kostenstelleToDelete);
                                await context.SaveChangesAsync();

                                // Erfolgsmeldung
                                MessageBox.Show("Kostenstelle wurde erfolgreich gelöscht.",
                                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                                // Daten neu laden
                                LoadKostenstellenAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen der Kostenstelle: {ex.Message}",
                            "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Öffnet das Hinzufügen-Fenster für eine neue Kostenstelle
        /// </summary>
        private void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Kostenstelle-Hinzufügen-Fenster als modalen Dialog öffnen
            var hinzufuegenFenster = new KostenstelleHinzufuegen();
            hinzufuegenFenster.Owner = this; // Dieses Fenster als Owner setzen

            // Nach dem Schließen des Hinzufügen-Fensters prüfen ob erfolgreich gespeichert
            // DialogResult wird automatisch auf true gesetzt wenn erfolgreich gespeichert wurde
            if (hinzufuegenFenster.ShowDialog() == true)
            {
                // Daten neu laden um neue Kostenstelle anzuzeigen
                LoadKostenstellenAsync();
            }
        }

        /// <summary>
        /// Schließt das Kostenstellen-Anzeige-Fenster
        /// </summary>
        private void BtnAnsichtSchliessen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}