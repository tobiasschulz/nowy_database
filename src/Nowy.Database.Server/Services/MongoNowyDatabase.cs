using Microsoft.Extensions.Logging;
using Nowy.Database.Common.Services;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;

namespace Nowy.Database.Server.Services;

internal class MongoNowyDatabase : INowyDatabase
{
    private readonly ILogger _logger;
    private readonly MongoRepository _repo;

    public MongoNowyDatabase(ILogger<MongoNowyDatabase> logger, MongoRepository repo)
    {
        this._logger = logger;
        this._repo = repo;
    }

    public INowyCollection<TModel> GetCollection<TModel>(string database_name) where TModel : class, IBaseModel
    {
        string entity_name = EntityNameHelper.GetEntityName(typeof(TModel));

        return new MongoNowyCollection<TModel>(
            logger: this._logger,
            repo: this._repo,
            database_name: database_name,
            entity_name: entity_name
        );
    }
}
