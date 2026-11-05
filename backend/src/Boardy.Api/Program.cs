using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Boardy API host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.Run();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Boardy API host terminated unexpectedly");
    Environment.ExitCode = 1;
    return 1;
}
finally
{
    Log.Information("Boardy API host shutting down");
    Log.CloseAndFlush();
}
