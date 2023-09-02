using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;

namespace Nowy.Database.Client.Services;

internal sealed class CachedNowyCollection<TModel> : INowyCollection<TModel> where TModel : class, IBaseModel
{
    private readonly ILogger _logger;
    private readonly HttpClient _http_client;
    private readonly INowyDatabaseCacheService _cache_service;
    private readonly IModelService _model_service;
    private readonly INowyMessageHub _message_hub;
    private readonly string _endpoint;
    private readonly string _database_name;
    private readonly string _entity_name;

    private static readonly JsonSerializerOptions _json_options = new JsonSerializerOptions() { PropertyNamingPolicy = null, };

    public CachedNowyCollection(
        ILogger logger,
        HttpClient http_client,
        INowyDatabaseCacheService cache_service,
        IModelService model_service,
        INowyMessageHub message_hub,
        string endpoint,
        string database_name,
        string entity_name
    )
    {
        _logger = logger;
        _http_client = http_client;
        _cache_service = cache_service;
        _model_service = model_service;
        _message_hub = message_hub;
        _endpoint = endpoint;
        _database_name = database_name;
        _entity_name = entity_name;
    }

    string INowyCollection<TModel>.DatabaseName => this._database_name;
    string INowyCollection<TModel>.EntityName => this._entity_name;

    public async Task<IReadOnlyList<TModel>> GetAllAsync(QueryOptions? options = null)
    {
        options ??= new();

        if (options is { with_deleted: false })
            return this._cache_service.Fetch<TModel>(q => q.Where(o => o.is_deleted == false));
        return this._cache_service.Fetch<TModel>();
    }

    public async Task<IReadOnlyList<TModel>> GetByFilterAsync(ModelFilter filter, QueryOptions? options = null)
    {
        options ??= new();

        // TODO
        throw new NotImplementedException();
    }

    public Task<TModel?> GetByIdAsync(string id, QueryOptions? options = null)
    {
        options ??= new();

        TModel? o = this._cache_service.FetchById<TModel>(id);

        if (o is { is_deleted: true } && options is { with_deleted: false })
        {
            o = null;
        }

        return Task.FromResult(o);
    }

    public Task<TModel> UpsertAsync(string id, TModel model)
    {
        this._cache_service.Add(model);
        this._cache_service.Save();
        return Task.FromResult(model);
    }

    public async Task DeleteAsync(string id)
    {
        TModel? o = this._cache_service.FetchById<TModel>(id);
        if (o is { })
        {
            this._cache_service.Delete(o);
        }

        this._cache_service.Save();
    }

    public INowyCollectionEventSubscription<TModel> Subscribe()
    {
        return new DefaultNowyCollectionEventSubscription<TModel>(this, _logger, _message_hub);
    }
}
