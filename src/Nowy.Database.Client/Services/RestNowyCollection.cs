using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;

namespace Nowy.Database.Client.Services;

internal sealed class RestNowyCollection<TModel> : INowyCollection<TModel> where TModel : class, IBaseModel
{
    private readonly ILogger _logger;
    private readonly HttpClient _http_client;
    private readonly INowyDatabaseAuthService? _database_auth_service;
    private readonly IModelService _model_service;
    private readonly INowyMessageHub _message_hub;
    private readonly string _endpoint;
    private readonly string _database_name;
    private readonly string _entity_name;

    private static readonly JsonSerializerOptions _json_options = new JsonSerializerOptions() { PropertyNamingPolicy = null, };

    private static readonly JsonSerializerOptions _json_options_filter = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public RestNowyCollection(
        ILogger logger,
        HttpClient http_client,
        INowyDatabaseAuthService? database_auth_service,
        IModelService model_service,
        INowyMessageHub message_hub,
        string endpoint,
        string database_name,
        string entity_name
    )
    {
        _logger = logger;
        _http_client = http_client;
        _database_auth_service = database_auth_service;
        _model_service = model_service;
        _message_hub = message_hub;
        _endpoint = endpoint;
        _database_name = database_name;
        _entity_name = entity_name;
    }

    string INowyCollection<TModel>.DatabaseName => this._database_name;
    string INowyCollection<TModel>.EntityName => this._entity_name;

    private void _configureAuth(HttpRequestMessage request)
    {
        if (_database_auth_service?.GetJWT() is { Length: > 0 } jwt)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }
        else
        {
            request.Headers.Authorization = null;
        }
    }

    public async Task<IReadOnlyList<TModel>> GetAllAsync(QueryOptions? options = null)
    {
        options ??= new();

        if (options is { with_deleted: false })
        {
            ModelFilter filter = ModelFilterBuilder.Equals(nameof(IBaseModel.is_deleted), false).Build();
            return await GetByFilterAsync(filter, new QueryOptions(with_deleted: true));
        }

        string url = $"{_endpoint}/api/v1/{_database_name}/{_entity_name}";
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        _configureAuth(request);

        using HttpResponseMessage response = await _http_client.SendAsync(request);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        List<TModel>? result = await JsonSerializer.DeserializeAsync<List<TModel>>(stream, options: _json_options) ?? new();
        return result;
    }

    public async Task<IReadOnlyList<TModel>> GetByFilterAsync(ModelFilter filter, QueryOptions? options = null)
    {
        options ??= new();

        if (options is { with_deleted: false })
        {
            filter = new()
            {
                FiltersAnd = new()
                {
                    filter,
                    ModelFilterBuilder.Equals(nameof(IBaseModel.is_deleted), false).Build(),
                }
            };
        }

        string url = $"{_endpoint}/api/v1/{_database_name}/{_entity_name}/filter/{HttpUtility.UrlEncode(JsonSerializer.Serialize(filter, _json_options_filter))}";
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        _configureAuth(request);

        using HttpResponseMessage response = await _http_client.SendAsync(request);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        List<TModel>? result = await JsonSerializer.DeserializeAsync<List<TModel>>(stream, options: _json_options) ?? new();
        return result;
    }

    public async Task<TModel?> GetByIdAsync(string id, QueryOptions? options = null)
    {
        options ??= new();

        if (string.IsNullOrEmpty(id)) throw new ArgumentOutOfRangeException(nameof(id));

        string url = $"{_endpoint}/api/v1/{_database_name}/{_entity_name}/{id}";
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        _configureAuth(request);

        using HttpResponseMessage response = await _http_client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        TModel? result = await JsonSerializer.DeserializeAsync<TModel>(stream, options: _json_options);

        if (result is { is_deleted: true } && options is { with_deleted: false })
        {
            result = null;
        }

        return result;
    }

    public async Task<TModel> UpsertAsync(string id, TModel model)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentOutOfRangeException(nameof(id));

        if (model is IUniqueModel unique_model)
        {
            model.ids = model.ids
                .Where(o => !o.StartsWith("UNIQUE:"))
                .Concat(unique_model.GetUniqueKeys().Select(k => $"UNIQUE:{k}"))
                .ToArray();
        }

        string url = $"{_endpoint}/api/v1/{_database_name}/{_entity_name}/{id}";
        using HttpRequestMessage request = new(HttpMethod.Post, url);
        _configureAuth(request);
        request.Content = JsonContent.Create(model, mediaType: null, _json_options);

        using HttpResponseMessage response = await _http_client.SendAsync(request);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        TModel? result = await JsonSerializer.DeserializeAsync<TModel>(stream, options: _json_options);
        _model_service.SendModelUpdated(model);
        return result ?? throw new ArgumentNullException(nameof(result));
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentOutOfRangeException(nameof(id));

        string url = $"{_endpoint}/api/v1/{_database_name}/{_entity_name}/{id}";
        using HttpRequestMessage request = new(HttpMethod.Delete, url);
        _configureAuth(request);

        using HttpResponseMessage response = await _http_client.SendAsync(request);
        string result = await response.Content.ReadAsStringAsync();
    }

    public INowyCollectionEventSubscription<TModel> Subscribe()
    {
        return new DefaultNowyCollectionEventSubscription<TModel>(this, _logger, _message_hub);
    }
}
