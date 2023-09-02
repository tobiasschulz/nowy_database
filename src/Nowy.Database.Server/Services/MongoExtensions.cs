using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Nowy.Database.Common.Models;
using Nowy.Database.Contract.Models;
using Nowy.Standard;

namespace Nowy.Database.Server.Services;

public static class MongoExtensions
{
    private static readonly JsonWriterSettings _json_writer_settings = new JsonWriterSettings() { OutputMode = JsonOutputMode.RelaxedExtendedJson, };

    public static async Task<MemoryStream> MongoToJsonStream(this IReadOnlyList<BsonDocument> list)
    {
        MemoryStream stream = new();

        await MongoToJsonStream(list, stream);

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    public static async Task MongoToJsonStream(this IReadOnlyList<BsonDocument> list, Stream stream)
    {
        await using StreamWriter writer = new(stream, leaveOpen: true);
        await writer.WriteAsync("[");
        await writer.FlushAsync();
        string sep = "";

        IBsonSerializer? serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
        foreach (BsonDocument document in list)
        {
            await writer.WriteAsync(sep);
            await writer.FlushAsync();
            sep = ",";

            using (JsonWriter bson_writer = new JsonWriter(writer, _json_writer_settings))
            {
                BsonSerializationContext? context = BsonSerializationContext.CreateRoot(bson_writer);
                serializer.Serialize(context, args: default, document);
            }

            await writer.FlushAsync();
        }

        await writer.WriteAsync("]");
        await writer.FlushAsync();
    }

    public static async Task<MemoryStream> MongoToJsonStream(this BsonDocument document)
    {
        MemoryStream stream = new();

        await using (StreamWriter writer = new(stream, leaveOpen: true))
        {
            IBsonSerializer? serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));

            using JsonWriter bsonWriter = new JsonWriter(writer, _json_writer_settings);
            BsonSerializationContext? context = BsonSerializationContext.CreateRoot(bsonWriter);
            serializer.Serialize(context, args: default, document);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    public static FilterDefinition<BsonDocument> ToMongoFilter(this ModelFilter filter)
    {
        if (filter.FiltersAnd is { })
        {
            return Builders<BsonDocument>.Filter.And(filter.FiltersAnd.Select(ToMongoFilter));
        }

        if (filter.FiltersOr is { })
        {
            return Builders<BsonDocument>.Filter.And(filter.FiltersOr.Select(ToMongoFilter));
        }

        if (filter.FilterNot is { })
        {
            return Builders<BsonDocument>.Filter.Not(ToMongoFilter(filter.FilterNot));
        }

        string? property = filter.Property;
        if (property == nameof(BaseModel.id)) property = "_id";
        if (property == nameof(BaseModel.ids)) property = "_ids";

        string? value = filter.Value;
        IReadOnlyList<string>? values = filter.Values;

        switch (filter.Operator)
        {
            case ModelFilterOperator.EXIST:
                if (value is not null) throw new ArgumentOutOfRangeException(nameof(value));
                if (values is not null) throw new ArgumentOutOfRangeException(nameof(values));

                return Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Exists(property),
                    Builders<BsonDocument>.Filter.Eq(property, BsonNull.Value)
                );

            case ModelFilterOperator.EQUAL:
                if (value is null) throw new ArgumentNullException(nameof(value));
                if (values is not null) throw new ArgumentOutOfRangeException(nameof(values));

                switch (filter.DataType)
                {
                    case ModelFilterDataType.LONG:
                        return Builders<BsonDocument>.Filter.Eq(property, value.ToLong());
                    case ModelFilterDataType.DOUBLE:
                        return Builders<BsonDocument>.Filter.Eq(property, value.ToDouble());
                    case ModelFilterDataType.BOOL:
                        if (value == "1")
                        {
                            return Builders<BsonDocument>.Filter.Eq(property, true);
                        }
                        else
                        {
                            return Builders<BsonDocument>.Filter.Or(
                                Builders<BsonDocument>.Filter.Eq(property, false),
                                Builders<BsonDocument>.Filter.Eq(property, BsonNull.Value),
                                Builders<BsonDocument>.Filter.Exists(property, false)
                            );
                        }
                    case ModelFilterDataType.DATETIMEOFFSET:
                        return Builders<BsonDocument>.Filter.Eq(property,
                            DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture));
                    case ModelFilterDataType.STRING:
                        return Builders<BsonDocument>.Filter.Eq(property, value);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(filter.DataType));
                }

            case ModelFilterOperator.IN:
                if (values is null) throw new ArgumentNullException(nameof(values));
                if (value is not null) throw new ArgumentOutOfRangeException(nameof(value));

                switch (filter.DataType)
                {
                    case ModelFilterDataType.LONG:
                        return Builders<BsonDocument>.Filter.In(property, values.Select(value => value.ToLong()));
                    case ModelFilterDataType.DOUBLE:
                        return Builders<BsonDocument>.Filter.In(property, values.Select(value => value.ToDouble()));
                    case ModelFilterDataType.BOOL:
                        return Builders<BsonDocument>.Filter.In(property, values.Select(value => value == "1"));
                    case ModelFilterDataType.DATETIMEOFFSET:
                        return Builders<BsonDocument>.Filter.In(property,
                            values.Select(value => DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture)));
                    case ModelFilterDataType.STRING:
                        return Builders<BsonDocument>.Filter.In(property, values);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(filter.DataType));
                }

            case ModelFilterOperator.LESS:
                if (value is null) throw new ArgumentNullException(nameof(value));
                if (values is not null) throw new ArgumentOutOfRangeException(nameof(values));

                switch (filter.DataType)
                {
                    case ModelFilterDataType.LONG:
                        return Builders<BsonDocument>.Filter.Lt(property, value.ToLong());
                    case ModelFilterDataType.DOUBLE:
                        return Builders<BsonDocument>.Filter.Lt(property, value.ToDouble());
                    case ModelFilterDataType.DATETIMEOFFSET:
                        return Builders<BsonDocument>.Filter.Lt(property,
                            DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture));
                    default:
                        throw new ArgumentOutOfRangeException(nameof(filter.DataType));
                }

            case ModelFilterOperator.LESS_OR_EQUAL:
                if (value is null) throw new ArgumentNullException(nameof(value));
                if (values is not null) throw new ArgumentOutOfRangeException(nameof(values));

                switch (filter.DataType)
                {
                    case ModelFilterDataType.LONG:
                        return Builders<BsonDocument>.Filter.Lte(property, value.ToLong());
                    case ModelFilterDataType.DOUBLE:
                        return Builders<BsonDocument>.Filter.Lte(property, value.ToDouble());
                    case ModelFilterDataType.DATETIMEOFFSET:
                        return Builders<BsonDocument>.Filter.Lte(property,
                            DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture));
                    default:
                        throw new ArgumentOutOfRangeException(nameof(filter.DataType));
                }

            case ModelFilterOperator.GREATER:
                if (value is null) throw new ArgumentNullException(nameof(value));
                if (values is not null) throw new ArgumentOutOfRangeException(nameof(values));

                switch (filter.DataType)
                {
                    case ModelFilterDataType.LONG:
                        return Builders<BsonDocument>.Filter.Gt(property, value.ToLong());
                    case ModelFilterDataType.DOUBLE:
                        return Builders<BsonDocument>.Filter.Gt(property, value.ToDouble());
                    case ModelFilterDataType.DATETIMEOFFSET:
                        return Builders<BsonDocument>.Filter.Gt(property,
                            DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture));
                    default:
                        throw new ArgumentOutOfRangeException(nameof(filter.DataType));
                }

            case ModelFilterOperator.GREATER_OR_EQUAL:
                if (value is null) throw new ArgumentNullException(nameof(value));
                if (values is not null) throw new ArgumentOutOfRangeException(nameof(values));

                switch (filter.DataType)
                {
                    case ModelFilterDataType.LONG:
                        return Builders<BsonDocument>.Filter.Gte(property, value.ToLong());
                    case ModelFilterDataType.DOUBLE:
                        return Builders<BsonDocument>.Filter.Gte(property, value.ToDouble());
                    case ModelFilterDataType.DATETIMEOFFSET:
                        return Builders<BsonDocument>.Filter.Gte(property,
                            DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture));
                    default:
                        throw new ArgumentOutOfRangeException(nameof(filter.DataType));
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(filter.Operator));
        }
    }
}
