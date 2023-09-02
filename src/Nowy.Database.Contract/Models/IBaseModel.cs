namespace Nowy.Database.Contract.Models;

public interface IBaseModel
{
    string id { get; set; }
    IReadOnlyList<string> ids { get; set; }
    bool is_modified { get; set; }
    bool is_deleted { get; set; }

    Dictionary<string, string?>? meta { get; set; }

    Dictionary<string, string?>? meta_temp { get; set; }
}
