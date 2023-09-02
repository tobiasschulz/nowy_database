using Nowy.Database.Contract.Models;

namespace Nowy.Database.Common.Services;

public static class EntityNameHelper
{
    public static string GetEntityName(Type type_model)
    {
        EntityNameAttribute[] attributes = type_model
            .GetCustomAttributes(typeof(EntityNameAttribute), inherit: true)
            .Cast<EntityNameAttribute>()
            .ToArray();

        string entity_name = attributes.FirstOrDefault()?.GetName()
                             ?? type_model.Name.Replace("Model", string.Empty);

        return entity_name;
    }
}
