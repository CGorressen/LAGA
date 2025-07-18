using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace LAGA
{
    /// <summary>
    /// Loading-Splash-Fenster das während der Anwendungsinitialisierung angezeigt wird
    /// Zeigt Logo, Fortschritt und Status-Updates während Backup und Datenbankinitialisierung
    /// Schließt sich automatisch und öffnet das MainWindow wenn alles fertig ist
    /// </summary>
    public partial class LoadingSplashWindow : Window
    {
        /// <summary>
        /// Konstruktor - Startet die Initialisierung
        /// </summary>
        public LoadingSplashWindow()
        {
            InitializeComponent();

            // Initialisierung starten sobald das Fenster geladen ist
            this.Loaded += async (sender, e) => await StartInitializationAsync();
        }

        /// <summary>
        /// Führt die komplette Anwendungsinitialisierung durch
        /// </summary>
        private async Task StartInitializationAsync()
        {
            try
            {
                // 1. Ordnerstruktur erstellen (schnell)
                UpdateProgress(10, "Erstelle Anwendungsordner...");
                await Task.Delay(200); // Kurze Pause für UI-Update
                InitializeApplicationDirectories();

                // 2. Backup durchführen (kann länger dauern)
                UpdateProgress(20, "Erstelle Datenbank-Backup...");
                await PerformStartupBackupWithProgressAsync();

                // 3. Datenbank initialisieren
                UpdateProgress(85, "Initialisiere Datenbank...");
                await Task.Delay(200);
                await InitializeDatabaseAsync();

                // 4. Finalisierung
                UpdateProgress(95, "Lade Benutzeroberfläche...");
                await Task.Delay(300);

                // 5. Fertig - MainWindow öffnen
                UpdateProgress(100, "LAGA ist bereit!");
                await Task.Delay(500); // Kurz "bereit" anzeigen

                // MainWindow öffnen und dieses Fenster schließen
                await OpenMainWindowAndCloseAsync();
            }
            catch (Exception ex)
            {
                // Kritischer Fehler während Initialisierung
                UpdateProgress(0, "Fehler beim Laden!");

                MessageBox.Show(
                    $"Fehler beim Initialisieren der Anwendung:\n\n{ex.Message}\n\n" +
                    $"Die Anwendung wird beendet.",
                    "Kritischer Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Aktualisiert Fortschrittsbalken und Status-Text
        /// </summary>
        /// <param name="progress">Fortschritt in Prozent (0-100)</param>
        /// <param name="status">Status-Text</param>
        private void UpdateProgress(int progress, string status)
        {
            try
            {
                // Fortschrittsbalken aktualisieren
                progressBar.Value = progress;

                // Prozent-Anzeige aktualisieren
                txtProgress.Text = $"{progress}%";

                // Status-Text aktualisieren
                txtStatus.Text = status;

                // Debug-Ausgabe
                System.Diagnostics.Debug.WriteLine($"🔄 Loading: {progress}% - {status}");

                // UI-Update erzwingen
                this.UpdateLayout();
                Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Progress-Update: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialisiert die portable Ordnerstruktur der Anwendung
        /// </summary>
        private void InitializeApplicationDirectories()
        {
            try
            {
                // Erstelle alle benötigten Anwendungsordner
                PathHelper.EnsureDirectoriesExist();
                System.Diagnostics.Debug.WriteLine("✅ Anwendungsordner erfolgreich initialisiert");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fehler beim Erstellen der Anwendungsordner: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Führt das Startup-Backup mit Fortschritts-Updates durch
        /// </summary>
        private async Task PerformStartupBackupWithProgressAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Starte automatisches Backup...");

                // Backup mit Timeout (max. 30 Sekunden)
                var backupTask = BackupService.AutomatischesBackupAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));

                // Fortschritt simulieren während Backup läuft
                var progressTask = SimulateBackupProgressAsync();

                var completedTask = await Task.WhenAny(backupTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Backup-Timeout erreicht - überspringe Backup");
                    UpdateProgress(80, "Backup übersprungen (Timeout)");
                    return;
                }

                bool backupErfolg = await backupTask;

                if (backupErfolg)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Automatisches Startup-Backup erfolgreich");
                    UpdateProgress(80, "Backup erfolgreich erstellt");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Automatisches Startup-Backup fehlgeschlagen");
                    UpdateProgress(80, "Backup fehlgeschlagen (nicht kritisch)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Startup-Backup: {ex.Message}");
                UpdateProgress(80, "Backup-Fehler (nicht kritisch)");
            }
        }

        /// <summary>
        /// Simuliert Backup-Fortschritt für bessere Benutzererfahrung
        /// </summary>
        private async Task SimulateBackupProgressAsync()
        {
            try
            {
                // Fortschritt von 20% bis 80% über ca. 3-5 Sekunden verteilen
                for (int i = 20; i <= 80; i += 5)
                {
                    UpdateProgress(i, "Erstelle Datenbank-Backup...");
                    await Task.Delay(300); // Alle 300ms um 5% erhöhen
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler bei Progress-Simulation: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialisiert die SQLite-Datenbank
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    await context.Database.EnsureCreatedAsync();
                }
                System.Diagnostics.Debug.WriteLine($"✅ Datenbank erfolgreich initialisiert: {PathHelper.DatabaseFilePath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fehler bei der Datenbankinitialisierung: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Öffnet das MainWindow und schließt das Splash-Fenster mit Fade-Effekt
        /// </summary>
        private Task OpenMainWindowAndCloseAsync()
        {
            try
            {
                // MainWindow erstellen (aber noch nicht anzeigen)
                var mainWindow = new MainWindow();

                // TaskCompletionSource für async/await Pattern
                var tcs = new TaskCompletionSource<bool>();

                // Fade-Out Animation für Splash-Fenster
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.5)
                };

                fadeOut.Completed += (s, e) =>
                {
                    try
                    {
                        // Nach Fade-Out: MainWindow anzeigen und als Application.Current.MainWindow setzen
                        Application.Current.MainWindow = mainWindow;
                        mainWindow.Show();
                        this.Close();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                };

                // Animation starten
                this.BeginAnimation(Window.OpacityProperty, fadeOut);

                return tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Öffnen des MainWindow: {ex.Message}");

                // Fallback: Direkt ohne Animation
                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                this.Close();

                return Task.CompletedTask;
            }
        }
    }
}