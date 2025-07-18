using System;
using System.Windows;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LAGA
{
    /// <summary>
    /// Modales Fenster für die Datenbanksicherung-Einstellungen
    /// Ermöglicht die Auswahl eines benutzerdefinierten Backup-Pfads oder Verwendung des Standard-Pfads
    /// Speichert die Einstellungen automatisch in einer JSON-Datei im Einstellungen-Ordner
    /// </summary>
    public partial class DatenbanksicherungEinstellungen : Window
    {
        /// <summary>
        /// Aktuell geladene Backup-Einstellungen
        /// </summary>
        private BackupEinstellungsService.BackupEinstellungen? _aktuelleEinstellungen;

        /// <summary>
        /// Konstruktor - Initialisiert das Fenster und lädt die aktuellen Einstellungen
        /// </summary>
        public DatenbanksicherungEinstellungen()
        {
            InitializeComponent();

            // Fenster-Eigenschaften setzen
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Einstellungen laden (asynchron)
            this.Loaded += async (sender, e) => await EinstellungenLadenAsync();
        }

        /// <summary>
        /// Lädt die aktuellen Backup-Einstellungen und zeigt sie in der UI an
        /// </summary>
        private async Task EinstellungenLadenAsync()
        {
            try
            {
                ZeigeStatus("Lade Backup-Einstellungen...", StatusTyp.Info);

                // Aktuelle Einstellungen laden
                _aktuelleEinstellungen = await BackupEinstellungsService.EinstellungenLadenAsync();

                // Aktuellen Backup-Pfad anzeigen
                string aktuellerPfad = await BackupEinstellungsService.AktuellenBackupPfadHolenAsync();
                TxtBackupPfad.Text = aktuellerPfad;

                // Status aktualisieren
                if (_aktuelleEinstellungen?.BenutzerdefiniertePfad != null)
                {
                    ZeigeStatus($"Benutzerdefinierter Backup-Pfad aktiv", StatusTyp.Erfolg);
                }
                else
                {
                    ZeigeStatus("Standard-Backup-Pfad wird verwendet", StatusTyp.Info);
                }
            }
            catch (Exception ex)
            {
                ZeigeStatus($"Fehler beim Laden der Einstellungen: {ex.Message}", StatusTyp.Fehler);
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Laden der Backup-Einstellungen: {ex.Message}");
            }
        }

        /// <summary>
        /// Event-Handler: Benutzer wählt einen neuen Backup-Pfad
        /// Verwendet Windows API Code Pack für optimale Ordnerauswahl
        /// </summary>
        private async void BtnPfadWaehlen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // CommonOpenFileDialog für Ordnerauswahl verwenden
                using (var folderDialog = new CommonOpenFileDialog())
                {
                    folderDialog.Title = "Wählen Sie einen Ordner für die Datenbank-Backups";
                    folderDialog.IsFolderPicker = true;
                    folderDialog.AllowNonFileSystemItems = false;
                    folderDialog.Multiselect = false;

                    // Aktuellen Pfad als Startpunkt setzen (falls benutzerdefiniert)
                    if (_aktuelleEinstellungen?.BenutzerdefiniertePfad != null &&
                        Directory.Exists(_aktuelleEinstellungen.BenutzerdefiniertePfad))
                    {
                        folderDialog.InitialDirectory = _aktuelleEinstellungen.BenutzerdefiniertePfad;
                    }

                    // Dialog anzeigen
                    CommonFileDialogResult result = folderDialog.ShowDialog(this);

                    if (result == CommonFileDialogResult.Ok)
                    {
                        string gewaehlterPfad = folderDialog.FileName;

                        // Pfad validieren
                        if (!BackupEinstellungsService.IstPfadGueltig(gewaehlterPfad))
                        {
                            ZeigeStatus("Der gewählte Pfad ist nicht gültig oder nicht beschreibbar", StatusTyp.Fehler);
                            System.Windows.MessageBox.Show(
                                "Der gewählte Pfad ist nicht gültig oder es können keine Dateien erstellt werden.\n\n" +
                                "Bitte wählen Sie einen anderen Pfad.",
                                "Ungültiger Pfad",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Neue Einstellungen speichern
                        await NeueEinstellungenSpeichernAsync(gewaehlterPfad);
                    }
                }
            }
            catch (Exception ex)
            {
                ZeigeStatus($"Fehler bei der Pfad-Auswahl: {ex.Message}", StatusTyp.Fehler);
                System.Diagnostics.Debug.WriteLine($"❌ Fehler bei Pfad-Auswahl: {ex.Message}");

                // Fallback zu einfacher Eingabe
                await FallbackPfadEingabeAsync();
            }
        }

        /// <summary>
        /// Fallback-Methode für Pfad-Eingabe falls der Dialog nicht funktioniert
        /// </summary>
        private async Task FallbackPfadEingabeAsync()
        {
            try
            {
                // Einfaches Eingabefenster
                var eingabeDialog = new PfadEingabeDialog(_aktuelleEinstellungen?.BenutzerdefiniertePfad);
                eingabeDialog.Owner = this;

                bool? result = eingabeDialog.ShowDialog();

                if (result == true && !string.IsNullOrWhiteSpace(eingabeDialog.EingegebenerPfad))
                {
                    string pfad = eingabeDialog.EingegebenerPfad.Trim();

                    if (BackupEinstellungsService.IstPfadGueltig(pfad))
                    {
                        await NeueEinstellungenSpeichernAsync(pfad);
                    }
                    else
                    {
                        ZeigeStatus("Eingegebener Pfad ist nicht gültig", StatusTyp.Fehler);
                    }
                }
            }
            catch (Exception ex)
            {
                ZeigeStatus($"Fehler bei der Pfad-Eingabe: {ex.Message}", StatusTyp.Fehler);
                System.Diagnostics.Debug.WriteLine($"❌ Fehler bei Fallback-Eingabe: {ex.Message}");
            }
        }

        /// <summary>
        /// Event-Handler: Einstellungen auf Standard zurücksetzen
        /// </summary>
        private async void BtnStandardZuruecksetzen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Bestätigung vom Benutzer
                var result = System.Windows.MessageBox.Show(
                    "Möchten Sie die Backup-Einstellungen auf den Standard-Pfad zurücksetzen?\n\n" +
                    "Vorhandene Backups bleiben erhalten.",
                    "Einstellungen zurücksetzen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ZeigeStatus("Setze Einstellungen zurück...", StatusTyp.Info);

                    // Einstellungen löschen (= Standard verwenden)
                    bool erfolg = await BackupEinstellungsService.EinstellungenZuruecksetzenAsync();

                    if (erfolg)
                    {
                        // UI aktualisieren
                        _aktuelleEinstellungen = null;
                        TxtBackupPfad.Text = PathHelper.BackupDirectory;
                        ZeigeStatus("Standard-Backup-Pfad wird verwendet", StatusTyp.Erfolg);
                    }
                    else
                    {
                        ZeigeStatus("Fehler beim Zurücksetzen der Einstellungen", StatusTyp.Fehler);
                    }
                }
            }
            catch (Exception ex)
            {
                ZeigeStatus($"Fehler beim Zurücksetzen: {ex.Message}", StatusTyp.Fehler);
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Zurücksetzen: {ex.Message}");
            }
        }

        /// <summary>
        /// Event-Handler: Fenster schließen
        /// </summary>
        private void BtnSchliessen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Speichert neue Backup-Einstellungen und aktualisiert die UI
        /// </summary>
        /// <param name="neuerPfad">Neuer benutzerdefinierter Backup-Pfad</param>
        private async Task NeueEinstellungenSpeichernAsync(string neuerPfad)
        {
            try
            {
                ZeigeStatus("Speichere neue Einstellungen...", StatusTyp.Info);

                // Neue Einstellungen erstellen
                var neueEinstellungen = new BackupEinstellungsService.BackupEinstellungen
                {
                    BenutzerdefiniertePfad = neuerPfad
                };

                // Speichern
                bool erfolg = await BackupEinstellungsService.EinstellungenSpeichernAsync(neueEinstellungen);

                if (erfolg)
                {
                    // UI aktualisieren
                    _aktuelleEinstellungen = neueEinstellungen;

                    // Neuen Backup-Pfad anzeigen (mit "LAGA Backup" Unterordner)
                    string neuerBackupPfad = Path.Combine(neuerPfad, "LAGA Backup");
                    TxtBackupPfad.Text = neuerBackupPfad;

                    ZeigeStatus($"Neuer Backup-Pfad gespeichert", StatusTyp.Erfolg);
                }
                else
                {
                    ZeigeStatus("Fehler beim Speichern der Einstellungen", StatusTyp.Fehler);
                }
            }
            catch (Exception ex)
            {
                ZeigeStatus($"Fehler beim Speichern: {ex.Message}", StatusTyp.Fehler);
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }

        /// <summary>
        /// Zeigt eine Status-Nachricht in der UI an
        /// </summary>
        /// <param name="nachricht">Anzuzeigende Nachricht</param>
        /// <param name="typ">Typ der Nachricht (Info, Erfolg, Fehler)</param>
        private void ZeigeStatus(string nachricht, StatusTyp typ)
        {
            try
            {
                // Status-Text setzen
                TxtStatus.Text = nachricht;

                // Farbe je nach Typ setzen
                switch (typ)
                {
                    case StatusTyp.Info:
                        TxtStatus.Foreground = System.Windows.Media.Brushes.DarkBlue;
                        break;
                    case StatusTyp.Erfolg:
                        TxtStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
                        break;
                    case StatusTyp.Fehler:
                        TxtStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
                        break;
                }

                // Debug-Ausgabe
                System.Diagnostics.Debug.WriteLine($"Status: {nachricht}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Anzeigen des Status: {ex.Message}");
            }
        }

        /// <summary>
        /// Enum für Status-Typen
        /// </summary>
        private enum StatusTyp
        {
            Info,
            Erfolg,
            Fehler
        }
    }

    /// <summary>
    /// Einfaches Dialog-Fenster für Pfad-Eingabe als Fallback
    /// Wird verwendet falls WindowsAPICodePack nicht verfügbar ist
    /// </summary>
    public class PfadEingabeDialog : Window
    {
        private System.Windows.Controls.TextBox textBox;
        public string EingegebenerPfad { get; private set; } = "";

        public PfadEingabeDialog(string? initialPfad = null)
        {
            Title = "Backup-Pfad eingeben";
            Width = 500;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.Margin = new Thickness(20);

            var label = new System.Windows.Controls.Label
            {
                Content = "Geben Sie den vollständigen Pfad für die Backup-Ordner ein:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            System.Windows.Controls.Grid.SetRow(label, 0);

            textBox = new System.Windows.Controls.TextBox
            {
                Text = initialPfad ?? "",
                Margin = new Thickness(0, 0, 0, 20),
                Padding = new Thickness(5)
            };
            System.Windows.Controls.Grid.SetRow(textBox, 1);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) => { EingegebenerPfad = textBox.Text; DialogResult = true; };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Abbrechen",
                Width = 75,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => { DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}
