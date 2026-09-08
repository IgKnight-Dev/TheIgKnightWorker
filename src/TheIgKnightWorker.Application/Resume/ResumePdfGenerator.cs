using Microsoft.Playwright;
using System.Diagnostics;
using TheIgKnightWorker.Application.Interfaces;

namespace TheIgKnightWorker.Application.Services;

public class ResumePdfGenerator :
    IResumePdfGenerator,
    IAsyncDisposable
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    private static readonly SemaphoreSlim _browserLock =
        new(1, 1);

    public async Task<byte[]> GenerateAsync(
        string html,
        CancellationToken cancellationToken = default)
    {
        var totalStopwatch = Stopwatch.StartNew();

        Console.WriteLine(
            "[ResumePdfGenerator] PDF generation started.");

        try
        {
            // ---------------------------------------------------------
            // 1. Validate HTML
            // ---------------------------------------------------------

            var validationStopwatch = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(html))
            {
                Console.WriteLine(
                    "[ResumePdfGenerator] HTML validation failed: HTML is empty.");

                throw new ArgumentException(
                    "HTML content is required.",
                    nameof(html));
            }

            Console.WriteLine(
                $"[ResumePdfGenerator] HTML validated. " +
                $"HTML length: {html.Length:N0} characters. " +
                $"Time: {validationStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // 2. Ensure browser
            // ---------------------------------------------------------

            var browserStopwatch = Stopwatch.StartNew();

            Console.WriteLine(
                "[ResumePdfGenerator] Ensuring Playwright browser is available...");

            var browser =
                await EnsureBrowserAsync(
                    cancellationToken);

            Console.WriteLine(
                $"[ResumePdfGenerator] Browser ready. " +
                $"Connected: {browser.IsConnected}. " +
                $"Time: {browserStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // 3. Create browser context
            // ---------------------------------------------------------

            var contextStopwatch = Stopwatch.StartNew();

            Console.WriteLine(
                "[ResumePdfGenerator] Creating browser context...");

            await using var context =
                await browser.NewContextAsync();

            Console.WriteLine(
                $"[ResumePdfGenerator] Browser context created. " +
                $"Time: {contextStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // 4. Create page
            // ---------------------------------------------------------

            var pageStopwatch = Stopwatch.StartNew();

            Console.WriteLine(
                "[ResumePdfGenerator] Creating browser page...");

            await using var page =
                await context.NewPageAsync();

            Console.WriteLine(
                $"[ResumePdfGenerator] Browser page created. " +
                $"Time: {pageStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // 5. Load HTML
            // ---------------------------------------------------------

            var contentStopwatch = Stopwatch.StartNew();

            Console.WriteLine(
                "[ResumePdfGenerator] Setting HTML content...");

            await page.SetContentAsync(
                html,
                new PageSetContentOptions
                {
                    WaitUntil =
                        WaitUntilState.NetworkIdle
                });

            Console.WriteLine(
                $"[ResumePdfGenerator] HTML content loaded. " +
                $"Time: {contentStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // 6. Generate PDF
            // ---------------------------------------------------------

            var pdfStopwatch = Stopwatch.StartNew();

            Console.WriteLine(
                "[ResumePdfGenerator] Generating PDF...");

            var pdfBytes =
                await page.PdfAsync(
                    new PagePdfOptions
                    {
                        Format = "A4",
                        PrintBackground = true,
                        Margin = new Margin
                        {
                            Top = "20px",
                            Bottom = "20px",
                            Left = "20px",
                            Right = "20px"
                        }
                    });

            Console.WriteLine(
                $"[ResumePdfGenerator] PDF generation completed. " +
                $"Size: {pdfBytes?.Length ?? 0:N0} bytes. " +
                $"Time: {pdfStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // 7. Validate PDF
            // ---------------------------------------------------------

            var pdfValidationStopwatch =
                Stopwatch.StartNew();

            if (pdfBytes == null ||
                pdfBytes.Length == 0)
            {
                Console.WriteLine(
                    "[ResumePdfGenerator] PDF validation failed: PDF is empty.");

                throw new InvalidOperationException(
                    "Playwright generated an empty PDF.");
            }

            if (!IsPdf(pdfBytes))
            {
                Console.WriteLine(
                    "[ResumePdfGenerator] PDF validation failed: Invalid PDF signature.");

                throw new InvalidOperationException(
                    "Playwright generated invalid PDF data.");
            }

            Console.WriteLine(
                $"[ResumePdfGenerator] PDF validated successfully. " +
                $"Time: {pdfValidationStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // 8. Total
            // ---------------------------------------------------------

            Console.WriteLine(
                $"[ResumePdfGenerator] PDF generation completed successfully. " +
                $"Total time: {totalStopwatch.ElapsedMilliseconds} ms. " +
                $"PDF size: {pdfBytes.Length:N0} bytes.");

            return pdfBytes;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                $"[ResumePdfGenerator] PDF generation cancelled. " +
                $"Total time: {totalStopwatch.ElapsedMilliseconds} ms.");

            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[ResumePdfGenerator] PDF generation failed. " +
                $"Total time: {totalStopwatch.ElapsedMilliseconds} ms. " +
                $"Error: {ex.Message}");

            throw;
        }
    }

    private static async Task<IBrowser> EnsureBrowserAsync(
        CancellationToken cancellationToken)
    {
        var totalStopwatch =
            Stopwatch.StartNew();

        if (_browser != null &&
            _browser.IsConnected)
        {
            Console.WriteLine(
                "[ResumePdfGenerator] Existing Playwright browser is connected. " +
                "Reusing browser.");

            return _browser;
        }

        Console.WriteLine(
            "[ResumePdfGenerator] Playwright browser is not available. " +
            "Waiting for browser lock...");

        var lockStopwatch =
            Stopwatch.StartNew();

        await _browserLock.WaitAsync(
            cancellationToken);

        Console.WriteLine(
            $"[ResumePdfGenerator] Browser lock acquired. " +
            $"Wait time: {lockStopwatch.ElapsedMilliseconds} ms");

        try
        {
            if (_browser != null &&
                _browser.IsConnected)
            {
                Console.WriteLine(
                    "[ResumePdfGenerator] Browser became available " +
                    "while waiting for lock. Reusing browser.");

                return _browser;
            }

            // ---------------------------------------------------------
            // Create Playwright
            // ---------------------------------------------------------

            var playwrightStopwatch =
                Stopwatch.StartNew();

            Console.WriteLine(
                "[ResumePdfGenerator] Creating Playwright instance...");

            _playwright ??=
                await Playwright.CreateAsync();

            Console.WriteLine(
                $"[ResumePdfGenerator] Playwright instance ready. " +
                $"Time: {playwrightStopwatch.ElapsedMilliseconds} ms");


            // ---------------------------------------------------------
            // Launch Chromium
            // ---------------------------------------------------------

            var launchStopwatch =
                Stopwatch.StartNew();

            Console.WriteLine(
                "[ResumePdfGenerator] Launching Chromium...");

            _browser =
                await _playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        Headless = true,
                        Args = new[]
                        {
                            "--no-sandbox",
                            "--disable-dev-shm-usage"
                        }
                    });

            Console.WriteLine(
                $"[ResumePdfGenerator] Chromium launched. " +
                $"Connected: {_browser.IsConnected}. " +
                $"Launch time: {launchStopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine(
                $"[ResumePdfGenerator] Browser initialization completed. " +
                $"Total time: {totalStopwatch.ElapsedMilliseconds} ms");

            return _browser;
        }
        finally
        {
            _browserLock.Release();

            Console.WriteLine(
                "[ResumePdfGenerator] Browser lock released.");
        }
    }

    public static async Task ShutdownBrowserAsync()
    {
        var stopwatch =
            Stopwatch.StartNew();

        Console.WriteLine(
            "[ResumePdfGenerator] Shutting down Playwright browser...");

        await _browserLock.WaitAsync();

        try
        {
            if (_browser != null)
            {
                Console.WriteLine(
                    "[ResumePdfGenerator] Closing Chromium browser...");

                await _browser.CloseAsync();

                _browser = null;

                Console.WriteLine(
                    $"[ResumePdfGenerator] Chromium browser closed. " +
                    $"Time: {stopwatch.ElapsedMilliseconds} ms");
            }

            if (_playwright != null)
            {
                Console.WriteLine(
                    "[ResumePdfGenerator] Disposing Playwright instance...");

                _playwright.Dispose();

                _playwright = null;
            }

            Console.WriteLine(
                $"[ResumePdfGenerator] Playwright shutdown completed. " +
                $"Total time: {stopwatch.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[ResumePdfGenerator] Error while shutting down Playwright. " +
                $"Time: {stopwatch.ElapsedMilliseconds} ms. " +
                $"Error: {ex.Message}");

            throw;
        }
        finally
        {
            _browserLock.Release();

            Console.WriteLine(
                "[ResumePdfGenerator] Browser lock released after shutdown.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine(
            "[ResumePdfGenerator] DisposeAsync called.");

        await ShutdownBrowserAsync();
    }

    private static bool IsPdf(
        byte[] bytes)
    {
        if (bytes == null ||
            bytes.Length < 5)
        {
            return false;
        }

        return
            bytes[0] == (byte)'%' &&
            bytes[1] == (byte)'P' &&
            bytes[2] == (byte)'D' &&
            bytes[3] == (byte)'F' &&
            bytes[4] == (byte)'-';
    }
}