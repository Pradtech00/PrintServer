using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfficePrintServer.Core.Interfaces;

namespace OfficePrintServer.Infrastructure.Services;

public class RawTcpPrintServer : BackgroundService
{
    private readonly ILogger<RawTcpPrintServer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private TcpListener? _listener;

    public RawTcpPrintServer(ILogger<RawTcpPrintServer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Raw TCP Print Server starting on port 9100...");

        try
        {
            _listener = new TcpListener(IPAddress.Any, 9100);
            _listener.Start();
            _logger.LogInformation("Raw TCP Print Server listening on port 9100");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _logger.LogInformation("TCP connection from {RemoteEndPoint}", client.Client.RemoteEndPoint);

                    // Handle each connection in a separate task
                    _ = HandleClientAsync(client, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting TCP connection");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TCP print server on port 9100");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                // Read all data from client
                var data = await ReadAllDataAsync(stream, stoppingToken);

                if (data.Length == 0)
                {
                    _logger.LogWarning("Empty print job received");
                    return;
                }

                _logger.LogInformation("Received {Length} bytes print job from {Client}",
                    data.Length, client.Client.RemoteEndPoint);

                // Get printer name from repository
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPrintRepository>();
                var printers = await repo.GetPrintersAsync();
                var printer = printers.FirstOrDefault(p => p.IsActive);

                if (printer == null)
                {
                    _logger.LogWarning("No active printer found to send print job");
                    return;
                }

                _logger.LogInformation("Sending {Length} bytes to printer {Printer}", data.Length, printer.SystemName);

                // Send raw data directly to printer via winspool.drv
                var rawHelper = new RawPrinterHelper();
                rawHelper.SendRawData(printer.SystemName, data, $"TCP Job from {client.Client.RemoteEndPoint}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TCP print job");
        }
    }

    private static async Task<byte[]> ReadAllDataAsync(NetworkStream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[81920]; // 80KB buffer (Windows default TCP window)

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await ms.WriteAsync(buffer, 0, bytesRead, ct);
        }

        return ms.ToArray();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Raw TCP Print Server...");
        _listener?.Stop();
        await base.StopAsync(cancellationToken);
    }
}
