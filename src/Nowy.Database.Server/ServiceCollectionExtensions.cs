using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using Nowy.Database.Contract.Services;
using Nowy.Database.Server.Endpoints;
using Nowy.Database.Server.Services;
using Nowy.Standard;

namespace Nowy.Database.Server;

public static class ServiceCollectionExtensions
{
    public static void AddNowyDatabaseServer(this IServiceCollection services, Action<INowyDatabaseServerConfig>? config_action)
    {
        NowyDatabaseServerConfig config = new();
        config.ParseEnvironmentVariables();
        config_action?.Invoke(config);
        config.Apply(services);

        BsonSerializer.RegisterIdGenerator(typeof(string), new StringObjectIdGenerator());
        BsonSerializer.RegisterSerializer(typeof(UnixTimestamp), new MongoUnixTimestampSerializer());

        services.AddSingleton<ApiEndpointsV1>();

        services.AddSingleton<MongoRepository>();
        services.AddSingleton<INowyDatabase, MongoNowyDatabase>();

        services.AddSingleton<IMongoClient>(sp =>
        {
            string? mongo_connection_string = config.MongoConnectionString;

            IConfiguration? configuration = sp.GetService<IConfiguration>();

            if (string.IsNullOrEmpty(mongo_connection_string)) mongo_connection_string = configuration?.GetSection("NowyDatabase")["ConnectionString"];
            if (string.IsNullOrEmpty(mongo_connection_string)) throw new Exception("NowyDatabase.ConnectionString is missing in appsettings");

            return new MongoClient(mongo_connection_string);
        });
    }

    public static T UseNowyDatabaseServer<T>(this T app) where T : class, IHost, IApplicationBuilder, IEndpointRouteBuilder
    {
        app.Services.GetRequiredService<ApiEndpointsV1>().MapEndpoints(app.MapGroup("api/v1"));

        return app;
    }
}

public interface INowyDatabaseServerConfig
{
    string MongoConnectionString { get; set; }
}

internal sealed class NowyDatabaseServerConfig : INowyDatabaseServerConfig
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Action<IServiceCollection>? _services_apply_func = null;

    public string? MongoConnectionString { get; set; }

    internal void Apply(IServiceCollection services)
    {
        this._services_apply_func?.Invoke(services);
    }

    internal void ParseEnvironmentVariables()
    {
        string[] connection_strings = new[]
            {
                Environment.GetEnvironmentVariable("NOWY_DATABASE_CONNECTIONSTRING") ?? string.Empty,
                Environment.GetEnvironmentVariable("LR_DATABASE_CONNECTIONSTRING") ?? string.Empty,
                Environment.GetEnvironmentVariable("TS_DATABASE_CONNECTIONSTRING") ?? string.Empty,
            }
            .SelectMany(s => s.Split(','))
            .Where(o => !string.IsNullOrEmpty(o))
            .ToArray();

        foreach (string connection_string in connection_strings)
        {
            MongoConnectionString = connection_string;
        }
    }
}
