// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="DateOnly"/>.
/// </summary>
public class DateOnlySerializer : StructSerializerBase<DateOnly>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        var dateTime = value.ToDateTime(new TimeOnly(12, 0));
        var ticks = BsonUtils.ToMillisecondsSinceEpoch(dateTime);
        context.Writer.WriteDateTime(ticks);
    }

    /// <inheritdoc/>
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = context.Reader.ReadDateTime();
        var dateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(ticks);
        return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
    }
}
