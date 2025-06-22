using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Text;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Google.Apis.Auth.OAuth2.Flows;

namespace LAGA
{
    /// <summary>
    /// Service für die Kommunikation mit der Gmail API
    /// Erweiterbare Struktur für Test-E-Mails und Warn-E-Mails
    /// </summary>
    public static class GmailEmailService
    {
        /// <summary>
        /// Bereiche für Gmail API - nur Senden von E-Mails erforderlich
        /// </summary>
        private static readonly string[] Scopes = { GmailService.Scope.GmailSend };

        /// <summary>
        /// Name der Anwendung für Gmail API
        /// </summary>
        private static readonly string ApplicationName = "LAGA Lagerverwaltung";

        /// <summary>
        /// Sucht automatisch nach JSON-Credentials-Dateien im Credentials-Ordner
        /// </summary>
        private static string? FindCredentialsFile()
        {
            try
            {
                // Alle JSON-Dateien im Credentials-Ordner suchen
                var jsonFiles = Directory.GetFiles(PathHelper.CredentialsDirectory, "*.json");

                if (jsonFiles.Length > 0)
                {
                    // Bevorzuge Dateien mit "client_secret" im Namen (typisch für OAuth-Credentials)
                    var clientSecretFiles = jsonFiles.Where(f => Path.GetFileName(f).Contains("client_secret")).ToArray();
                    if (clientSecretFiles.Length > 0)
                    {
                        return clientSecretFiles[0];
                    }

                    // Falls keine client_secret Datei gefunden, nimm die erste JSON-Datei
                    return jsonFiles[0];
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Vollständiger Pfad zur automatisch gefundenen Credentials-Datei
        /// </summary>
        private static string? CredentialsFilePath => FindCredentialsFile();

        /// <summary>
        /// Prüft ob eine Gmail API Credentials-Datei vorhanden ist
        /// Sucht automatisch nach JSON-Dateien im Credentials-Ordner
        /// </summary>
        /// <returns>True wenn eine Credentials-Datei existiert</returns>
        public static bool AreCredentialsAvailable()
        {
            return !string.IsNullOrEmpty(CredentialsFilePath);
        }

        /// <summary>
        /// Erstellt und authentifiziert den Gmail Service
        /// Optimiert für OAuth 2.0 Client Credentials (Desktop-Anwendung)
        /// </summary>
        /// <returns>Authentifizierter Gmail Service oder null bei Fehler</returns>
        private static async Task<GmailService?> CreateGmailServiceAsync()
        {
            try
            {
                // Prüfen ob Credentials-Datei existiert
                var credentialsPath = CredentialsFilePath;
                if (string.IsNullOrEmpty(credentialsPath))
                {
                    throw new FileNotFoundException($"Keine JSON-Credentials-Datei im Ordner {PathHelper.CredentialsDirectory} gefunden");
                }

                // OAuth 2.0 Flow für Desktop-Anwendung
                UserCredential userCredential;
                using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        System.Threading.CancellationToken.None);
                }

                // Gmail Service mit User Credentials erstellen
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = userCredential,
                    ApplicationName = ApplicationName,
                });

                return service;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Erstellen des Gmail Service: {ex.Message}");

                // Spezifische Fehlermeldung für häufige Probleme
                if (ex.Message.Contains("redirect_uri_mismatch"))
                {
                    throw new Exception("OAuth-Konfigurationsfehler: Redirect URI stimmt nicht überein. " +
                                       "Bitte überprüfen Sie Ihre Google Cloud Console Einstellungen.");
                }
                else if (ex.Message.Contains("invalid_client"))
                {
                    throw new Exception("Ungültige Client-Credentials. Bitte überprüfen Sie Ihre credentials.json Datei.");
                }
                else if (ex.Message.Contains("access_denied"))
                {
                    throw new Exception("Zugriff verweigert. Bitte gewähren Sie der Anwendung die erforderlichen Berechtigungen.");
                }

                throw;
            }
        }

        /// <summary>
        /// Sendet eine Test-E-Mail an alle registrierten Empfänger
        /// </summary>
        /// <returns>True wenn alle E-Mails erfolgreich gesendet wurden</returns>
        public static async Task<bool> SendeTestEmailAsync()
        {
            try
            {
                // Gmail Service erstellen
                var service = await CreateGmailServiceAsync();
                if (service == null)
                {
                    throw new InvalidOperationException("Gmail Service konnte nicht erstellt werden.");
                }

                try
                {
                    // Alle Empfänger aus der Datenbank laden
                    List<string> empfaengerEmails;
                    using (var context = new LagerContext())
                    {
                        empfaengerEmails = await context.Empfaenger
                            .Select(e => e.Email)
                            .ToListAsync();
                    }

                    if (!empfaengerEmails.Any())
                    {
                        MessageBox.Show("Es sind keine Empfänger für Test-E-Mails registriert.\n\n" +
                                       "Bitte fügen Sie zuerst Empfänger hinzu.",
                                       "Keine Empfänger", MessageBoxButton.OK, MessageBoxImage.Information);
                        return false;
                    }

                    // Test-E-Mail-Inhalt definieren
                    string betreff = "LAGA Test-E-Mail";
                    string nachricht = "Dies ist eine automatisch generierte Testnachricht des Lagerverwaltungssystems LAGA.\n\n" +
                                       "Eine Antwort auf diese Nachricht ist nicht erforderlich.\n\n" +
                                       "Mit freundlichen Grüßen\n" +
                                       "Ihr LAGA-System.";


                    // E-Mail an alle Empfänger senden
                    int erfolgreicheEmails = 0;
                    var fehlerListe = new List<string>();

                    foreach (string empfaengerEmail in empfaengerEmails)
                    {
                        try
                        {
                            bool erfolg = await SendeEmailAsync(service, empfaengerEmail, betreff, nachricht);
                            if (erfolg)
                            {
                                erfolgreicheEmails++;
                            }
                            else
                            {
                                fehlerListe.Add(empfaengerEmail);
                            }

                            // Kurze Pause zwischen E-Mails um API-Limits zu vermeiden
                            await Task.Delay(200);
                        }
                        catch (Exception ex)
                        {
                            fehlerListe.Add($"{empfaengerEmail} ({ex.Message})");
                            System.Diagnostics.Debug.WriteLine($"Fehler beim Senden an {empfaengerEmail}: {ex.Message}");
                        }
                    }

                    // Ergebnis-Meldung anzeigen
                    if (erfolgreicheEmails == empfaengerEmails.Count)
                    {
                        MessageBox.Show($"Test-E-Mail wurde erfolgreich an alle {erfolgreicheEmails} Empfänger gesendet!",
                                       "Test-E-Mail versendet", MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }
                    else if (erfolgreicheEmails > 0)
                    {
                        string fehlerText = string.Join("\n", fehlerListe);
                        MessageBox.Show($"Test-E-Mail wurde an {erfolgreicheEmails} von {empfaengerEmails.Count} Empfängern gesendet.\n\n" +
                                       $"Fehler bei:\n{fehlerText}",
                                       "Teilweise erfolgreich", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return true;
                    }
                    else
                    {
                        string fehlerText = string.Join("\n", fehlerListe);
                        MessageBox.Show($"Test-E-Mail konnte an keinen Empfänger gesendet werden.\n\n" +
                                       $"Fehler:\n{fehlerText}",
                                       "Test-E-Mail fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                finally
                {
                    // Gmail Service ordnungsgemäß freigeben
                    service?.Dispose();
                }
            }
            catch (FileNotFoundException)
            {
                var foundFiles = Directory.GetFiles(PathHelper.CredentialsDirectory, "*.json");
                string filesList = foundFiles.Length > 0
                    ? string.Join("\n", foundFiles.Select(f => "- " + Path.GetFileName(f)))
                    : "Keine JSON-Dateien gefunden";

                MessageBox.Show($"Gmail API Credentials-Datei nicht gefunden!\n\n" +
                               $"Credentials-Ordner: {PathHelper.CredentialsDirectory}\n\n" +
                               $"Gefundene Dateien:\n{filesList}\n\n" +
                               $"Bitte legen Sie eine gültige JSON-Credentials-Datei " +
                               $"(von der Google Cloud Console) in den Credentials-Ordner.",
                               "Credentials fehlen", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Senden der Test-E-Mail:\n\n{ex.Message}",
                               "Gmail API Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Sendet eine Warn-E-Mail für einen Artikel mit niedrigem Bestand an alle registrierten Empfänger
        /// E-Mail wird im Plain Text Format erstellt entsprechend der Dokumentation
        /// </summary>
        /// <param name="artikel">Artikel mit niedrigem Bestand</param>
        /// <param name="aktuellerBestand">Aktueller Bestand des Artikels</param>
        /// <returns>True wenn alle E-Mails erfolgreich gesendet wurden</returns>
        public static async Task<bool> SendeWarnEmailAsync(Artikel artikel, int aktuellerBestand)
        {
            try
            {
                // Gmail Service erstellen
                var service = await CreateGmailServiceAsync();
                if (service == null)
                {
                    throw new InvalidOperationException("Gmail Service konnte nicht erstellt werden.");
                }

                try
                {
                    // Alle Empfänger aus der Datenbank laden
                    List<string> empfaengerEmails;
                    using (var context = new LagerContext())
                    {
                        empfaengerEmails = await context.Empfaenger
                            .Select(e => e.Email)
                            .ToListAsync();
                    }

                    if (!empfaengerEmails.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("Keine Empfänger für Warn-E-Mails registriert");
                        return false;
                    }

                    // Warn-E-Mail-Inhalt erstellen
                    string betreff = $"LAGA Warnung: {artikel.Bezeichnung} - Mindestbestand erreicht";
                    string nachricht = ErstelleWarnEmailInhalt(artikel, aktuellerBestand);

                    // E-Mail an alle Empfänger senden
                    int erfolgreicheEmails = 0;
                    var fehlerListe = new List<string>();

                    foreach (string empfaengerEmail in empfaengerEmails)
                    {
                        try
                        {
                            bool erfolg = await SendeEmailAsync(service, empfaengerEmail, betreff, nachricht);
                            if (erfolg)
                            {
                                erfolgreicheEmails++;
                            }
                            else
                            {
                                fehlerListe.Add(empfaengerEmail);
                            }

                            // Kurze Pause zwischen E-Mails um API-Limits zu vermeiden
                            await Task.Delay(200);
                        }
                        catch (Exception ex)
                        {
                            fehlerListe.Add($"{empfaengerEmail} ({ex.Message})");
                            System.Diagnostics.Debug.WriteLine($"Fehler beim Senden der Warn-E-Mail an {empfaengerEmail}: {ex.Message}");
                        }
                    }

                    // Ergebnis bewerten
                    bool alleSentSuccessfully = erfolgreicheEmails == empfaengerEmails.Count;

                    System.Diagnostics.Debug.WriteLine($"Warn-E-Mail für '{artikel.Bezeichnung}': {erfolgreicheEmails}/{empfaengerEmails.Count} erfolgreich");

                    return alleSentSuccessfully;
                }
                finally
                {
                    // Gmail Service ordnungsgemäß freigeben
                    service?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Senden der Warn-E-Mail: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Erstellt den Inhalt der Warn-E-Mail entsprechend der Dokumentation
        /// Plain Text Format für bessere Spam-Vermeidung
        /// </summary>
        /// <param name="artikel">Der betroffene Artikel</param>
        /// <param name="aktuellerBestand">Der aktuelle Bestand</param>
        /// <returns>Formatierter E-Mail-Text</returns>
        private static string ErstelleWarnEmailInhalt(Artikel artikel, int aktuellerBestand)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("Dies ist eine automatisch generierte Nachricht des Lagerverwaltungssystems LAGA.");
            sb.AppendLine();
            sb.AppendLine("Folgender Artikel hat den Mindestbestand erreicht und muss nachbestellt werden:");
            sb.AppendLine();
            sb.AppendLine("--------------------------------------------------");

            // Artikel-Informationen
            sb.AppendLine(artikel.Bezeichnung);
            sb.AppendLine($"Aktueller Bestand: {aktuellerBestand}");
            sb.AppendLine($"Mindestbestand: {artikel.Mindestbestand}");
            sb.AppendLine($"Maximalbestand: {artikel.Maximalbestand}");

            // Benötigte Einheiten berechnen (Maximalbestand - aktueller Bestand)
            int benoetigteEinheiten = Math.Abs(artikel.Maximalbestand - aktuellerBestand);
            sb.AppendLine($"Benötigte Einheiten: {benoetigteEinheiten}");
            sb.AppendLine("--------------------------------------------------");

            // Kostenstelle
            sb.AppendLine($"Kostenstelle: {artikel.Kostenstelle?.Bezeichnung ?? "Unbekannt"}");
            sb.AppendLine();

            // Lieferanten-Informationen
            if (artikel.Lieferant != null)
            {
                sb.AppendLine($"Lieferant: {artikel.Lieferant.Bezeichnung}");
                sb.AppendLine($"Webseite: {artikel.Lieferant.Webseite}");
                sb.AppendLine($"E-Mail: {artikel.Lieferant.Email}");
                sb.AppendLine($"Telefon: {artikel.Lieferant.Telefon}");
                sb.AppendLine($"Artikelnummer: {artikel.ExterneArtikelIdLieferant}");
                sb.AppendLine();
            }

            // Hersteller-Informationen
            if (artikel.Hersteller != null)
            {
                sb.AppendLine($"Hersteller: {artikel.Hersteller.Bezeichnung}");
                sb.AppendLine($"Webseite: {artikel.Hersteller.Webseite}");
                sb.AppendLine($"E-Mail: {artikel.Hersteller.Email}");
                sb.AppendLine($"Telefon: {artikel.Hersteller.Telefon}");
                sb.AppendLine($"Artikelnummer: {artikel.ExterneArtikelIdHersteller}");
            }

            sb.AppendLine();
            sb.AppendLine("Mit freundlichen Grüßen");
            sb.AppendLine("Ihr LAGA-System");

            return sb.ToString();
        }

        /// <summary>
        /// Sendet eine einzelne E-Mail über Gmail API
        /// Erweiterbare Methode für verschiedene E-Mail-Typen
        /// </summary>
        /// <param name="service">Authentifizierter Gmail Service</param>
        /// <param name="empfaengerEmail">E-Mail-Adresse des Empfängers</param>
        /// <param name="betreff">E-Mail-Betreff</param>
        /// <param name="nachricht">E-Mail-Nachricht (Plain Text)</param>
        /// <returns>True wenn E-Mail erfolgreich gesendet wurde</returns>
        private static async Task<bool> SendeEmailAsync(GmailService service, string empfaengerEmail, string betreff, string nachricht)
        {
            try
            {
                // MimeMessage erstellen für bessere E-Mail-Formatierung
                var mimeMessage = new MimeMessage();

                // Absender setzen (wird automatisch von Gmail auf den authentifizierten Account gesetzt)
                mimeMessage.From.Add(new MailboxAddress("LAGA System", ""));

                // Empfänger setzen
                mimeMessage.To.Add(new MailboxAddress("", empfaengerEmail));

                // Betreff setzen
                mimeMessage.Subject = betreff;

                // Nachricht setzen (Plain Text)
                var bodyBuilder = new BodyBuilder();
                bodyBuilder.TextBody = nachricht;
                mimeMessage.Body = bodyBuilder.ToMessageBody();

                // MimeMessage in Gmail-kompatibles Format konvertieren
                using var stream = new MemoryStream();
                await mimeMessage.WriteToAsync(stream);
                var rawMessage = Convert.ToBase64String(stream.ToArray())
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                // Gmail Message erstellen
                var gmailMessage = new Message()
                {
                    Raw = rawMessage
                };

                // E-Mail über Gmail API senden
                var request = service.Users.Messages.Send(gmailMessage, "me");
                var result = await request.ExecuteAsync();

                // Erfolgreich wenn Message ID zurückgegeben wird
                return !string.IsNullOrEmpty(result.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Senden der E-Mail an {empfaengerEmail}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gibt Informationen über den Status der Gmail-Konfiguration zurück
        /// Nützlich für Debugging und Setup-Überprüfung
        /// </summary>
        /// <returns>String mit Status-Informationen</returns>
        public static string GetGmailStatusInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Gmail API Konfiguration:");
            sb.AppendLine($"Credentials-Ordner: {PathHelper.CredentialsDirectory}");

            var credentialsPath = CredentialsFilePath;
            if (!string.IsNullOrEmpty(credentialsPath))
            {
                sb.AppendLine($"Gefundene Credentials-Datei: {Path.GetFileName(credentialsPath)}");
                sb.AppendLine($"Vollständiger Pfad: {credentialsPath}");
            }
            else
            {
                sb.AppendLine("Keine Credentials-Datei gefunden");
            }

            sb.AppendLine($"Credentials verfügbar: {AreCredentialsAvailable()}");
            sb.AppendLine($"Anwendungsname: {ApplicationName}");
            sb.AppendLine($"Erforderliche Bereiche: {string.Join(", ", Scopes)}");

            return sb.ToString();
        }
    }
}