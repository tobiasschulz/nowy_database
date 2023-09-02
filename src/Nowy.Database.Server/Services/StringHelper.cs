namespace Nowy.Database.Server.Services;

internal static class StringHelper
{
    public static string MakeRandomUuid()
    {
        return Guid.NewGuid().ToString("D");
    }
}
