// ReSharper disable StringLiteralTypo
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: "logs/raspberrypi-controller-.log",
        rollingInterval: RollingInterval.Day,
        formatProvider: CultureInfo.InvariantCulture,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Raspberry Pi Controller service starting");

    var builder = WebApplication.CreateBuilder(args);

    // Enable running as Linux systemd daemon
    builder.Host.UseSystemd();

    builder.Services.AddSerilog();
    builder.Services.AddOpenApi();

    // Auto-generated options binding from [OptionsBinding] attributes
    builder.Services.AddOptionsFromRaspberryPi(builder.Configuration);

    // Auto-generated service registration from [Registration] attributes
    builder.Services.AddDependencyRegistrationsFromRaspberryPi(builder.Configuration);

    // Typed HTTP client (requires manual registration)
    builder.Services.AddHttpClient<IHostAgentHttpClient, HostAgentHttpClient>();

    // API Handlers (generated from OpenAPI spec)
    builder.Services.AddApiHandlersFromRaspberryPi();

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
    Log.Fatal(ex, "Raspberry Pi Controller service terminated unexpectedly");
}
finally
{
    Log.Information("Raspberry Pi Controller service stopped");
    await Log
        .CloseAndFlushAsync()
        .ConfigureAwait(false);
}