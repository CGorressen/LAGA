using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Common;

// Explizite Namespace-Auflösung für ImageFormat
using SystemImageFormat = System.Drawing.Imaging.ImageFormat;

namespace LAGA
{
    /// <summary>
    /// Service für die Generierung und den Druck von Barcode-Etiketten
    /// Erstellt PDF-Etiketten im Format 40×20mm mit Barcode und Artikelinformationen
    /// </summary>
    public static class BarcodeEtikettService
    {
        /// <summary>
        /// Verzeichnis für die Etikett-PDFs
        /// </summary>
        private static readonly string EtikettenVerzeichnis = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Etiketten");

        /// <summary>
        /// Statischer Konstruktor - initialisiert QuestPDF und erstellt Etikett-Verzeichnis
        /// </summary>
        static BarcodeEtikettService()
        {
            // QuestPDF Community-Lizenz setzen (kostenlos für nicht-kommerzielle Nutzung)
            QuestPDF.Settings.License = LicenseType.Community;

            // Etikett-Verzeichnis erstellen falls es nicht existiert
            Directory.CreateDirectory(EtikettenVerzeichnis);
        }

        /// <summary>
        /// Erstellt und druckt PDF-Etiketten für eine Liste von ArtikelEinheiten
        /// </summary>
        /// <param name="artikelEinheiten">Liste der ArtikelEinheiten für die Etiketten erstellt werden sollen</param>
        /// <param name="artikel">Der zugehörige Artikel mit Bezeichnung</param>
        public static async Task<bool> ErstelleUndDruckeEtikettenAsync(
            List<ArtikelEinheit> artikelEinheiten, Artikel artikel)
        {
            try
            {
                var erfolgreicheEtiketten = new List<string>();

                foreach (var einheit in artikelEinheiten)
                {
                    // PDF-Etikett für jede Einheit erstellen
                    string pdfPfad = await ErstelleEinzelEtikettAsync(einheit, artikel);

                    if (!string.IsNullOrEmpty(pdfPfad))
                    {
                        erfolgreicheEtiketten.Add(pdfPfad);
                    }
                }

                // Alle erstellten Etiketten drucken
                if (erfolgreicheEtiketten.Count > 0)
                {
                    await DruckeEtikettenAsync(erfolgreicheEtiketten);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Etiketten: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt ein einzelnes PDF-Etikett für eine ArtikelEinheit
        /// </summary>
        private static async Task<string> ErstelleEinzelEtikettAsync(ArtikelEinheit einheit, Artikel artikel)
        {
            try
            {
                // Barcode-Bild generieren
                byte[] barcodeBytes = GeneriereBarcodeImage(einheit.Barcode);

                // PDF-Dateiname erstellen
                string dateiname = $"{artikel.Id}_{einheit.Barcode}.pdf";
                string vollstaendigerPfad = Path.Combine(EtikettenVerzeichnis, dateiname);

                // PDF-Dokument erstellen (40×20mm = 113×57 Punkte)
                await Task.Run(() =>
                {
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            // Seitengröße: 40×20mm
                            page.Size(113, 57, Unit.Point);
                            page.Margin(4);

                            page.Content().Column(column =>
                            {
                                // Artikelbezeichnung (gekürzt, max. 10 Zeichen)
                                string kurzeBezeichnung = artikel.Bezeichnung.Length > 10
                                    ? artikel.Bezeichnung.Substring(0, 10) + "..."
                                    : artikel.Bezeichnung;

                                column.Item().Text(kurzeBezeichnung)
                                    .FontSize(6)
                                    .Bold()
                                    .AlignCenter();

                                // Kleiner Abstand
                                column.Item().PaddingVertical(1);

                                // Barcode-Bild - ohne feste Größe, lässt QuestPDF das Layout bestimmen
                                column.Item().AlignCenter()
                                    .Image(barcodeBytes)
                                    .FitWidth();

                                // Kleiner Abstand
                                column.Item().PaddingVertical(1);

                                // Barcode-Text (zur visuellen Kontrolle) - kleinere Schrift
                                column.Item().Text(einheit.Barcode)
                                    .FontSize(5)
                                    .AlignCenter();
                            });
                        });
                    }).GeneratePdf(vollstaendigerPfad);
                });

                return vollstaendigerPfad;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Etiketts für Barcode {einheit.Barcode}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generiert ein Barcode-Bild im Code128-Format
        /// </summary>
        private static byte[] GeneriereBarcodeImage(string barcodeText)
        {
            try
            {
                // ZXing Barcode-Writer konfigurieren
                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Height = 30,  // Noch kleiner: 40 → 30
                        Width = 120,  // Noch kleiner: 150 → 120
                        Margin = 0    // Kein Rand: 1 → 0
                    }
                };

                // Barcode als Pixel-Daten generieren
                var pixelData = writer.Write(barcodeText);

                // Pixel-Daten in Bitmap umwandeln
                using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
                {
                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format32bppRgb);

                    try
                    {
                        // Pixel-Daten in Bitmap kopieren
                        System.Runtime.InteropServices.Marshal.Copy(
                            pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    // Bitmap als PNG-Bytes zurückgeben
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, SystemImageFormat.Png);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Generieren des Barcode-Bildes: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Druckt die erstellten PDF-Etiketten über den Standard-Drucker
        /// </summary>
        private static async Task DruckeEtikettenAsync(List<string> pdfPfade)
        {
            try
            {
                foreach (string pdfPfad in pdfPfade)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            // Erster Versuch: PDF über Standard-Drucker drucken
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = pdfPfad,
                                UseShellExecute = true,
                                Verb = "print",
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };

                            using (var process = Process.Start(startInfo))
                            {
                                if (process != null)
                                {
                                    process.WaitForExit(5000); // Max. 5 Sekunden warten
                                }
                            }
                        }
                        catch (Win32Exception)
                        {
                            // Fallback: PDF im Standard-PDF-Viewer öffnen
                            // Benutzer kann dann manuell drucken
                            try
                            {
                                var openInfo = new ProcessStartInfo
                                {
                                    FileName = pdfPfad,
                                    UseShellExecute = true,
                                    CreateNoWindow = false
                                };

                                Process.Start(openInfo);
                            }
                            catch (Exception)
                            {
                                // Falls auch das Öffnen fehlschlägt, ignorieren
                                // PDFs sind trotzdem gespeichert
                            }
                        }
                        catch (Exception)
                        {
                            // Andere Druckfehler ignorieren - PDFs sind gespeichert
                        }
                    });

                    // Pause zwischen den Druckaufträgen
                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                // Fehler beim Drucken nicht als kritisch behandeln
                // PDFs sind gespeichert, das ist das Wichtigste
                throw new Exception($"Etiketten wurden erstellt, aber Druckfehler: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gibt den Pfad zum Etikett-Verzeichnis zurück
        /// </summary>
        public static string GetEtikettenVerzeichnis()
        {
            return EtikettenVerzeichnis;
        }

        /// <summary>
        /// Löscht alte Etikett-PDFs (älter als 30 Tage)
        /// </summary>
        public static void BereinigeAlteEtiketten()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-30);
                var dateien = Directory.GetFiles(EtikettenVerzeichnis, "*.pdf");

                foreach (string datei in dateien)
                {
                    var fileInfo = new FileInfo(datei);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(datei);
                    }
                }
            }
            catch (Exception)
            {
                // Fehler beim Bereinigen ignorieren - nicht kritisch
            }
        }
    }
}