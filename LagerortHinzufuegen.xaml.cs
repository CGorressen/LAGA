using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster zum Hinzufügen neuer Lagerorte
    /// Nach erfolgreichem Speichern wird der Dialog automatisch geschlossen
    /// </summary>
    public partial class LagerortHinzufuegen : Window
    {
        public LagerortHinzufuegen()
        {
            InitializeComponent();

            // Fokus auf das Eingabefeld setzen
            Loaded += (s, e) => txtBezeichnung.Focus();
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Inhalt des Eingabefelds ändert
        /// Dient zur Validierung der Eingabe in Echtzeit
        /// </summary>
        private void InputField_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        /// <summary>
        /// Behandelt Enter-Taste im Eingabefeld
        /// </summary>
        private void TxtBezeichnung_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnLagerortHinzufuegen.IsEnabled)
            {
                BtnLagerortHinzufuegen_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Überprüft das Eingabefeld und aktiviert/deaktiviert den Hinzufügen-Button
        /// </summary>
        private void ValidateInput()
        {
            // Button ist nur aktiv wenn das Feld ausgefüllt ist
            bool fieldFilled = !string.IsNullOrWhiteSpace(txtBezeichnung.Text);
            btnLagerortHinzufuegen.IsEnabled = fieldFilled;
        }

        /// <summary>
        /// Speichert den neuen Lagerort asynchron in der Datenbank
        /// Nach erfolgreichem Speichern wird der Dialog automatisch geschlossen
        /// </summary>
        private async void BtnLagerortHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Validierung vor dem Speichern (inklusive Duplikatsprüfung)
            if (!ValidateField() || !await ValidateBeforeSaveAsync())
            {
                return;
            }

            try
            {
                // Button während des Speichervorgangs deaktivieren
                btnLagerortHinzufuegen.IsEnabled = false;
                btnLagerortHinzufuegen.Content = "Speichert...";

                // Neuen Lagerort erstellen
                var neuerLagerort = new Lagerort
                {
                    Bezeichnung = txtBezeichnung.Text.Trim()
                };

                // Asynchron in Datenbank speichern
                using (var context = new LagerContext())
                {
                    context.Lagerorte.Add(neuerLagerort);
                    await context.SaveChangesAsync();
                }

                // Erfolgsmeldung anzeigen
                MessageBox.Show("Lagerort wurde erfolgreich hinzugefügt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Dialog schließen und Erfolg signalisieren
                this.DialogResult = true;
                this.Close();
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
            {
                MessageBox.Show("Ein Lagerort mit dieser Bezeichnung existiert bereits. Bitte wählen Sie eine andere Bezeichnung.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBezeichnung.Focus();
            }
            catch (Exception ex)
            {
                // Fehlermeldung bei Datenbankproblemen
                MessageBox.Show($"Fehler beim Speichern des Lagerortes: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                btnLagerortHinzufuegen.Content = "Lagerort Hinzufügen";
                ValidateInput(); // Prüft erneut ob Button aktiviert werden soll
            }
        }

        /// <summary>
        /// Umfassende Validierung vor dem Speichern
        /// Prüft ob bereits ein Lagerort mit derselben Bezeichnung existiert
        /// </summary>
        private async Task<bool> ValidateBeforeSaveAsync()
        {
            // Bezeichnung bereits vorhanden?
            try
            {
                using (var context = new LagerContext())
                {
                    var exists = await context.Lagerorte
                        .AnyAsync(l => l.Bezeichnung == txtBezeichnung.Text.Trim());

                    if (exists)
                    {
                        MessageBox.Show("Ein Lagerort mit dieser Bezeichnung existiert bereits.",
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
        /// Validiert das Eingabefeld und zeigt Fehlermeldung an
        /// </summary>
        private bool ValidateField()
        {
            if (string.IsNullOrWhiteSpace(txtBezeichnung.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Bezeichnung ein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBezeichnung.Focus();
                return false;
            }

            return true;
        }
    }
}