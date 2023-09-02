namespace Nowy.Database.Contract.Models;

public interface IUniqueModel : IBaseModel
{
    IEnumerable<string> GetUniqueKeys();
}
