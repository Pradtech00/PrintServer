using Microsoft.AspNetCore.Builder;
using Serilog;

namespace OfficePrintServer.Service;

public class ApiHostService : BackgroundService
{
    private readonly ILogger<ApiHostService> _logger;
    private WebApplication? _webApp;

    public ApiHostService(ILogger<ApiHostService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Office Print Server API on port 18080...");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Environment.GetCommandLineArgs(),
            ContentRootPath = AppDomain.CurrentDomain.BaseDirectory
        });

        builder.WebHost.UseUrls("http://0.0.0.0:18080");
        builder.WebHost.UseKestrel();
        builder.Host.UseSerilog();

        var startup = new OfficePrintServer.API.Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services);

        _webApp = builder.Build();
        startup.Configure(_webApp);

        _logger.LogInformation("Office Print Server is running. Listening on http://0.0.0.0:18080");
        await _webApp.RunAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Office Print Server...");
        if (_webApp != null)
        {
            await _webApp.StopAsync(cancellationToken);
            await _webApp.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
