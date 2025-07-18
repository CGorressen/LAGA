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
    /// 
    /// WICHTIG: Garantiert dass MainWindow erst öffnet wenn ALLE Prozesse wirklich abgeschlossen sind!
    /// Kein paralleles Verarbeiten mehr - alles wird sequenziell abgearbeitet für maximale Sicherheit.
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
        /// GARANTIERT: MainWindow öffnet erst wenn alle Schritte wirklich abgeschlossen sind
        /// </summary>
        private async Task StartInitializationAsync()
        {
            try
            {
                // Schritt 1: Ordnerstruktur erstellen (schnell, deterministisch)
                UpdateProgress(5, "Erstelle Anwendungsordner...");
                await Task.Delay(100); // UI-Update ermöglichen
                InitializeApplicationDirectories();

                UpdateProgress(10, "Ordnerstruktur bereit");
                await Task.Delay(200); // Kurz anzeigen

                // Schritt 2: Backup durchführen (kann länger dauern, mit echtem Progress)
                UpdateProgress(15, "Starte Datenbank-Backup...");
                await PerformRealBackupWithProgressAsync();

                // Schritt 3: Datenbank initialisieren (kann bei Netzwerk länger dauern!)
                UpdateProgress(85, "Initialisiere Datenbank...");
                await InitializeDatabaseWithMonitoringAsync();

                // Schritt 4: Finalisierung
                UpdateProgress(95, "Lade Benutzeroberfläche...");
                await Task.Delay(300);

                // Schritt 5: Fertig - MainWindow öffnen
                UpdateProgress(100, "LAGA ist bereit!");
                await Task.Delay(500); // Kurz "bereit" anzeigen

                // MainWindow öffnen und dieses Fenster schließen
                // ERST JETZT ist garantiert dass alle Prozesse wirklich abgeschlossen sind!
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
        /// Führt das echte Backup mit Progress-Callbacks durch
        /// KEINE SIMULATION MEHR - alles basiert auf echten Datei-Operationen
        /// </summary>
        private async Task PerformRealBackupWithProgressAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Starte echtes Backup mit Progress-Tracking...");

                // Progress-Callback erstellen der unseren UI-Progress updated
                var progress = new Progress<(int percent, string status)>(update =>
                {
                    // Backup läuft von 15% bis 85% des Gesamtfortschritts
                    // update.percent kommt von BackupService (20-80), wir mappen das auf 15-85
                    int mappedPercent = 15 + ((update.percent - 20) * 70 / 60);
                    mappedPercent = Math.Max(15, Math.Min(85, mappedPercent)); // Sicherheitsbegrenzung

                    UpdateProgress(mappedPercent, update.status);
                    System.Diagnostics.Debug.WriteLine($"🔄 Backup Progress: {mappedPercent}% - {update.status}");
                });

                // Echtes Backup mit Progress-Callbacks ausführen
                bool backupErfolg = await BackupService.AutomatischesBackupAsync(progress);

                if (backupErfolg)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Echtes Backup erfolgreich abgeschlossen");
                    UpdateProgress(85, "Backup erfolgreich erstellt");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Backup fehlgeschlagen - nicht kritisch");
                    UpdateProgress(85, "Backup fehlgeschlagen (nicht kritisch)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Fehler beim Backup: {ex.Message}");
                UpdateProgress(85, "Backup-Fehler (nicht kritisch)");
                // Backup-Fehler sind nicht kritisch - Anwendung kann trotzdem starten
            }
        }

        /// <summary>
        /// Initialisiert die SQLite-Datenbank mit kontinuierlichem Monitoring
        /// Überwacht den Prozess kontinuierlich bis er WIRKLICH abgeschlossen ist
        /// </summary>
        private async Task InitializeDatabaseWithMonitoringAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Starte Datenbank-Initialisierung mit Monitoring...");

                // Datenbank-Initialisierung als Task starten
                var dbInitTask = InitializeDatabaseAsync();

                // Progress-Monitoring während die DB-Init läuft
                int currentProgress = 85;
                var progressMessages = new[]
                {
                    "Verbinde mit Datenbank...",
                    "Prüfe Datenbankstruktur...",
                    "Erstelle Tabellen...",
                    "Konfiguriere Indizes...",
                    "Finalisiere Datenbank..."
                };

                int messageIndex = 0;

                // Überwache den Datenbank-Task kontinuierlich
                while (!dbInitTask.IsCompleted)
                {
                    // Aktualisiere Status-Message alle 800ms
                    if (messageIndex < progressMessages.Length)
                    {
                        UpdateProgress(currentProgress, progressMessages[messageIndex]);
                        messageIndex++;
                        currentProgress = Math.Min(94, currentProgress + 2); // Langsam von 85% auf 94%
                    }
                    else
                    {
                        // Falls es länger dauert, zeige generische Message
                        UpdateProgress(currentProgress, "Datenbank wird konfiguriert...");
                    }

                    await Task.Delay(800); // Alle 800ms Status update
                }

                // Task ist completed - auf Result warten um Exception zu propagieren
                await dbInitTask;

                UpdateProgress(95, "Datenbank erfolgreich initialisiert");
                System.Diagnostics.Debug.WriteLine("✅ Datenbank-Initialisierung wirklich abgeschlossen");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Kritischer Fehler bei Datenbank-Initialisierung: {ex.Message}");
                throw; // Datenbank-Fehler sind kritisch!
            }
        }

        /// <summary>
        /// Initialisiert die SQLite-Datenbank (ursprüngliche Methode)
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using (var context = new LagerContext())
                {
                    // Das kann bei Netzwerk/USB-Speicher sehr lange dauern!
                    await context.Database.EnsureCreatedAsync();
                }
                System.Diagnostics.Debug.WriteLine($"✅ Datenbank erfolgreich initialisiert: {PathHelper.DatabaseFilePath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fehler beim Initialisieren der Datenbank: {ex.Message}", ex);
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
                // Sicherheitsbegrenzung für Progress-Werte
                progress = Math.Max(0, Math.Min(100, progress));

                // Fortschrittsbalken aktualisieren
                progressBar.Value = progress;

                // Prozent-Anzeige aktualisieren
                txtProgress.Text = $"{progress}%";

                // Status-Text aktualisieren
                txtStatus.Text = status;

                // Debug-Ausgabe
                System.Diagnostics.Debug.WriteLine($"🔄 Loading: {progress}% - {status}");

                // UI-Update erzwingen für sofortige Sichtbarkeit
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
        /// Öffnet das MainWindow und schließt das Loading-Fenster
        /// WIRD NUR AUFGERUFEN wenn alle Initialisierungsschritte wirklich abgeschlossen sind!
        /// </summary>
        private async Task OpenMainWindowAndCloseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 Öffne MainWindow - alle Initialisierungen sind abgeschlossen!");

                // MainWindow erstellen
                var mainWindow = new MainWindow();

                // WICHTIG: MainWindow als Application.Current.MainWindow setzen
                // Dadurch funktioniert die Navigation zurück ins StartFenster
                Application.Current.MainWindow = mainWindow;

                // MainWindow anzeigen
                mainWindow.Show();

                // Kurze Pause für smooth Transition
                await Task.Delay(200);

                // Loading-Fenster schließen
                this.Close();

                System.Diagnostics.Debug.WriteLine("✅ Loading-Fenster geschlossen, MainWindow ist aktiv");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fehler beim Öffnen des MainWindow: {ex.Message}");
                MessageBox.Show(
                    $"Fehler beim Öffnen der Hauptanwendung:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
            }
        }
    }
}