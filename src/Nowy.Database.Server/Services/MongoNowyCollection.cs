using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;

namespace Nowy.Database.Server.Services;

internal sealed class MongoNowyCollection<TModel> : INowyCollection<TModel> where TModel : class, IBaseModel
{
    private readonly ILogger _logger;
    private readonly MongoRepository _repo;
    private readonly string _database_name;
    private readonly string _entity_name;

    private static readonly JsonSerializerOptions _json_options = new JsonSerializerOptions() { PropertyNamingPolicy = null, };

    public MongoNowyCollection(
        ILogger logger,
        MongoRepository repo,
        string database_name,
        string entity_name
    )
    {
        this._logger = logger;
        this._repo = repo;
        this._database_name = database_name;
        this._entity_name = entity_name;
    }

    string INowyCollection<TModel>.DatabaseName => this._database_name;
    string INowyCollection<TModel>.EntityName => this._entity_name;

    public async Task<IReadOnlyList<TModel>> GetAllAsync(QueryOptions? options = null)
    {
        options ??= new();

        if (options is { with_deleted: false })
        {
            ModelFilter filter = ModelFilterBuilder.Equals(nameof(IBaseModel.is_deleted), false).Build();
            return await this.GetByFilterAsync(filter, new QueryOptions(with_deleted: true));
        }

        List<BsonDocument> list = await this._repo.GetAllAsync(database_name: this._database_name, entity_name: this._entity_name);

        await using MemoryStream stream = await list.MongoToJsonStream();

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

        List<BsonDocument> list = await this._repo.GetByFilterAsync(database_name: this._database_name, entity_name: this._entity_name, filter: filter);

        await using MemoryStream stream = await list.MongoToJsonStream();

        List<TModel>? result = await JsonSerializer.DeserializeAsync<List<TModel>>(stream, options: _json_options) ?? new();
        return result;
    }

    public async Task<TModel?> GetByIdAsync(string id, QueryOptions? options = null)
    {
        options ??= new();

        BsonDocument? document = await this._repo.GetByIdAsync(database_name: this._database_name, entity_name: this._entity_name, id: id);

        if (document is { })
        {
            await using MemoryStream stream = await document.MongoToJsonStream();

            TModel? result = await JsonSerializer.DeserializeAsync<TModel>(stream, options: _json_options);

            if (result is { is_deleted: true } && options is { with_deleted: false })
            {
                result = null;
            }

            return result;
        }

        return null;
    }

    public async Task<TModel> UpsertAsync(string id, TModel model)
    {
        string input_json = JsonSerializer.Serialize(model, _json_options);

        BsonDocument input_document = BsonDocument.Parse(input_json);
        BsonDocument? document = await this._repo.UpsertAsync(database_name: this._database_name, entity_name: this._entity_name, id: id, input: input_document);
        await using MemoryStream stream = await document.MongoToJsonStream();

        TModel? result = await JsonSerializer.DeserializeAsync<TModel>(stream, options: _json_options);
        return result ?? throw new ArgumentNullException(nameof(result));
    }

    public async Task DeleteAsync(string id)
    {
        BsonDocument? document = await this._repo.DeleteAsync(database_name: this._database_name, entity_name: this._entity_name, id: id);
    }

    public INowyCollectionEventSubscription<TModel> Subscribe()
    {
        return new NullNowyCollectionEventSubscription<TModel>();
    }
}
