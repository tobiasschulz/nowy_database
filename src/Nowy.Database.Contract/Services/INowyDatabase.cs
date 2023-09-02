using Nowy.Database.Contract.Models;

namespace Nowy.Database.Contract.Services;

public interface INowyDatabase
{
    INowyCollection<TModel> GetCollection<TModel>(string database_name) where TModel : class, IBaseModel;
}
