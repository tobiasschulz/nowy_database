using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nowy.Database.Client;
using Nowy.Database.Client.TestProgram;
using Nowy.Database.Common;
using Nowy.Database.Contract.Services;
using Nowy.MessageHub.Client;
using Nowy.Standard;
using Serilog;
using Serilog.Core;
using Serilog.Events;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((host_builder_context, services) =>
    {
        services.AddSingleton<Worker1>();
        services.AddHostedService<Worker1>();
        services.AddLogging(builder =>
        {
            Logger logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .CreateLogger();
            builder.AddSerilog(logger);
        });

        services.AddHttpClient();

        services.AddNowyDatabaseClient(config => { config.EndpointUrl = "https://main.database.schulz.dev"; });

        services.AddNowyMessageHubClient(config =>
        {
            config.AddEndpoint("https://main.messagehub.schulz.dev");
            config.AddReceiver<Worker1.Receiver1>();
        });

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        // Settings settings = new();
        // configuration.Bind(nameof(Settings), settings);
        // services.AddSingleton(settings);
    });

builder.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

await builder.Build().RunAsync();
