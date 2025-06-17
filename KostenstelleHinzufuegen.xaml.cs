using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster zum Hinzufügen neuer Kostenstellen
    /// </summary>
    public partial class KostenstelleHinzufuegen : Window
    {
        public KostenstelleHinzufuegen()
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
            if (e.Key == Key.Enter && btnHinzufuegen.IsEnabled)
            {
                BtnHinzufuegen_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Überprüft das Eingabefeld und aktiviert/deaktiviert den Hinzufügen-Button
        /// </summary>
        private void ValidateInput()
        {
            // Button ist nur aktiv wenn das Feld ausgefüllt ist
            bool fieldFilled = !string.IsNullOrWhiteSpace(txtBezeichnung.Text);
            btnHinzufuegen.IsEnabled = fieldFilled;
        }

        /// <summary>
        /// Speichert die neue Kostenstelle asynchron in der Datenbank
        /// </summary>
        private async void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Validierung vor dem Speichern
            if (string.IsNullOrWhiteSpace(txtBezeichnung.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Bezeichnung ein.",
                    "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBezeichnung.Focus();
                return;
            }

            try
            {
                // Button während des Speichervorgangs deaktivieren
                btnHinzufuegen.IsEnabled = false;
                btnHinzufuegen.Content = "Speichert...";

                // Neue Kostenstelle erstellen
                var neueKostenstelle = new Kostenstelle
                {
                    Bezeichnung = txtBezeichnung.Text.Trim()
                };

                // Asynchron in Datenbank speichern
                using (var context = new LagerContext())
                {
                    context.Kostenstellen.Add(neueKostenstelle);
                    await context.SaveChangesAsync();
                }

                // Erfolgsmeldung anzeigen
                MessageBox.Show("Kostenstelle wurde erfolgreich hinzugefügt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Fenster schließen und Erfolg signalisieren
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                // Fehlermeldung bei Datenbankproblemen
                MessageBox.Show($"Fehler beim Speichern der Kostenstelle: {ex.Message}",
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
            this.DialogResult = false;
            this.Close();
        }
    }
}