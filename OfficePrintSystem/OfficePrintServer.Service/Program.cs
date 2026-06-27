using Microsoft.Extensions.Logging.EventLog;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "printserver-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddSerilog();
    builder.Logging.AddEventLog(new EventLogSettings
    {
        SourceName = "OfficePrintServer",
        LogName = "Application"
    });

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "OfficePrintServer";
    });

    builder.Services.AddHostedService<OfficePrintServer.Service.ApiHostService>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
