using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nowy.Database.Common.Models;
using Nowy.Database.Common.Services;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;
using Nowy.Standard;

namespace Nowy.Database.Client.Services;

// ReSharper disable ArrangeMethodOrOperatorBody
// ReSharper disable MemberCanBePrivate.Global
public sealed record ModelTypeSettings(string DatabaseName);

internal sealed class DefaultNowyDatabaseCacheService : BackgroundService, INowyDatabaseCacheService
{
    private readonly ILogger _logger;
    private readonly INowyDatabase _nowy_database;
    private readonly IEnumerable<IDatabaseStaticDataImporter> _static_data_importers;

    private static readonly object _lock = new();
    private readonly Dictionary<(Type model_type, string uuid), IBaseModel> _models = new();
    private ImmutableDictionary<Type, ModelTypeSettings> _model_types = ImmutableDictionary<Type, ModelTypeSettings>.Empty;

    private static readonly Lazy<BlockingCollection<Func<Task>>> _lazy_tasks = new();
    private static BlockingCollection<Func<Task>> _tasks => _lazy_tasks.Value;

    public UnixTimestamp LatestInteractionTimestamp { get; set; } = UnixTimestamp.Epoch;

    public static class MetaTemp
    {
        public static string timestamp_database_insert = nameof(timestamp_database_insert);
        public static string timestamp_database_update = nameof(timestamp_database_update);
    }


    public DefaultNowyDatabaseCacheService(
        ILogger<DefaultNowyDatabaseCacheService> logger,
        INowyDatabase nowy_database,
        IEnumerable<IDatabaseStaticDataImporter> static_data_importers
    )
    {
        this._logger = logger;
        this._nowy_database = nowy_database;
        this._static_data_importers = static_data_importers;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await Task.Run(async () =>
        {
            this.Load();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Func<Task> task = _tasks.Take(stoppingToken);

                    await task.Invoke();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, $"Exception in database task");
                }
            }
        });
    }

    private void _triggerLoop()
    {
        _tasks.Add(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            await this._loopAsync();

            this._triggerLoop();
        });
    }

    public void RegisterModel<TModel>(string database_name) where TModel : class, IBaseModel
    {
        lock (_lock)
        {
            this._model_types = this._model_types.SetItem(typeof(TModel), new ModelTypeSettings(DatabaseName: database_name));
        }
    }

    public IReadOnlyDictionary<(Type model_type, string uuid), IBaseModel> GetAllModels()
    {
        lock (_lock)
        {
            return this._models.ToDictionarySafe(o => o.Key, o => o.Value);
        }
    }

    public void Add(IBaseModel item)
    {
        lock (_lock)
        {
            Type item_type = item.GetType();
            this._models[( item_type, item.id ?? string.Empty )] = item;
        }
    }

    public void Delete(IBaseModel item)
    {
        lock (_lock)
        {
            this._models.Remove(( item.GetType(), item.id ?? string.Empty ));
        }
    }

    public void AddRange(IEnumerable<IBaseModel> collection)
    {
        this.AddRange(collection, out int count_updated);
    }

    public void AddRange(IEnumerable<IBaseModel> collection, out int count_updated)
    {
        count_updated = 0;

        lock (_lock)
        {
            foreach (IBaseModel item in collection)
            {
                Type item_type = item.GetType();
                (Type item_type, string) models_key = ( item_type, item.id ?? string.Empty );
                this._models.TryGetValue(models_key, out IBaseModel? existing_item);

                long item_timestamp_database_update = item.meta_temp is { } mt ? mt.Get(MetaTemp.timestamp_database_update, "0").ToLong() : 0;
                long existing_item_timestamp_database_update = existing_item?.meta_temp is { } mt2 ? mt2.Get(MetaTemp.timestamp_database_update, "0").ToLong() : 0;

                if (existing_item is null || item_timestamp_database_update > existing_item_timestamp_database_update)
                {
                    this._models[models_key] = item;
                    count_updated++;
                }
            }
        }
    }

    public IBaseModel[] Fetch(Predicate<IBaseModel> predicate)
    {
        IBaseModel[] ret;
        lock (_lock)
        {
            ret = this._models.Values.Where(o => predicate(o)).ToArray();
        }

        return ret;
    }

    public IBaseModel[] Fetch(Func<IEnumerable<IBaseModel>, IEnumerable<IBaseModel>>? query_transform = null)
    {
        IBaseModel[] ret;
        lock (_lock)
        {
            IEnumerable<IBaseModel> query = this._models.Values;
            if (query_transform is { })
                query = query_transform(query);
            ret = query.ToArray();
        }

        return ret;
    }

    public TModel[] FetchBy<TModel>(Predicate<TModel> predicate, Func<IEnumerable<TModel>, IEnumerable<TModel>>? query_transform = null) where TModel : class, IBaseModel
    {
        TModel[] ret;
        lock (_lock)
        {
            IEnumerable<TModel> query = this._models.Values.OfType<TModel>().Where(o => predicate(o));
            if (query_transform is { })
                query = query_transform(query);
            ret = query.ToArray();
        }

        return ret;
    }

    public TModel[] Fetch<TModel>(Func<IEnumerable<TModel>, IEnumerable<TModel>>? query_transform = null) where TModel : class, IBaseModel
    {
        TModel[] ret;
        lock (_lock)
        {
            IEnumerable<TModel> query = this._models.Values.OfType<TModel>();
            if (query_transform is { })
                query = query_transform(query);
            ret = query.ToArray();
        }

        return ret;
    }

    public TModel? FetchFirstOrDefault<TModel>(Predicate<TModel> predicate, Func<IEnumerable<TModel>, IEnumerable<TModel>>? query_transform = null) where TModel : class, IBaseModel
    {
        TModel? ret;
        lock (_lock)
        {
            IEnumerable<TModel> query = this._models.Values.OfType<TModel>().Where(o => predicate(o));
            if (query_transform is { })
                query = query_transform(query);
            ret = query.FirstOrDefault();
        }

        return ret;
    }

    public TModel? FetchById<TModel>(string? uuid) where TModel : class, IBaseModel
    {
        TModel? ret;
        lock (_lock)
        {
            if (this._models.TryGetValue(( typeof(TModel), uuid ?? string.Empty ), out IBaseModel? out_m))
            {
                ret = (TModel)out_m;
            }
            else
            {
                ret = null;
            }
        }

        return ret;
    }

    public IBaseModel? FetchById(string? uuid, Type model_type)
    {
        IBaseModel? ret;
        lock (_lock)
        {
            if (this._models.TryGetValue(( model_type, uuid ?? string.Empty ), out IBaseModel? out_m))
            {
                ret = (IBaseModel)out_m;
            }
            else
            {
                ret = null;
            }
        }

        return ret;
    }


    public void Load()
    {
        _tasks.Add(async () =>
        {
            await this._loadFromStorageAsync();
            await this._loadStaticDataAsync();
        });
    }

    private async Task _loadFromStorageAsync()
    {
        await this._syncWithNowyDatabaseAsync();
    }

    private async Task _loadStaticDataAsync()
    {
        foreach (IDatabaseStaticDataImporter static_data_importer in this._static_data_importers.ToArray())
        {
            await static_data_importer.LoadStaticDataAsync();
        }
    }

    public void Save()
    {
        _tasks.Add(async () =>
        {
            bool is_save_necessary;
            lock (_lock)
            {
                is_save_necessary = this._models.Values.Any(o => o.is_modified);
            }

            if (is_save_necessary)
            {
                await this._saveToStorageAsync();
            }
        });
    }

    private async Task _saveToStorageAsync()
    {
        await this._syncWithNowyDatabaseAsync();
    }

    private async Task _loopAsync()
    {
        await this._syncWithNowyDatabaseAsync();

        await Task.Delay(TimeSpan.FromSeconds(20));
    }

    private string _getStorageFilePath(Type model_type, string bucket)
    {
        if (!string.IsNullOrEmpty(bucket))
            return Path.Combine("files", $"models.{model_type.Name}.bucket-{bucket}.json");
        else
            return Path.Combine("files", $"models.{model_type.Name}.json");
    }

    private async Task _syncWithNowyDatabaseAsync()
    {
        foreach (( Type model_type, ModelTypeSettings model_type_settings ) in this._model_types)
        {
            await ( ( typeof(DefaultNowyDatabaseCacheService).GetMethod(nameof(this._syncWithNowyDatabaseWithTypeAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(model_type)
                .Invoke(this, new object?[] { model_type_settings, }) as Task ) ?? Task.CompletedTask );
        }
    }

    private async Task _syncWithNowyDatabaseWithTypeAsync<TModel>(ModelTypeSettings model_type_settings) where TModel : class, IBaseModel
    {
        Type model_type = typeof(TModel);

        INowyCollection<TModel> collection = this._nowy_database.GetCollection<TModel>(database_name: model_type_settings.DatabaseName);

        TModel[] models_to_save;
        do
        {
            lock (_lock)
            {
                models_to_save = this._models
                    .Values
                    .Where(o => o.is_modified)
                    .OfType<TModel>()
                    .ToArray();
            }

            long timestamp_millis_now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            this._logger.LogInformation($"Save {models_to_save.Length} models of type {model_type.Name}.");

            foreach (TModel model in models_to_save)
            {
                model.meta_temp ??= new();

                if (!model.meta_temp.ContainsKey(MetaTemp.timestamp_database_insert))
                {
                    model.meta_temp[MetaTemp.timestamp_database_insert] = timestamp_millis_now.ToString();
                    model.meta_temp[MetaTemp.timestamp_database_update] = timestamp_millis_now.ToString();
                    model.is_modified = false;

                    try
                    {
                        await collection.UpsertAsync(model.id, model);
                    }
                    catch (NowyDatabaseException ex) // when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                    {
                        this._logger.LogWarning($"Skipped Nowy Write error: {ex.Message}");
                    }
                }
                else
                {
                    model.meta_temp[MetaTemp.timestamp_database_update] = timestamp_millis_now.ToString();
                    model.is_modified = false;

                    await collection.UpsertAsync(model.id, model);
                }
            }
        } while (models_to_save.Length != 0);

        IReadOnlyList<TModel> models_from_mongo = await collection.GetAllAsync();

        if (models_from_mongo.Count != 0)
        {
            this.AddRange(models_from_mongo, out int count_updated);

            this._logger.LogInformation($"Load {count_updated} models of type {model_type.Name}.");
        }
    }

    /*
    protected async Task _loadFromStorageStorageFiles()
    {
        Directory.CreateDirectory("files");
        foreach (Type model_type in this._model_types)
        {
            foreach (string bucket in IBaseModelExtensions.GetStorageBuckets())
            {
                string file_path = this._getStorageFilePath(model_type, bucket: bucket);
                string file_path_temp = $"{file_path}.tmp.{StringHelper.Random.NextLong(1, 100):000}";

                Log.Information($"Read: {file_path}");

                List<dynamic>? data = ( await File.ReadAllTextAsync(file_path) )?.FromJson<List<dynamic>>(JsonPropertyName, type_name_handling: false);

                // Log.Information(data?.Take(5).ToJson(error_handling: true, type_name_handling: false, preserve_object_references: true));
                // Log.Information(data?.Take(5).Select(o => o.GetType()).ToJson(error_handling: true, type_name_handling: false, preserve_object_references: true));

                if (data is { })
                {
                    this.AddRange(data.OfType<IBaseModel>());
                }
            }
        }
    }

    protected async Task _saveToStorageStorageFiles()
    {
        IReadOnlyDictionary<(Type model_type, string uuid), IBaseModel> all_models = this.GetAllModels();

        foreach (Type model_type in this._model_types)
        {
            IEnumerable<IBaseModel> data_enumerable = all_models
                .Where(o => o.Key.model_type == model_type)
                .Select(o => o.Value);

            if (data_enumerable.Any(o => o.ShouldSave))
            {
                IBaseModel[] data = data_enumerable.ToArray();

                foreach (IBaseModel model in data)
                {
                    model.ShouldSave = false;
                }

                foreach (IGrouping<string, (string bucket, IBaseModel o)> group in data.Select(o => ( bucket: IBaseModelExtensions.GetStorageBucket(o.uuid), o ))
                             .GroupBy(o => o.bucket))
                {
                    string file_path = this._getStorageFilePath(model_type, bucket: group.Key);
                    string file_path_temp = $"{file_path}.tmp.{StringHelper.Random.NextLong(1, 100):000}";

                    Log.Information($"Write: {file_path}");

                    await using (Stream stream = File.Open(file_path_temp, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        group.ToArray().ToJson(stream, JsonPropertyName, type_name_handling: false, preserve_object_references: true);
                    }

                    File.Move(file_path_temp, file_path, true);
                }
            }
        }
    }
    */
}
