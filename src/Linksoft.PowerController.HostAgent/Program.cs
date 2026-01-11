// ReSharper disable StringLiteralTypo
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: "logs/hostagent-.log",
        rollingInterval: RollingInterval.Day,
        formatProvider: CultureInfo.InvariantCulture,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("HostAgent service starting");

    var builder = WebApplication.CreateBuilder(args);

    // Enable running as Windows Service or Linux systemd daemon
    builder.Host.UseWindowsService();
    builder.Host.UseSystemd();

    builder.Services.AddSerilog();
    builder.Services.AddOpenApi();

    // Auto-generated options binding from [OptionsBinding] attributes
    builder.Services.AddOptionsFromHostAgent(builder.Configuration);

    // Auto-generated service registration from [Registration] attributes
    builder.Services.AddDependencyRegistrationsFromHostAgent(builder.Configuration);

    // API handlers (generated from OpenAPI spec)
    builder.Services.AddApiHandlersFromHostAgent();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapEndpoints();

    await app
        .RunAsync()
        .ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "HostAgent service terminated unexpectedly");
}
finally
{
    Log.Information("HostAgent service stopped");
    await Log
        .CloseAndFlushAsync()
        .ConfigureAwait(false);
}