using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Nowy.Database.Client.Services;
using Nowy.Database.Common.Services;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;
using Nowy.Standard;

namespace Nowy.Database.Client;

public static class ServiceCollectionExtensions
{
    public static void AddNowyDatabaseClient(this IServiceCollection services, Action<INowyDatabaseClientConfig>? config_action)
    {
        NowyDatabaseClientConfig config = new();
        config.ParseEnvironmentVariables();
        config_action?.Invoke(config);
        config.Apply(services);

        services.AddSingleton<IModelService, DefaultModelService>(sp => new DefaultModelService());

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
        {
            services.AddHostedService<INowyDatabaseCacheService>(sp => new DefaultNowyDatabaseCacheService(
                sp.GetRequiredService<ILogger<DefaultNowyDatabaseCacheService>>(),
                sp.GetRequiredService<INowyDatabase>(),
                sp.GetRequiredService<IEnumerable<IDatabaseStaticDataImporter>>()
            ));
        }

        services.AddTransient<INowyDatabase>(sp =>
        {
            string? endpoint_url = config.EndpointUrl;
            IConfiguration? configuration = sp.GetService<IConfiguration>();

            if (string.IsNullOrEmpty(endpoint_url)) endpoint_url = configuration?.GetSection("NowyDatabase")["Url"];
            if (string.IsNullOrEmpty(endpoint_url)) throw new Exception("NowyDatabase.Url is missing in appsettings");

            return new RestNowyDatabase(
                sp.GetRequiredService<ILogger<RestNowyDatabase>>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetService<INowyDatabaseAuthService>(),
                sp.GetService<INowyDatabaseCacheService>(),
                sp.GetRequiredService<IModelService>(),
                sp.GetRequiredService<INowyMessageHub>(),
                endpoint_url
            );
        });
    }
}

public interface INowyDatabaseClientConfig
{
    string EndpointUrl { get; set; }
}

internal sealed class NowyDatabaseClientConfig : INowyDatabaseClientConfig
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Action<IServiceCollection>? _services_apply_func = null;

    public string? EndpointUrl { get; set; }

    internal void Apply(IServiceCollection services)
    {
        this._services_apply_func?.Invoke(services);
    }

    internal void ParseEnvironmentVariables()
    {
        string[] endpoint_urls = new[]
            {
                Environment.GetEnvironmentVariable("NOWY_DATABASE_ENDPOINT_URL") ?? string.Empty,
                Environment.GetEnvironmentVariable("LR_DATABASE_ENDPOINT_URL") ?? string.Empty,
                Environment.GetEnvironmentVariable("TS_DATABASE_ENDPOINT_URL") ?? string.Empty,
            }
            .SelectMany(s => s.Split(','))
            .Where(o => !string.IsNullOrEmpty(o))
            .ToArray();

        foreach (string endpoint_url in endpoint_urls)
        {
            EndpointUrl = endpoint_url;
        }
    }
}
