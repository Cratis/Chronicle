// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="TimeOnly"/>.
/// </summary>
public class TimeOnlySerializer : StructSerializerBase<TimeOnly>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
    {
        var dateTime = DateTime.MinValue;
        dateTime += value.ToTimeSpan();
        var ticks = BsonUtils.ToMillisecondsSinceEpoch(dateTime);
        context.Writer.WriteDateTime(ticks);
    }

    /// <inheritdoc/>
    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = context.Reader.ReadDateTime();
        var dateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(ticks);
        return new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
    }
}
