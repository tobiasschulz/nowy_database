using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Nowy.Standard;

namespace Nowy.Database.Server.Services;

public class MongoUnixTimestampSerializer : SerializerBase<UnixTimestamp>
{
    public override UnixTimestamp Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Int64)
        {
            return UnixTimestamp.FromUnixTimeSeconds(context.Reader.ReadInt64());
        }
        else if (context.Reader.CurrentBsonType == BsonType.String)
        {
            string? value = context.Reader.ReadString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return UnixTimestamp.Epoch;
            }

            return UnixTimestamp.FromUnixTimeSeconds(value.ToLong());
        }
        else
        {
            context.Reader.SkipValue();
            return UnixTimestamp.Epoch;
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UnixTimestamp value)
    {
        context.Writer.WriteInt64(value.UnixTimeSeconds);
    }
}
