using System.Windows;
using System.Windows.Input;

namespace LAGA
{
    /// <summary>
    /// Benutzerdefinierte MessageBox die Enter-Tasteneingaben ignoriert
    /// Verhindert das automatische Schließen durch Scanner-Enter-Eingaben
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        /// <summary>
        /// Constructor für die benutzerdefinierte MessageBox
        /// </summary>
        /// <param name="message">Die anzuzeigende Nachricht</param>
        /// <param name="title">Der Fenstertitel (optional)</param>
        public CustomMessageBox(string message, string title = "Barcode-Fehler")
        {
            InitializeComponent();

            PlayErrorSound();

            // Nachricht und Titel setzen
            txtMessage.Text = message;
            this.Title = title;

            // DEBUG: MessageBox wurde erstellt
            System.Diagnostics.Debug.WriteLine($"🔥 CustomMessageBox erstellt: '{message}'");

            // KRITISCH: Alle Default-Verhalten deaktivieren
            btnOK.IsDefault = false;
            btnOK.IsCancel = false;

            // NEUER ANSATZ: KeyDown UND PreviewKeyDown blockieren
            this.KeyDown += CustomMessageBox_KeyDown;
            this.PreviewKeyDown += CustomMessageBox_PreviewKeyDown;

            

            // Fokus auf das Fenster selbst setzen
            this.Focus();
        }

        /// <summary>
        /// PreviewKeyDown Event - wird VOR OnKeyDown ausgelöst
        /// Hier blockieren wir Enter bereits im "Preview" Stadium
        /// </summary>
        private void CustomMessageBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"📥 PreviewKeyDown: {e.Key}");

            if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {e.Key} BLOCKIERT in PreviewKeyDown");
                e.Handled = true;
            }
        }

        /// <summary>
        /// KeyDown Event - zusätzliche Sicherheit falls PreviewKeyDown nicht ausreicht
        /// </summary>
        private void CustomMessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"📨 KeyDown: {e.Key}");

            if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {e.Key} BLOCKIERT in KeyDown");
                e.Handled = true;
            }
        }

        /// <summary>
        /// Überschreibt die Tastatureingabe-Behandlung
        /// ZUSÄTZLICHE Sicherheitsebene
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🔧 OnKeyDown Override: {e.Key}");

            // Enter und Return komplett ignorieren
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                System.Diagnostics.Debug.WriteLine("❌ Enter-Taste BLOCKIERT in OnKeyDown Override");
                e.Handled = true;
                return; // NIEMALS base.OnKeyDown aufrufen
            }

            // Escape-Taste auch ignorieren
            if (e.Key == Key.Escape)
            {
                System.Diagnostics.Debug.WriteLine("❌ Escape-Taste BLOCKIERT in OnKeyDown Override");
                e.Handled = true;
                return;
            }

            // Alle anderen Tasten normal behandeln
            System.Diagnostics.Debug.WriteLine($"✅ Taste {e.Key} wird normal behandelt");
            base.OnKeyDown(e);
        }

        /// <summary>
        /// OK-Button wurde geklickt - Fenster schließen
        /// Nur so kann das Fenster geschlossen werden
        /// </summary>
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("✅ OK-Button geklickt - CustomMessageBox wird geschlossen");
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Statische Methode zum einfachen Anzeigen der benutzerdefinierten MessageBox
        /// Kann genauso verwendet werden wie MessageBox.Show()
        /// </summary>
        /// <param name="message">Die anzuzeigende Nachricht</param>
        /// <param name="title">Der Fenstertitel (optional)</param>
        /// <param name="owner">Das übergeordnete Fenster (optional)</param>
        /// <returns>True wenn OK geklickt wurde</returns>
        public static bool Show(string message, string title = "Barcode-Fehler", Window? owner = null)
        {
            System.Diagnostics.Debug.WriteLine($"🔥 CustomMessageBox.Show() aufgerufen: '{message}'");

            var dialog = new CustomMessageBox(message, title);

            if (owner != null)
            {
                dialog.Owner = owner;
            }

            System.Diagnostics.Debug.WriteLine("🔥 ShowDialog() wird aufgerufen...");
            var result = dialog.ShowDialog() == true;
            System.Diagnostics.Debug.WriteLine($"🔥 ShowDialog() beendet mit Ergebnis: {result}");

            return result;
        }

        /// <summary>
        /// Überschreibt das Schließen-Event um zu debuggen wann/wie das Fenster geschlossen wird
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ CustomMessageBox wird geschlossen!");
            base.OnClosing(e);
        }

        private void PlayErrorSound()
        {
            try
            {
                // Option 1: Windows System Error Sound (Standard)
                System.Media.SystemSounds.Hand.Play();

                // Alternative Sounds (auskommentiert):
                // System.Media.SystemSounds.Exclamation.Play();  // Weniger dramatisch
                // System.Media.SystemSounds.Beep.Play();         // Einfacher Beep
                // System.Media.SystemSounds.Asterisk.Play();     // Neutraler Sound

                System.Diagnostics.Debug.WriteLine("🔊 Error-Sound abgespielt");
            }
            catch (Exception ex)
            {
                // Sound-Fehler sollten die MessageBox nicht zum Absturz bringen
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Abspielen des Error-Sounds: {ex.Message}");
            }
        }

    }
}