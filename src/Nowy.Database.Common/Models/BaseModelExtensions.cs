using System.Security.Cryptography;
using System.Text;

namespace Nowy.Database.Common.Models;

public static class BaseModelExtensions
{
    private static readonly object _lock_storage_buckets = new();
    private static readonly Dictionary<string, string> _cache_storage_buckets = new();
    private static readonly HashSet<string> _cache_storage_buckets_values = new();

    public static string GetStorageBucket(string key)
    {
        key ??= string.Empty;

        string? ret;
        lock (_lock_storage_buckets)
        {
            _cache_storage_buckets.TryGetValue(key, out ret);
        }

        if (!string.IsNullOrEmpty(ret))
        {
            return ret;
        }
        else
        {
            using SHA256 algorithm = SHA256.Create();

            byte[] hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(key));
            ret = BitConverter.ToUInt64(hash, 0).ToString().Substring(0, 1);

            lock (_lock_storage_buckets)
            {
                _cache_storage_buckets[key] = ret;
                _cache_storage_buckets_values.Add(ret);
            }

            return ret;
        }
    }

    public static string[] GetStorageBuckets()
    {
        lock (_lock_storage_buckets)
        {
            foreach (string bucket in Enumerable.Range(0, 10).Select(o => o.ToString()))
            {
                _cache_storage_buckets_values.Add(bucket);
            }

            return _cache_storage_buckets_values.ToArray();
        }
    }
}
