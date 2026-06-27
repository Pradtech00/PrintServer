using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;

namespace OfficePrintServer.API.Services;

public class QueueProcessor : BackgroundService
{
    private readonly IPrintQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueProcessor> _logger;

    public QueueProcessor(IPrintQueue queue, IServiceProvider serviceProvider, ILogger<QueueProcessor> _logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        this._logger = _logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Print Queue Processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueJobAsync(stoppingToken);
                if (job == null) continue;

                _logger.LogInformation("Processing job {JobId} for printer {PrinterId}", job.Id, job.PrinterId);

                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPrintRepository>();
                var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

                await repo.UpdateJobStatusAsync(job.Id, "Printing");

                var printer = await repo.GetPrinterByIdAsync(job.PrinterId);
                if (printer == null)
                {
                    throw new Exception($"Printer with ID {job.PrinterId} not found.");
                }

                if (!File.Exists(job.FilePath))
                {
                    throw new FileNotFoundException("Temp print file not found on server.", job.FilePath);
                }

                await printerService.PrintPdfAsync(printer.SystemName, job.FilePath, job.DocumentName);
                
                await repo.UpdateJobStatusAsync(job.Id, "Completed");
                _logger.LogInformation("Job {JobId} completed successfully.", job.Id);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing print job.");
            }
        }
    }
}