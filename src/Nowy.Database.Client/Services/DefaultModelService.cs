using Nowy.Database.Contract.Models;

namespace Nowy.Database.Client.Services;

internal sealed class DefaultModelService : IModelService
{
    public event Action<IBaseModel>? ModelUpdated;
    public void SendModelUpdated(IBaseModel model) => ModelUpdated?.Invoke(model);
}
