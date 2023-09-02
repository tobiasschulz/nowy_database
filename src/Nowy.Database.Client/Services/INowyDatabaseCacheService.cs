using Microsoft.Extensions.Hosting;
using Nowy.Database.Common.Models;
using Nowy.Database.Contract.Models;

namespace Nowy.Database.Client.Services;

internal interface INowyDatabaseCacheService : IHostedService
{
    IBaseModel[] Fetch(Predicate<IBaseModel> predicate);


    IBaseModel[] Fetch(Func<IEnumerable<IBaseModel>, IEnumerable<IBaseModel>>? query_transform = null);


    TModel[] FetchBy<TModel>(Predicate<TModel> predicate, Func<IEnumerable<TModel>, IEnumerable<TModel>>? query_transform = null) where TModel : class, IBaseModel;


    TModel[] Fetch<TModel>(Func<IEnumerable<TModel>, IEnumerable<TModel>>? query_transform = null) where TModel : class, IBaseModel;


    TModel? FetchFirstOrDefault<TModel>(Predicate<TModel> predicate, Func<IEnumerable<TModel>, IEnumerable<TModel>>? query_transform = null)
        where TModel : class, IBaseModel;


    TModel? FetchById<TModel>(string? uuid) where TModel : class, IBaseModel;


    IBaseModel? FetchById(string? uuid, Type model_type);

    void Add(IBaseModel item);

    void AddRange(IEnumerable<IBaseModel> collection);

    void Delete(IBaseModel item);

    void Save();
}
