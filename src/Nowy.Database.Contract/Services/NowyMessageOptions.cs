namespace Nowy.Database.Contract.Services;

public readonly record struct NowyMessageOptions(bool ExceptSender, string[] Recipients, TimeSpan? SendDelay = null)
{
}
