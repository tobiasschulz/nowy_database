using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Nowy.Database.Contract.Models;
using Nowy.Database.Server.Services;

namespace Nowy.Database.Server.Endpoints;

public class ApiEndpointsV1
{
    private readonly ILogger _logger;

    public ApiEndpointsV1(ILogger<ApiEndpointsV1> logger)
    {
        this._logger = logger;
    }

    public void MapEndpoints(RouteGroupBuilder app)
    {
        app.MapGet("{database_name}/{entity_name}", this.GetAllAsync);
        app.MapGet("{database_name}/{entity_name}/filter/{filter_json}", this.GetByFilterAsync);
        app.MapGet("{database_name}/{entity_name}/{id}", this.GetByIdAsync);
        app.MapPost("{database_name}/{entity_name}/{id}", this.UpsertAsync);
        app.MapPut("{database_name}/{entity_name}/{id}", this.UpsertAsync);
        app.MapDelete("{database_name}/{entity_name}/{id}", this.DeleteAsync);
    }

    public async Task<Results<PushStreamHttpResult, NotFound>> GetAllAsync(
        MongoRepository repo, string database_name, string entity_name
    )
    {
        this._logger.LogInformation($"{nameof(this.GetAllAsync)}: {nameof(database_name)} = {database_name}, {nameof(entity_name)} = {entity_name}");

        List<BsonDocument> list = await repo.GetAllAsync(database_name: database_name, entity_name: entity_name);

        return TypedResults.Stream(async stream => { await list.MongoToJsonStream(stream); }, "application/json");
    }

    public async Task<Results<PushStreamHttpResult, NotFound>> GetByFilterAsync(
        MongoRepository repo, string database_name, string entity_name, string filter_json
    )
    {
        this._logger.LogInformation($"{nameof(this.GetAllAsync)}: {nameof(database_name)} = {database_name}, {nameof(entity_name)} = {entity_name}");

        ModelFilter filter = JsonSerializer.Deserialize<ModelFilter>(filter_json) ?? throw new ArgumentNullException(nameof(filter_json));

        List<BsonDocument> list = await repo.GetByFilterAsync(database_name: database_name, entity_name: entity_name, filter: filter);

        return TypedResults.Stream(async stream => { await list.MongoToJsonStream(stream); }, "application/json");
    }

    public async Task<Results<FileContentHttpResult, NoContent, NotFound>> GetByIdAsync(
        MongoRepository repo, string database_name, string entity_name, string id
    )
    {
        this._logger.LogInformation($"{nameof(this.GetByIdAsync)}: {nameof(database_name)} = {database_name}, {nameof(entity_name)} = {entity_name}, {nameof(id)} = {id}");

        BsonDocument? document = await repo.GetByIdAsync(database_name: database_name, entity_name: entity_name, id: id);

        if (document is { })
        {
            await using MemoryStream stream = await document.MongoToJsonStream();
            return TypedResults.Bytes(stream.ToArray(), "application/json");
        }
        else
        {
            return TypedResults.NoContent();
        }
    }

    public async Task<Results<FileContentHttpResult, NotFound>> UpsertAsync(
        MongoRepository repo, string database_name, string entity_name, string id,
        HttpRequest request
    )
    {
        string input_json;
        await using (MemoryStream input_json_stream = new MemoryStream(2048))
        {
            await request.Body.CopyToAsync(input_json_stream);
            input_json = Encoding.UTF8.GetString(input_json_stream.ToArray());
        }

        this._logger.LogInformation(
            $"{nameof(this.UpsertAsync)}: {nameof(database_name)} = {database_name}, {nameof(entity_name)} = {entity_name}, {nameof(id)} = {id}, {nameof(input_json)} = {input_json}");

        BsonDocument input_document = BsonDocument.Parse(input_json);
        BsonDocument? document = await repo.UpsertAsync(database_name: database_name, entity_name: entity_name, id: id, input: input_document);
        await using MemoryStream stream = await document.MongoToJsonStream();

        this._logger.LogInformation($"{nameof(this.UpsertAsync)}: => {Encoding.UTF8.GetString(stream.ToArray())}");

        return TypedResults.Bytes(stream.ToArray(), "application/json");
    }

    public async Task<Results<FileContentHttpResult, NoContent, NotFound>> DeleteAsync(
        MongoRepository repo, string database_name, string entity_name, string id
    )
    {
        this._logger.LogInformation($"{nameof(this.DeleteAsync)}: {nameof(database_name)} = {database_name}, {nameof(entity_name)} = {entity_name}, {nameof(id)} = {id}");

        BsonDocument? document = await repo.DeleteAsync(database_name: database_name, entity_name: entity_name, id: id);

        if (document is { })
        {
            await using MemoryStream stream = await document.MongoToJsonStream();
            return TypedResults.Bytes(stream.ToArray(), "application/json");
        }
        else
        {
            return TypedResults.NoContent();
        }
    }
}
