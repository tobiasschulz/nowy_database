using Nowy.Database.Contract.Models;

namespace Nowy.Database.Contract.Services;

public interface INowyCollection<TModel> where TModel : class, IBaseModel
{
    string DatabaseName { get; }
    string EntityName { get; }

    Task<IReadOnlyList<TModel>> GetAllAsync(QueryOptions? options = null);
    Task<IReadOnlyList<TModel>> GetByFilterAsync(ModelFilter filter, QueryOptions? options = null);
    Task<TModel?> GetByIdAsync(string id, QueryOptions? options = null);
    Task<TModel> UpsertAsync(string id, TModel model);
    Task DeleteAsync(string id);
    INowyCollectionEventSubscription<TModel> Subscribe();
}

public readonly record struct QueryOptions(bool with_deleted);

public sealed class NowyDatabaseException : Exception
{
}
