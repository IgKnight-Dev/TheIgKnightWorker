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

        try
        {
            // ---------------------------------------------------------
            // 1. Validate HTML
            // ---------------------------------------------------------

            var validationStopwatch = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException(
                    "HTML content is required.",
                    nameof(html));
            }

            // ---------------------------------------------------------
            // 2. Ensure browser
            // ---------------------------------------------------------

            var browserStopwatch = Stopwatch.StartNew();

            var browser =
                await EnsureBrowserAsync(
                    cancellationToken);


            // ---------------------------------------------------------
            // 3. Create browser context
            // ---------------------------------------------------------

            var contextStopwatch = Stopwatch.StartNew();

            await using var context =
                await browser.NewContextAsync();
    

            // ---------------------------------------------------------
            // 4. Create page
            // ---------------------------------------------------------

            var pageStopwatch = Stopwatch.StartNew();


            await using var page =
                await context.NewPageAsync();


            // ---------------------------------------------------------
            // 5. Load HTML
            // ---------------------------------------------------------

            var contentStopwatch = Stopwatch.StartNew();

            await page.SetContentAsync(
                html,
                new PageSetContentOptions
                {
                    WaitUntil =
                        WaitUntilState.NetworkIdle
                });


            // ---------------------------------------------------------
            // 6. Generate PDF
            // ---------------------------------------------------------

            var pdfStopwatch = Stopwatch.StartNew();

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


            // ---------------------------------------------------------
            // 7. Validate PDF
            // ---------------------------------------------------------

            var pdfValidationStopwatch =
                Stopwatch.StartNew();

            if (pdfBytes == null ||
                pdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Playwright generated an empty PDF.");
            }

            if (!IsPdf(pdfBytes))
            {
                throw new InvalidOperationException(
                    "Playwright generated invalid PDF data.");
            }

            // ---------------------------------------------------------
            // 8. Total
            // ---------------------------------------------------------

            return pdfBytes;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
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
            return _browser;
        }

        var lockStopwatch =
            Stopwatch.StartNew();

        await _browserLock.WaitAsync(
            cancellationToken);

        try
        {
            if (_browser != null &&
                _browser.IsConnected)
            {
                return _browser;
            }

            // ---------------------------------------------------------
            // Create Playwright
            // ---------------------------------------------------------

            var playwrightStopwatch =
                Stopwatch.StartNew();

            _playwright ??=
                await Playwright.CreateAsync();


            // ---------------------------------------------------------
            // Launch Chromium
            // ---------------------------------------------------------

            var launchStopwatch =
                Stopwatch.StartNew();

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

            return _browser;
        }
        finally
        {
            _browserLock.Release();

        }
    }

    public static async Task ShutdownBrowserAsync()
    {
        var stopwatch =
            Stopwatch.StartNew();

        await _browserLock.WaitAsync();

        try
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();

                _browser = null;

            }

            if (_playwright != null)
            {
                _playwright.Dispose();

                _playwright = null;
            }

        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
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