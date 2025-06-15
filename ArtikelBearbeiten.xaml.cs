using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// UserControl zum Bearbeiten bestehender Artikel
    /// </summary>
    public partial class ArtikelBearbeiten : UserControl
    {
        /// <summary>
        /// Der zu bearbeitende Artikel
        /// </summary>
        private readonly Artikel _artikel;

        /// <summary>
        /// Collections für die Dropdown-Datenquellen
        /// </summary>
        private ObservableCollection<Lieferquelle> _lieferquellen;
        private ObservableCollection<Kostenstelle> _kostenstellen;
        private ObservableCollection<Lagerort> _lagerorte;

        /// <summary>
        /// Ursprüngliche Werte für Änderungserkennung
        /// </summary>
        private readonly int _originalLieferantId;
        private readonly int _originalHerstellerId;
        private readonly int _originalLieferzeit;
        private readonly string _originalExterneArtikelIdLieferant;
        private readonly string _originalExterneArtikelIdHersteller;
        private readonly int _originalKostenstelleId;
        private readonly int _originalLagerortId;
        private readonly bool _originalIstEinzelteil;
        private readonly int _originalMindestbestand;
        private readonly int _originalMaximalbestand;

        public ArtikelBearbeiten(Artikel artikel)
        {
            InitializeComponent();

            _artikel = artikel ?? throw new ArgumentNullException(nameof(artikel));

            // Ursprüngliche Werte speichern
            _originalLieferantId = artikel.LieferantId;
            _originalHerstellerId = artikel.HerstellerId;
            _originalLieferzeit = artikel.Lieferzeit;
            _originalExterneArtikelIdLieferant = artikel.ExterneArtikelIdLieferant;
            _originalExterneArtikelIdHersteller = artikel.ExterneArtikelIdHersteller;
            _originalKostenstelleId = artikel.KostenstelleId;
            _originalLagerortId = artikel.LagerortId;
            _originalIstEinzelteil = artikel.IstEinzelteil;
            _originalMindestbestand = artikel.Mindestbestand;
            _originalMaximalbestand = artikel.Maximalbestand;

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

            // Daten laden und Artikel-Daten in Felder laden
            LoadDataAndPopulateFieldsAsync();
        }

        /// <summary>
        /// Lädt Dropdown-Daten und befüllt die Felder mit Artikel-Daten
        /// </summary>
        private async void LoadDataAndPopulateFieldsAsync()
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

                // Felder mit Artikel-Daten befüllen
                PopulateFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Dropdown-Daten: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Befüllt die Eingabefelder mit den Artikel-Daten
        /// </summary>
        private void PopulateFields()
        {
            // Bezeichnung (schreibgeschützt)
            txtBezeichnung.Text = _artikel.Bezeichnung;

            // Textfelder
            txtExterneArtikelIdLieferant.Text = _artikel.ExterneArtikelIdLieferant;
            txtExterneArtikelIdHersteller.Text = _artikel.ExterneArtikelIdHersteller;
            txtMindestbestand.Text = _artikel.Mindestbestand.ToString();
            txtMaximalbestand.Text = _artikel.Maximalbestand.ToString();

            // ComboBoxes
            cmbLieferant.SelectedValue = _artikel.LieferantId;
            cmbHersteller.SelectedValue = _artikel.HerstellerId;
            cmbLieferzeit.SelectedItem = _artikel.Lieferzeit;
            cmbKostenstelle.SelectedValue = _artikel.KostenstelleId;
            cmbLagerort.SelectedValue = _artikel.LagerortId;

            // CheckBoxes
            if (_artikel.IstEinzelteil)
            {
                chkEinzelteil.IsChecked = true;
                chkKartonMitEinzelteilen.IsChecked = false;
            }
            else
            {
                chkKartonMitEinzelteilen.IsChecked = true;
                chkEinzelteil.IsChecked = false;
            }
        }

        /// <summary>
        /// Wird bei Änderungen in Textfeldern oder ComboBoxen ausgelöst
        /// </summary>
        private void InputField_Changed(object sender, EventArgs e)
        {
            CheckForChanges();
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

            CheckForChanges();
        }

        /// <summary>
        /// Überprüft ob Änderungen vorgenommen wurden und aktiviert/deaktiviert den Speichern-Button
        /// </summary>
        private void CheckForChanges()
        {
            // Prüft ob sich ein Wert geändert hat
            bool hasChanges =
                (int?)cmbLieferant.SelectedValue != _originalLieferantId ||
                (int?)cmbHersteller.SelectedValue != _originalHerstellerId ||
                (int?)cmbLieferzeit.SelectedItem != _originalLieferzeit ||
                txtExterneArtikelIdLieferant.Text != _originalExterneArtikelIdLieferant ||
                txtExterneArtikelIdHersteller.Text != _originalExterneArtikelIdHersteller ||
                (int?)cmbKostenstelle.SelectedValue != _originalKostenstelleId ||
                (int?)cmbLagerort.SelectedValue != _originalLagerortId ||
                (chkEinzelteil.IsChecked == true) != _originalIstEinzelteil ||
                txtMindestbestand.Text != _originalMindestbestand.ToString() ||
                txtMaximalbestand.Text != _originalMaximalbestand.ToString();

            // Prüft ob alle Felder ausgefüllt sind
            bool allFieldsFilled =
                !string.IsNullOrWhiteSpace(txtExterneArtikelIdLieferant.Text) &&
                !string.IsNullOrWhiteSpace(txtExterneArtikelIdHersteller.Text) &&
                !string.IsNullOrWhiteSpace(txtMindestbestand.Text) &&
                !string.IsNullOrWhiteSpace(txtMaximalbestand.Text) &&
                cmbLieferant.SelectedItem != null &&
                cmbHersteller.SelectedItem != null &&
                cmbLieferzeit.SelectedItem != null &&
                cmbKostenstelle.SelectedItem != null &&
                cmbLagerort.SelectedItem != null &&
                (chkKartonMitEinzelteilen.IsChecked == true || chkEinzelteil.IsChecked == true);

            // Button ist nur aktiv wenn Änderungen vorhanden sind UND alle Felder ausgefüllt sind
            btnSpeichern.IsEnabled = hasChanges && allFieldsFilled;
        }

        /// <summary>
        /// Speichert die Änderungen asynchron in der Datenbank
        /// </summary>
        private async void BtnSpeichern_Click(object sender, RoutedEventArgs e)
        {
            // Validierung vor dem Speichern
            if (!ValidateFields())
            {
                return;
            }

            try
            {
                // Button während des Speichervorgangs deaktivieren
                btnSpeichern.IsEnabled = false;
                btnSpeichern.Content = "Speichert...";

                // Asynchron in Datenbank speichern
                using (var context = new LagerContext())
                {
                    // Artikel anhand der ID aus der Datenbank laden
                    var artikelToUpdate = await context.Artikel
                        .FirstOrDefaultAsync(a => a.Id == _artikel.Id);

                    if (artikelToUpdate != null)
                    {
                        // Werte aktualisieren
                        artikelToUpdate.LieferantId = (int)cmbLieferant.SelectedValue;
                        artikelToUpdate.HerstellerId = (int)cmbHersteller.SelectedValue;
                        artikelToUpdate.Lieferzeit = (int)cmbLieferzeit.SelectedItem;
                        artikelToUpdate.ExterneArtikelIdLieferant = txtExterneArtikelIdLieferant.Text.Trim();
                        artikelToUpdate.ExterneArtikelIdHersteller = txtExterneArtikelIdHersteller.Text.Trim();
                        artikelToUpdate.KostenstelleId = (int)cmbKostenstelle.SelectedValue;
                        artikelToUpdate.LagerortId = (int)cmbLagerort.SelectedValue;
                        artikelToUpdate.IstEinzelteil = chkEinzelteil.IsChecked == true;
                        artikelToUpdate.Mindestbestand = int.Parse(txtMindestbestand.Text);
                        artikelToUpdate.Maximalbestand = int.Parse(txtMaximalbestand.Text);

                        // Änderungen speichern
                        await context.SaveChangesAsync();
                    }
                }

                // Erfolgsmeldung anzeigen
                MessageBox.Show("Änderung wurde erfolgreich gespeichert.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Zurück zur Artikel-Anzeige
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var artikelAnzeige = new ArtikelAnzeigen();
                    mainWindow.MainContentArea.Content = artikelAnzeige;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Änderungen: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                btnSpeichern.Content = "Speichern";
                CheckForChanges();
            }
        }

        /// <summary>
        /// Validiert alle editierbaren Felder
        /// </summary>
        private bool ValidateFields()
        {
            if (!int.TryParse(txtMindestbestand.Text, out int mindest) || mindest < 0)
            {
                MessageBox.Show("Mindestbestand muss eine positive Zahl sein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMindestbestand.Focus();
                return false;
            }

            if (!int.TryParse(txtMaximalbestand.Text, out int maximal) || maximal < 0)
            {
                MessageBox.Show("Maximalbestand muss eine positive Zahl sein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMaximalbestand.Focus();
                return false;
            }

            if (mindest > maximal)
            {
                MessageBox.Show("Mindestbestand darf nicht größer als Maximalbestand sein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMindestbestand.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Schließt die Artikel-Bearbeitung ohne zu speichern
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            // Zurück zur Artikel-Anzeige
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var artikelAnzeige = new ArtikelAnzeigen();
                mainWindow.MainContentArea.Content = artikelAnzeige;
            }
        }
    }
}