using OfficePrintServer.Core.Entities;

namespace OfficePrintServer.Core.Interfaces;

public interface IPrintQueue
{
    Task EnqueueJobAsync(PrintJob job);
    Task<PrintJob?> DequeueJobAsync(CancellationToken cancellationToken);
}