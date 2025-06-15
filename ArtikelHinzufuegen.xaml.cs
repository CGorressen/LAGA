using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// UserControl zum Hinzufügen neuer Artikel mit komplexer Validierung
    /// </summary>
    public partial class ArtikelHinzufuegen : UserControl
    {
        /// <summary>
        /// Collections für die Dropdown-Datenquellen
        /// </summary>
        private ObservableCollection<Lieferquelle> _lieferquellen;
        private ObservableCollection<Kostenstelle> _kostenstellen;
        private ObservableCollection<Lagerort> _lagerorte;

        public ArtikelHinzufuegen()
        {
            InitializeComponent();

            // Collections initialisieren
            _lieferquellen = new ObservableCollection<Lieferquelle>();
            _kostenstellen = new ObservableCollection<Kostenstelle>();
            _lagerorte = new ObservableCollection<Lagerort>();

            // Dropdown-Datenquellen setzen
            cmbLieferant.ItemsSource = _lieferquellen;
            cmbHersteller.ItemsSource = _lieferquellen;
            cmbKostenstelle.ItemsSource = _kostenstellen;
            cmbLagerort.ItemsSource = _lagerorte;

            // Lieferzeit-Dropdown mit Werten 1-10 füllen
            for (int i = 1; i <= 10; i++)
            {
                cmbLieferzeit.Items.Add(i);
            }

            // Daten laden
            LoadDropdownDataAsync();
        }

        /// <summary>
        /// Lädt alle Daten für die Dropdowns asynchron aus der Datenbank
        /// </summary>
        private async void LoadDropdownDataAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Lieferquellen laden (für Lieferant und Hersteller)
                    var lieferquellen = await context.Lieferquellen
                        .OrderBy(l => l.Bezeichnung)
                        .ToListAsync();

                    _lieferquellen.Clear();
                    foreach (var lieferquelle in lieferquellen)
                    {
                        _lieferquellen.Add(lieferquelle);
                    }

                    // Kostenstellen laden
                    var kostenstellen = await context.Kostenstellen
                        .OrderBy(k => k.Bezeichnung)
                        .ToListAsync();

                    _kostenstellen.Clear();
                    foreach (var kostenstelle in kostenstellen)
                    {
                        _kostenstellen.Add(kostenstelle);
                    }

                    // Lagerorte laden
                    var lagerorte = await context.Lagerorte
                        .OrderBy(l => l.Bezeichnung)
                        .ToListAsync();

                    _lagerorte.Clear();
                    foreach (var lagerort in lagerorte)
                    {
                        _lagerorte.Add(lagerort);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Dropdown-Daten: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Wird bei Änderungen in Textfeldern oder ComboBoxen ausgelöst
        /// </summary>
        private void InputField_Changed(object sender, EventArgs e)
        {
            ValidateAllInputs();
        }

        /// <summary>
        /// Wird bei Änderungen der Einheit-CheckBoxes ausgelöst
        /// </summary>
        private void Einheit_Changed(object sender, RoutedEventArgs e)
        {
            // Sicherstellen dass nur eine CheckBox ausgewählt ist
            if (sender == chkKartonMitEinzelteilen && chkKartonMitEinzelteilen.IsChecked == true)
            {
                chkEinzelteil.IsChecked = false;
            }
            else if (sender == chkEinzelteil && chkEinzelteil.IsChecked == true)
            {
                chkKartonMitEinzelteilen.IsChecked = false;
            }

            ValidateAllInputs();
        }

        /// <summary>
        /// Validiert alle Eingaben und aktiviert/deaktiviert den Hinzufügen-Button
        /// </summary>
        private void ValidateAllInputs()
        {
            bool isValid = true;

            // Textfelder prüfen
            if (string.IsNullOrWhiteSpace(txtBezeichnung.Text) ||
                string.IsNullOrWhiteSpace(txtExterneArtikelIdLieferant.Text) ||
                string.IsNullOrWhiteSpace(txtExterneArtikelIdHersteller.Text) ||
                string.IsNullOrWhiteSpace(txtMindestbestand.Text) ||
                string.IsNullOrWhiteSpace(txtMaximalbestand.Text))
            {
                isValid = false;
            }

            // ComboBoxes prüfen
            if (cmbLieferant.SelectedItem == null ||
                cmbHersteller.SelectedItem == null ||
                cmbLieferzeit.SelectedItem == null ||
                cmbKostenstelle.SelectedItem == null ||
                cmbLagerort.SelectedItem == null)
            {
                isValid = false;
            }

            // Einheit-CheckBoxes prüfen (genau eine muss ausgewählt sein)
            if (chkKartonMitEinzelteilen.IsChecked != true && chkEinzelteil.IsChecked != true)
            {
                isValid = false;
            }

            // Integer-Validierung für Bestände
            if (isValid)
            {
                if (!int.TryParse(txtMindestbestand.Text, out int mindest) || mindest < 0 ||
                    !int.TryParse(txtMaximalbestand.Text, out int maximal) || maximal < 0 ||
                    mindest > maximal)
                {
                    isValid = false;
                }
            }

            btnHinzufuegen.IsEnabled = isValid;
        }

        /// <summary>
        /// Speichert den neuen Artikel asynchron in der Datenbank
        /// </summary>
        private async void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Umfassende Validierung vor dem Speichern
            if (!await ValidateBeforeSaveAsync())
            {
                return;
            }

            try
            {
                // Button während des Speichervorgangs deaktivieren
                btnHinzufuegen.IsEnabled = false;
                btnHinzufuegen.Content = "Speichert...";

                // Neuen Artikel erstellen
                var neuerArtikel = new Artikel
                {
                    Bezeichnung = txtBezeichnung.Text.Trim(),
                    LieferantId = (int)cmbLieferant.SelectedValue,
                    HerstellerId = (int)cmbHersteller.SelectedValue,
                    Lieferzeit = (int)cmbLieferzeit.SelectedItem,
                    ExterneArtikelIdLieferant = txtExterneArtikelIdLieferant.Text.Trim(),
                    ExterneArtikelIdHersteller = txtExterneArtikelIdHersteller.Text.Trim(),
                    KostenstelleId = (int)cmbKostenstelle.SelectedValue,
                    LagerortId = (int)cmbLagerort.SelectedValue,
                    IstEinzelteil = chkEinzelteil.IsChecked == true,
                    Mindestbestand = int.Parse(txtMindestbestand.Text),
                    Maximalbestand = int.Parse(txtMaximalbestand.Text)
                };

                // Asynchron in Datenbank speichern
                using (var context = new LagerContext())
                {
                    context.Artikel.Add(neuerArtikel);
                    await context.SaveChangesAsync();
                }

                // Erfolgsmeldung anzeigen
                MessageBox.Show("Artikel wurde erfolgreich hinzugefügt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Felder zurücksetzen für weitere Eingaben
                ClearAllFields();
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
            {
                MessageBox.Show("Ein Artikel mit dieser Bezeichnung existiert bereits. Bitte wählen Sie eine andere Bezeichnung.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBezeichnung.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern des Artikels: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                btnHinzufuegen.Content = "Hinzufügen";
                ValidateAllInputs();
            }
        }

        /// <summary>
        /// Umfassende Validierung vor dem Speichern
        /// </summary>
        private async Task<bool> ValidateBeforeSaveAsync()
        {
            // Bezeichnung bereits vorhanden?
            try
            {
                using (var context = new LagerContext())
                {
                    var exists = await context.Artikel
                        .AnyAsync(a => a.Bezeichnung == txtBezeichnung.Text.Trim());

                    if (exists)
                    {
                        MessageBox.Show("Ein Artikel mit dieser Bezeichnung existiert bereits.",
                            "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtBezeichnung.Focus();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Validierung: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Schließt die Artikel-Ansicht
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ClearMainContent();
        }

        /// <summary>
        /// Leert alle Eingabefelder nach erfolgreichem Speichern
        /// </summary>
        private void ClearAllFields()
        {
            txtBezeichnung.Clear();
            txtExterneArtikelIdLieferant.Clear();
            txtExterneArtikelIdHersteller.Clear();
            txtMindestbestand.Clear();
            txtMaximalbestand.Clear();

            cmbLieferant.SelectedItem = null;
            cmbHersteller.SelectedItem = null;
            cmbLieferzeit.SelectedItem = null;
            cmbKostenstelle.SelectedItem = null;
            cmbLagerort.SelectedItem = null;

            chkKartonMitEinzelteilen.IsChecked = false;
            chkEinzelteil.IsChecked = false;

            txtBezeichnung.Focus();
        }
    }
}