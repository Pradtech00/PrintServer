using Microsoft.EntityFrameworkCore;
using OfficePrintServer.API.Services;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;
using OfficePrintServer.Infrastructure.Data;
using OfficePrintServer.Infrastructure.Services;

namespace OfficePrintServer.API;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Office Print Server API", Version = "v1" });
        });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfficePrintServer.db");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IPrintRepository, PrintRepository>();
        services.AddSingleton<IPrintQueue, PrintQueue>();
        services.AddScoped<IPrinterService, PrinterService>();

        services.AddHostedService<QueueProcessor>();
        services.AddHostedService<RawTcpPrintServer>();
    }

    public void Configure(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            // Auto-refresh printer list from Windows on startup
            try
            {
                var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();
                var repo = scope.ServiceProvider.GetRequiredService<IPrintRepository>();
                var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Startup>>();

                var sid = Task.Run(() => printerService.GetInstalledPrintersAsync()).GetAwaiter().GetResult();
                var installed = sid.ToList();
                var existing = Task.Run(() => repo.GetPrintersAsync()).GetAwaiter().GetResult();
                var existingDict = new Dictionary<string, Printer>();
                foreach (var p in existing)
                    existingDict[p.SystemName] = p;

                logger.LogInformation("Found {Count} installed printers: {Names}", installed.Count, string.Join(", ", installed.Select(p => p.SystemName)));

                foreach (var p in installed)
                {
                    if (!existingDict.ContainsKey(p.SystemName))
                    {
                        Task.Run(() => repo.AddPrinterAsync(p)).GetAwaiter().GetResult();
                        logger.LogInformation("Added printer to database: {Name}", p.SystemName);
                    }
                    else
                    {
                        Task.Run(() => repo.UpdatePrinterStatusAsync(existingDict[p.SystemName].Id, p.Status)).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                var log = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Startup>>();
                log.LogWarning(ex, "Failed to auto-refresh printer list on startup");
            }
        }

        app.UseCors();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();
    }
}
