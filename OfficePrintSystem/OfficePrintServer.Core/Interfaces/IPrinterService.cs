using OfficePrintServer.Core.Entities;

namespace OfficePrintServer.Core.Interfaces;

public interface IPrinterService
{
    Task<IEnumerable<Printer>> GetInstalledPrintersAsync();
    Task<string> GetPrinterStatusAsync(string printerName);
    Task PrintPdfAsync(string printerName, string filePath, string documentName);
}