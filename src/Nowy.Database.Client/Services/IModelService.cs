using Nowy.Database.Contract.Models;

namespace Nowy.Database.Client.Services;

public interface IModelService
{
    event Action<IBaseModel>? ModelUpdated;
    void SendModelUpdated(IBaseModel model);
}
