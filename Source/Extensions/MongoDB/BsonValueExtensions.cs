// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.Types;
using MongoDB.Bson;
using NJsonSchema;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="BsonValue"/>.
/// </summary>
public static class BsonValueExtensions
{
    /// <summary>
    /// Convert a CLR value to the correct <see cref="BsonValue"/>.
    /// </summary>
    /// <param name="input">Input value to convert from.</param>
    /// <param name="targetType">Optional target type.</param>
    /// <returns>Converted <see cref="BsonValue"/>.</returns>
    public static BsonValue ToBsonValue(this object? input, Type? targetType = default)
    {
        if (input is null)
        {
            return BsonNull.Value;
        }

        if (input.IsConcept())
        {
            input = input.GetConceptValue();
        }

        if (targetType is not null)
        {
            input = TypeConversion.Convert(targetType, input);
        }

        switch (input)
        {
            case short actualValue:
                return new BsonInt32(actualValue);

            case ushort actualValue:
                return new BsonInt32(actualValue);

            case int actualValue:
                return new BsonInt32(actualValue);

            case uint actualValue:
                return new BsonInt32((int)actualValue);

            case long actualValue:
                return new BsonInt64(actualValue);

            case ulong actualValue:
                return new BsonInt64((long)actualValue);

            case float actualValue:
                return new BsonDouble(actualValue);

            case double actualValue:
                return new BsonDouble(actualValue);

            case decimal actualValue:
                return new BsonDecimal128(actualValue);

            case bool actualValue:
                return new BsonBoolean(actualValue);

            case string actualValue:

                // if (Guid.TryParse(actualValue, out var actualValueAsGuid))
                // {
                //     return new BsonBinaryData(actualValueAsGuid, GuidRepresentation.Standard);
                // }
                return new BsonString(actualValue);

            case DateTime actualValue:
                return new BsonDateTime(actualValue);

            case DateTimeOffset actualValue:
                return new BsonDateTime(actualValue.ToUnixTimeMilliseconds());

            case DateOnly actualValue:
                return new BsonDateTime(actualValue.ToDateTime(new TimeOnly(12, 0)));

            case TimeOnly actualValue:
                return new BsonDateTime(BsonUtils.ToMillisecondsSinceEpoch(DateTime.UnixEpoch + actualValue.ToTimeSpan()));

            case Guid actualValue:
                return new BsonBinaryData(actualValue, GuidRepresentation.Standard);
        }

        return BsonNull.Value;
    }

    /// <summary>
    /// Convert a <see cref="BsonValue"/> to a specific target type.
    /// </summary>
    /// <param name="value"><see cref="BsonValue"/> to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>Converted value.</returns>
    public static object? ToTargetType(this BsonValue value, Type targetType)
    {
        switch (Type.GetTypeCode(targetType))
        {
            case TypeCode.Int16:
                return (short)value.ToInt32();

            case TypeCode.UInt16:
                return (ushort)value.ToInt32();

            case TypeCode.Int32:
                return value.ToInt32();

            case TypeCode.UInt32:
                return (uint)value.ToInt32();

            case TypeCode.Int64:
                return value.ToInt64();

            case TypeCode.UInt64:
                return (ulong)value.ToInt64();

            case TypeCode.Single:
                return (float)value.ToDouble();

            case TypeCode.Double:
                return value.ToDouble();

            case TypeCode.Decimal:
                return value.ToDecimal();

            case TypeCode.DateTime:
                return value.ToUniversalTime();

            case TypeCode.Byte:
                return (byte)value.ToInt32();
        }

        if (targetType == typeof(Guid))
        {
            if (value is BsonString bsonString)
            {
                return Guid.Parse(bsonString.ToString()!);
            }

            if (value is BsonBinaryData bsonBinaryData)
            {
                return bsonBinaryData.ToGuid(GuidRepresentation.Standard);
            }
        }

        if (targetType == typeof(DateTimeOffset))
        {
            if (value is BsonDateTime bsonDateTime)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(bsonDateTime.MillisecondsSinceEpoch);
            }

            if (value is BsonString bsonString)
            {
                return DateTimeOffset.ParseExact(bsonString.ToString()!, DateTimeOffsetSupportingBsonDateTimeSerializer.StringSerializationFormat, DateTimeFormatInfo.InvariantInfo);
            }
        }

        if (targetType == typeof(DateOnly) && value is BsonDateTime bsonDateOnly)
        {
            var dateTime = bsonDateOnly.ToUniversalTime();
            return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        if (targetType == typeof(TimeOnly) && value is BsonDateTime bsonTimeOnly)
        {
            var dateTime = bsonTimeOnly.ToUniversalTime();
            return new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
        }

        return null;
    }

    /// <summary>
    /// Convert to a <see cref="BsonValue"/> based on the type information in <see cref="JsonSchemaProperty"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    /// <param name="schemaProperty"><see cref="JsonSchemaProperty"/> with type info.</param>
    /// <returns>Converted value.</returns>
    public static BsonValue ToBsonValueBasedOnSchemaPropertyType(this object? value, JsonSchema schemaProperty)
    {
        if (value is null)
        {
            return BsonNull.Value;
        }

        var type = (schemaProperty.Type == JsonObjectType.None && schemaProperty.HasReference) ?
                    schemaProperty.Reference.Type :
                    schemaProperty.Type;

        switch (type)
        {
            case JsonObjectType.String:
                return new BsonString(value is string actualString ? actualString : value.ToString()!);

            case JsonObjectType.Boolean:
                return new BsonBoolean(value is bool actualBool ? actualBool : bool.Parse(value.ToString()!));

            case JsonObjectType.Integer:
                return new BsonInt32(value is int actualInt ? actualInt : int.Parse(value.ToString()!));

            case JsonObjectType.Number:
                return new BsonDouble(value is double actualDouble ? actualDouble : double.Parse(value.ToString()!));
        }

        return BsonNull.Value;
    }
}
