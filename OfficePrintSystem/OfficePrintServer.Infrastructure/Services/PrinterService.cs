using System.Drawing;
using System.Drawing.Printing;
using Microsoft.Extensions.Logging;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;

namespace OfficePrintServer.Infrastructure.Services;

public class PrinterService : IPrinterService
{
    private readonly ILogger<PrinterService> _logger;

    public PrinterService(ILogger<PrinterService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<Printer>> GetInstalledPrintersAsync()
    {
        var printers = new List<Printer>();
        var tcs = new TaskCompletionSource<List<Printer>>();
        var thread = new Thread(() =>
        {
            try
            {
                var list = new List<Printer>();
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    var settings = new PrinterSettings { PrinterName = printerName };
                    list.Add(new Printer
                    {
                        SystemName = printerName,
                        DisplayName = printerName,
                        Status = settings.IsValid ? "Ready" : "Offline",
                        IsActive = true
                    });
                }
                tcs.TrySetResult(list);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        // Timeout 10 seconds to prevent hanging
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(10000));
        if (completed == tcs.Task)
        {
            printers = await tcs.Task;
        }
        else
        {
            _logger.LogWarning("Printer enumeration timed out after 10 seconds");
            thread.Interrupt();
        }

        return printers;
    }

    public Task<string> GetPrinterStatusAsync(string printerName)
    {
        try
        {
            var settings = new PrinterSettings { PrinterName = printerName };
            return Task.FromResult(settings.IsValid ? "Ready" : "Offline");
        }
        catch
        {
            return Task.FromResult("Unknown");
        }
    }

    public async Task PrintPdfAsync(string printerName, string filePath, string documentName)
    {
        var tcs = new TaskCompletionSource<bool>();
        var thread = new Thread(async () =>
        {
            try
            {
                Windows.Data.Pdf.PdfDocument? pdfDoc = null;

                try
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(
                        System.IO.Path.GetFullPath(filePath));
                    pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Windows.Data.Pdf failed, falling back to raw print");
                }

                if (pdfDoc == null || pdfDoc.PageCount == 0)
                {
                    // Fallback: print raw file or just send to spooler
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    byte[] rawData = new byte[fs.Length];
                    fs.Read(rawData, 0, rawData.Length);

                    var rawPrinter = new RawPrinterHelper();
                    rawPrinter.SendRawData(printerName, rawData, documentName);

                    tcs.SetResult(true);
                    return;
                }

                using var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerName;
                printDoc.DocumentName = documentName;
                printDoc.DefaultPageSettings.Landscape = false;

                int currentPage = 0;

                printDoc.PrintPage += (sender, e) =>
                {
                    try
                    {
                        var page = pdfDoc.GetPage((uint)currentPage);
                        using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();

                        page.RenderToStreamAsync(stream).AsTask().GetAwaiter().GetResult();

                        using var netStream = stream.AsStreamForRead();
                        using var bmp = new Bitmap(netStream);

                        var marginBounds = e.MarginBounds;
                        e.Graphics!.DrawImage(bmp, marginBounds);

                        currentPage++;
                        e.HasMorePages = currentPage < pdfDoc.PageCount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error rendering page {Page} for document {Doc}",
                            currentPage, documentName);
                        e.HasMorePages = false;
                    }
                };

                printDoc.Print();
                tcs.SetResult(true);

                _logger.LogInformation("Document '{Doc}' sent to printer '{Printer}' successfully",
                    documentName, printerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Print failed for document '{Doc}' on printer '{Printer}'",
                    documentName, printerName);
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        await tcs.Task;
    }
}

internal class RawPrinterHelper
{
    [System.Runtime.InteropServices.DllImport("winspool.drv", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOC_INFO_1 pDocInfo);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    internal struct DOC_INFO_1
    {
        public string? pDocName;
        public string? pOutputFile;
        public string? pDataType;
    }

    public void SendRawData(string printerName, byte[] data, string docName)
    {
        if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

        try
        {
            var docInfo = new DOC_INFO_1
            {
                pDocName = docName,
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

            try
            {
                if (!StartPagePrinter(hPrinter))
                    throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                try
                {
                    if (!WritePrinter(hPrinter, data, data.Length, out int written))
                        throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                    if (written != data.Length)
                        throw new Exception($"Only {written} of {data.Length} bytes written to printer");
                }
                finally
                {
                    EndPagePrinter(hPrinter);
                }
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }
}
