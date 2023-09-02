namespace Nowy.Database.Contract.Models;

[AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public class EntityNameAttribute : System.Attribute
{
    private string _name;

    public EntityNameAttribute(string name)
    {
        _name = name;
    }

    public string GetName() => _name;
}
