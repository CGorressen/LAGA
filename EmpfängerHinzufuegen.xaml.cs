using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster zum Hinzufügen neuer E-Mail-Empfänger
    /// Nach erfolgreichem Speichern wird der Dialog automatisch geschlossen
    /// Orientiert sich an der Struktur von KostenstelleHinzufuegen
    /// </summary>
    public partial class EmpfängerHinzufuegen : Window
    {
        /// <summary>
        /// Regex-Pattern für E-Mail-Validierung
        /// Überprüft grundlegende E-Mail-Format-Anforderungen
        /// </summary>
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public EmpfängerHinzufuegen()
        {
            InitializeComponent();

            // Fokus auf das Eingabefeld setzen
            Loaded += (s, e) => txtEmail.Focus();
        }

        /// <summary>
        /// Wird ausgelöst wenn sich der Inhalt des E-Mail-Eingabefelds ändert
        /// Validiert die E-Mail-Adresse in Echtzeit und aktiviert/deaktiviert den Button
        /// </summary>
        private void TxtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEmailInput();
        }

        /// <summary>
        /// Behandelt Enter-Taste im E-Mail-Eingabefeld
        /// Führt Hinzufügen-Aktion aus wenn Button aktiviert ist
        /// </summary>
        private void TxtEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnHinzufuegen.IsEnabled)
            {
                BtnHinzufuegen_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Validiert die E-Mail-Eingabe und aktualisiert UI-Status
        /// Prüft Format und zeigt entsprechende Fehlermeldungen an
        /// </summary>
        private void ValidateEmailInput()
        {
            string email = txtEmail.Text.Trim();

            // Leeres Feld - Button deaktivieren, keine Fehlermeldung
            if (string.IsNullOrWhiteSpace(email))
            {
                btnHinzufuegen.IsEnabled = false;
                HideError();
                return;
            }

            // E-Mail-Format prüfen
            if (!IsValidEmail(email))
            {
                btnHinzufuegen.IsEnabled = false;
                ShowError("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
                return;
            }

            // Gültige E-Mail - Button aktivieren, Fehler verstecken
            btnHinzufuegen.IsEnabled = true;
            HideError();
        }

        /// <summary>
        /// Überprüft ob die E-Mail-Adresse ein gültiges Format hat
        /// </summary>
        /// <param name="email">Zu prüfende E-Mail-Adresse</param>
        /// <returns>True wenn E-Mail-Format gültig ist</returns>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Regex-Validierung für grundlegendes E-Mail-Format
                return EmailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Zeigt eine Fehlermeldung unter dem Eingabefeld an
        /// </summary>
        /// <param name="nachricht">Fehlermeldungstext</param>
        private void ShowError(string nachricht)
        {
            txtFehler.Text = nachricht;
            txtFehler.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Versteckt die Fehlermeldung
        /// </summary>
        private void HideError()
        {
            txtFehler.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Speichert den neuen Empfänger asynchron in der Datenbank
        /// Nach erfolgreichem Speichern wird der Dialog automatisch geschlossen
        /// </summary>
        private async void BtnHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            // Finale Validierung vor dem Speichern
            if (!ValidateBeforeSave())
            {
                return;
            }

            // Eindeutigkeit der E-Mail-Adresse in der Datenbank prüfen
            if (!await ValidateEmailUniqueAsync())
            {
                return;
            }

            try
            {
                // Button während des Speichervorgangs deaktivieren
                btnHinzufuegen.IsEnabled = false;
                btnHinzufuegen.Content = "Speichert...";

                // Neuen Empfänger erstellen
                var neuerEmpfaenger = new Empfaenger
                {
                    Email = txtEmail.Text.Trim().ToLower() // E-Mail in Kleinbuchstaben speichern
                };

                // Asynchron in Datenbank speichern
                using (var context = new LagerContext())
                {
                    context.Empfaenger.Add(neuerEmpfaenger);
                    await context.SaveChangesAsync();
                }

                // Erfolgsmeldung anzeigen
                MessageBox.Show("Empfänger wurde erfolgreich hinzugefügt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Dialog schließen und Erfolg signalisieren
                this.DialogResult = true;
                this.Close();
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
            {
                // Eindeutigkeits-Constraint verletzt (sollte durch vorherige Prüfung verhindert werden)
                ShowError("Diese E-Mail-Adresse ist bereits registriert.");
                txtEmail.Focus();
                txtEmail.SelectAll();
            }
            catch (Exception ex)
            {
                // Allgemeine Datenbankfehler
                MessageBox.Show($"Fehler beim Speichern des Empfängers: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Button wieder aktivieren
                btnHinzufuegen.Content = "Hinzufügen";
                ValidateEmailInput(); // Status neu bewerten
            }
        }

        /// <summary>
        /// Finale Validierung vor dem Speichern
        /// </summary>
        /// <returns>True wenn alle Validierungen erfolgreich sind</returns>
        private bool ValidateBeforeSave()
        {
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Bitte geben Sie eine E-Mail-Adresse ein.");
                txtEmail.Focus();
                return false;
            }

            if (!IsValidEmail(email))
            {
                ShowError("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
                txtEmail.Focus();
                txtEmail.SelectAll();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prüft asynchron ob die E-Mail-Adresse bereits in der Datenbank existiert
        /// </summary>
        /// <returns>True wenn E-Mail eindeutig ist (noch nicht vorhanden)</returns>
        private async Task<bool> ValidateEmailUniqueAsync()
        {
            try
            {
                string email = txtEmail.Text.Trim().ToLower();

                using (var context = new LagerContext())
                {
                    // Prüfen ob E-Mail bereits existiert (case-insensitive)
                    var exists = await context.Empfaenger
                        .AnyAsync(e => e.Email.ToLower() == email);

                    if (exists)
                    {
                        ShowError("Diese E-Mail-Adresse ist bereits registriert.");
                        txtEmail.Focus();
                        txtEmail.SelectAll();
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Validierung: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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