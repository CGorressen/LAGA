using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster zum Hinzufügen neuer Lieferquellen
    /// </summary>
    public partial class LieferquelleHinzufuegen : Window
    {
        public LieferquelleHinzufuegen()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Inhalt eines Eingabefelds ändert
        /// Dient zur Validierung der Eingaben in Echtzeit
        /// </summary>
        private void InputField_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Überprüft ob alle Pflichtfelder ausgefüllt sind
            ValidateInput();
        }

        /// <summary>
        /// Überprüft alle Eingabefelder und aktiviert/deaktiviert den Hinzufügen-Button
        /// </summary>
        private void ValidateInput()
        {
            // Button ist nur aktiv wenn alle Felder ausgefüllt sind
            bool allFieldsFilled = !string.IsNullOrWhiteSpace(txtBezeichnung.Text) &&
                                   !string.IsNullOrWhiteSpace(txtWebseite.Text) &&
                                   !string.IsNullOrWhiteSpace(txtEmail.Text) &&
                                   !string.IsNullOrWhiteSpace(txtTelefon.Text);

            btnHinzufuegen.IsEnabled = allFieldsFilled;
        }

        /// <summary>
        /// Speichert die neue Lieferquelle asynchron in der Datenbank
        /// </summary>
        private async void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Validierung vor dem Speichern
            if (!ValidateAllFields())
            {
                return;
            }

            try
            {
                // Button während des Speichervorgangs deaktivieren
                btnHinzufuegen.IsEnabled = false;
                btnHinzufuegen.Content = "Speichert...";

                // Neue Lieferquelle erstellen
                var neueLieferquelle = new Lieferquelle
                {
                    Bezeichnung = txtBezeichnung.Text.Trim(),
                    Webseite = txtWebseite.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Telefon = txtTelefon.Text.Trim()
                };

                // Asynchron in Datenbank speichern
                using (var context = new LagerContext())
                {
                    context.Lieferquellen.Add(neueLieferquelle);
                    await context.SaveChangesAsync();
                }

                // Erfolgsmeldung anzeigen
                MessageBox.Show("Lieferquelle wurde erfolgreich hinzugefügt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Eingabefelder zurücksetzen für weitere Eingaben
                ClearAllFields();
            }
            catch (Exception ex)
            {
                // Fehlermeldung bei Datenbankproblemen
                MessageBox.Show($"Fehler beim Speichern der Lieferquelle: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                btnHinzufuegen.Content = "Hinzufügen";
                ValidateInput(); // Prüft erneut ob Button aktiviert werden soll
            }
        }

        /// <summary>
        /// Schließt das Fenster ohne zu speichern
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Validiert alle Eingabefelder und zeigt Fehlermeldungen an
        /// </summary>
        private bool ValidateAllFields()
        {
            if (string.IsNullOrWhiteSpace(txtBezeichnung.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Bezeichnung ein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBezeichnung.Focus();
                return false;
            }

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

        /// <summary>
        /// Leert alle Eingabefelder nach erfolgreichem Speichern
        /// </summary>
        private void ClearAllFields()
        {
            txtBezeichnung.Clear();
            txtWebseite.Clear();
            txtEmail.Clear();
            txtTelefon.Clear();

            // Fokus auf das erste Feld setzen
            txtBezeichnung.Focus();
        }
    }
}