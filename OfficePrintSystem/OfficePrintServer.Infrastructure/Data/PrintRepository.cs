using Microsoft.EntityFrameworkCore;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;

namespace OfficePrintServer.Infrastructure.Data;

public class PrintRepository : IPrintRepository
{
    private readonly AppDbContext _context;

    public PrintRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Printer>> GetPrintersAsync()
    {
        return await _context.Printers.ToListAsync();
    }

    public async Task<Printer?> GetPrinterByIdAsync(string id)
    {
        return await _context.Printers.FindAsync(id);
    }

    public async Task AddPrinterAsync(Printer printer)
    {
        await _context.Printers.AddAsync(printer);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePrinterStatusAsync(string id, string status)
    {
        var printer = await _context.Printers.FindAsync(id);
        if (printer != null)
        {
            printer.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<PrintJob>> GetJobsAsync()
    {
        return await _context.PrintJobs
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<PrintJob?> GetJobByIdAsync(string id)
    {
        return await _context.PrintJobs.FindAsync(id);
    }

    public async Task AddJobAsync(PrintJob job)
    {
        await _context.PrintJobs.AddAsync(job);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateJobStatusAsync(string id, string status, string? errorMessage = null)
    {
        var job = await _context.PrintJobs.FindAsync(id);
        if (job != null)
        {
            job.Status = status;
            if (errorMessage != null)
            {
                job.ErrorMessage = errorMessage;
            }
            await _context.SaveChangesAsync();
        }
    }
}