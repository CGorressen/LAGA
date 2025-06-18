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
            // Validierung vor dem Speichern (inklusive Duplikatsprüfung)
            if (!ValidateAllFields() || !await ValidateBeforeSaveAsync())
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

                // Eingabefelder leeren für weitere Eingaben
                ClearAllFields();
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
            {
                MessageBox.Show("Eine Lieferquelle mit dieser Bezeichnung existiert bereits. Bitte wählen Sie eine andere Bezeichnung.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBezeichnung.Focus();
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
        /// Umfassende Validierung vor dem Speichern
        /// Prüft ob bereits eine Lieferquelle mit derselben Bezeichnung existiert
        /// </summary>
        private async Task<bool> ValidateBeforeSaveAsync()
        {
            // Bezeichnung bereits vorhanden?
            try
            {
                using (var context = new LagerContext())
                {
                    var exists = await context.Lieferquellen
                        .AnyAsync(l => l.Bezeichnung == txtBezeichnung.Text.Trim());

                    if (exists)
                    {
                        MessageBox.Show("Eine Lieferquelle mit dieser Bezeichnung existiert bereits.",
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
        /// Schließt das Fenster ohne zu speichern
        /// </summary>
        private void BtnAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
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

            txtBezeichnung.Focus();
        }
    }
}