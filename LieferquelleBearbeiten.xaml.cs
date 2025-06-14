using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster zum Bearbeiten einer bestehenden Lieferquelle
    /// </summary>
    public partial class LieferquelleBearbeiten : Window
    {
        /// <summary>
        /// Die zu bearbeitende Lieferquelle
        /// </summary>
        private readonly Lieferquelle _lieferquelle;

        /// <summary>
        /// Ursprüngliche Werte für Änderungserkennung
        /// </summary>
        private readonly string _originalWebseite;
        private readonly string _originalEmail;
        private readonly string _originalTelefon;

        /// <summary>
        /// Konstruktor - erhält die zu bearbeitende Lieferquelle
        /// </summary>
        public LieferquelleBearbeiten(Lieferquelle lieferquelle)
        {
            InitializeComponent();

            _lieferquelle = lieferquelle ?? throw new ArgumentNullException(nameof(lieferquelle));

            // Ursprüngliche Werte speichern
            _originalWebseite = lieferquelle.Webseite;
            _originalEmail = lieferquelle.Email;
            _originalTelefon = lieferquelle.Telefon;

            // Felder mit aktuellen Werten befüllen
            LoadLieferquelleData();
        }

        /// <summary>
        /// Lädt die Daten der Lieferquelle in die Eingabefelder
        /// </summary>
        private void LoadLieferquelleData()
        {
            txtBezeichnung.Text = _lieferquelle.Bezeichnung;
            txtWebseite.Text = _lieferquelle.Webseite;
            txtEmail.Text = _lieferquelle.Email;
            txtTelefon.Text = _lieferquelle.Telefon;
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Inhalt eines editierbaren Felds ändert
        /// </summary>
        private void EditableField_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckForChanges();
        }

        /// <summary>
        /// Überprüft ob Änderungen vorgenommen wurden und aktiviert/deaktiviert den Speichern-Button
        /// </summary>
        private void CheckForChanges()
        {
            // Prüft ob sich ein Wert geändert hat
            bool hasChanges = txtWebseite.Text != _originalWebseite ||
                             txtEmail.Text != _originalEmail ||
                             txtTelefon.Text != _originalTelefon;

            // Prüft ob alle Felder ausgefüllt sind
            bool allFieldsFilled = !string.IsNullOrWhiteSpace(txtWebseite.Text) &&
                                  !string.IsNullOrWhiteSpace(txtEmail.Text) &&
                                  !string.IsNullOrWhiteSpace(txtTelefon.Text);

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
                    // Lieferquelle anhand der ID aus der Datenbank laden
                    var lieferquelleToUpdate = await context.Lieferquellen
                        .FirstOrDefaultAsync(l => l.Id == _lieferquelle.Id);

                    if (lieferquelleToUpdate != null)
                    {
                        // Werte aktualisieren
                        lieferquelleToUpdate.Webseite = txtWebseite.Text.Trim();
                        lieferquelleToUpdate.Email = txtEmail.Text.Trim();
                        lieferquelleToUpdate.Telefon = txtTelefon.Text.Trim();

                        // Änderungen speichern
                        await context.SaveChangesAsync();
                    }
                }

                // Erfolgsmeldung anzeigen
                MessageBox.Show("Änderung wurde erfolgreich gespeichert.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Fenster schließen und Erfolg signalisieren
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                // Fehlermeldung bei Datenbankproblemen
                MessageBox.Show($"Fehler beim Speichern der Änderungen: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                btnSpeichern.Content = "Speichern";
                CheckForChanges(); // Button-Status neu bewerten
            }
        }

        /// <summary>
        /// Schließt das Fenster ohne zu speichern
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Validiert alle editierbaren Felder
        /// </summary>
        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(txtWebseite.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Webseite ein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtWebseite.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Bitte geben Sie eine E-Mail-Adresse ein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtTelefon.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Telefonnummer ein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTelefon.Focus();
                return false;
            }

            return true;
        }
    }
}