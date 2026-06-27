using OfficePrintServer.Core.Entities;

namespace OfficePrintServer.Core.Interfaces;

public interface IPrintRepository
{
    Task<IEnumerable<Printer>> GetPrintersAsync();
    Task<Printer?> GetPrinterByIdAsync(string id);
    Task AddPrinterAsync(Printer printer);
    Task UpdatePrinterStatusAsync(string id, string status);
    
    Task<IEnumerable<PrintJob>> GetJobsAsync();
    Task<PrintJob?> GetJobByIdAsync(string id);
    Task AddJobAsync(PrintJob job);
    Task UpdateJobStatusAsync(string id, string status, string? errorMessage = null);
}