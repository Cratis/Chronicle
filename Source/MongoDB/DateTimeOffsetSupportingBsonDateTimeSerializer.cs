// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

#pragma warning disable AS0008

namespace Cratis.MongoDB;

/// <summary>
/// Represents a serializer for handling serialization of <see cref="DateTimeOffset"/> to and from MongoDB.
/// </summary>
/// <remarks>
/// Based on this: https://www.codeproject.com/Tips/1268086/MongoDB-Csharp-Serializer-for-DateTimeOffset-to-Bs.
/// </remarks>
public class DateTimeOffsetSupportingBsonDateTimeSerializer : StructSerializerBase<DateTimeOffset>,
             IRepresentationConfigurable<DateTimeOffsetSupportingBsonDateTimeSerializer>
{
    /// <summary>
    /// The serialization format used.
    /// </summary>
    public static readonly string StringSerializationFormat = "YYYY-MM-ddTHH:mm:ss.FFFFFFK";

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeOffsetSupportingBsonDateTimeSerializer"/> class.
    /// </summary>
    public DateTimeOffsetSupportingBsonDateTimeSerializer() : this(BsonType.DateTime)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeOffsetSupportingBsonDateTimeSerializer"/> class.
    /// </summary>
    /// <param name="representation"><see cref="BsonType"/> representation.</param>
    public DateTimeOffsetSupportingBsonDateTimeSerializer(BsonType representation)
    {
        switch (representation)
        {
            case BsonType.String:
            case BsonType.DateTime:
                break;
            default:
                throw new ArgumentException($"{representation} is not a valid representation for {GetType().Name}");
        }

        Representation = representation;
    }

    /// <inheritdoc/>
    public BsonType Representation { get; }

    /// <inheritdoc/>
    public override DateTimeOffset Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;

        var bsonType = bsonReader.GetCurrentBsonType();
        switch (bsonType)
        {
            case BsonType.String:
                var stringValue = bsonReader.ReadString();
                return DateTimeOffset.ParseExact(stringValue, StringSerializationFormat, DateTimeFormatInfo.InvariantInfo);

            case BsonType.DateTime:
                var dateTimeValue = bsonReader.ReadDateTime();
                return DateTimeOffset.FromUnixTimeMilliseconds(dateTimeValue);

            default:
                throw new FormatException($"Cannot deserialize a '{BsonUtils.GetFriendlyTypeName(ValueType)}' from BsonType '{bsonType}'.");
        }
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTimeOffset value)
    {
        var bsonWriter = context.Writer;

        switch (Representation)
        {
            case BsonType.String:
                bsonWriter.WriteString(value.ToString(StringSerializationFormat, DateTimeFormatInfo.InvariantInfo));
                break;

            case BsonType.DateTime:
                bsonWriter.WriteDateTime(value.ToUnixTimeMilliseconds());
                break;

            default:
                var message = $"'{Representation}' is not a valid DateTimeOffset representation.";
                throw new BsonSerializationException(message);
        }
    }

    /// <inheritdoc/>
    public DateTimeOffsetSupportingBsonDateTimeSerializer WithRepresentation(BsonType representation)
    {
        if (representation == Representation)
        {
            return this;
        }
        return new DateTimeOffsetSupportingBsonDateTimeSerializer(representation);
    }

    /// <inheritdoc/>
    IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
    {
        return WithRepresentation(representation);
    }
}
