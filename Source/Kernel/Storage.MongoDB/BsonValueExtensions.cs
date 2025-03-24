// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Cratis.Applications.MongoDB;
using Cratis.Reflection;
using Cratis.Strings;
using Cratis.Types;
using MongoDB.Bson;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB;

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
        var inputType = input.GetType();

        if (inputType.IsDictionary())
        {
            var dictionaryType = input.GetType();
            var keyType = dictionaryType.GetKeyType();
            var valueType = dictionaryType.GetValueType();
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var keyProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Key))!;
            var valueProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Value))!;

            var dictionary = input as IEnumerable;
            var dictionaryDocument = new BsonDocument();
            foreach (var keyValuePair in dictionary!)
            {
                var key = keyProperty.GetValue(keyValuePair)?.ToString() ?? string.Empty;
                dictionaryDocument[key] = ToBsonValue(valueProperty.GetValue(keyValuePair));
            }
            return dictionaryDocument;
        }

        if (inputType.IsEnumerable())
        {
            var array = new BsonArray();
            foreach (var item in (input as IEnumerable)!)
            {
                array.Add(ToBsonValue(item));
            }
            return array;
        }

        if (input.IsConcept())
        {
            input = input.GetConceptValue();
        }

        if (targetType is not null)
        {
            input = TypeConversion.Convert(targetType, input);
        }

        if (inputType.IsEnum)
        {
            input = (int)input;
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
                return new BsonInt64((int)actualValue);

            case long actualValue:
                return new BsonInt64(actualValue);

            case ulong actualValue:
                return new BsonDecimal128((long)actualValue);

            case float actualValue:
                return new BsonDouble(actualValue);

            case double actualValue:
                return new BsonDouble(actualValue);

            case decimal actualValue:
                return new BsonDecimal128(actualValue);

            case bool actualValue:
                return new BsonBoolean(actualValue);

            case string actualValue:
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

            case Type actualValue:
                return new BsonString(actualValue.AssemblyQualifiedName);

            case Uri actualValue:
                return new BsonString(actualValue.ToString());
        }

        var document = new BsonDocument();
        foreach (var property in inputType.GetProperties())
        {
            var propertyName = property.Name.ToCamelCase();
            if (propertyName == "id")
            {
                propertyName = "_id";
            }
            var propertyValue = property.GetValue(input);
            document.Add(propertyName, propertyValue.ToBsonValue()!);
        }

        return document;
    }

    /// <summary>
    /// Convert a <see cref="BsonValue"/> to a specific target type.
    /// </summary>
    /// <param name="value"><see cref="BsonValue"/> to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>Converted value.</returns>
    public static object? ToTargetType(this BsonValue value, Type targetType)
    {
        if (value is BsonNull && TryConvertNullPrimitive(targetType, out var result))
        {
            return result;
        }

        if (TryConvertPrimitive(value, targetType, out result))
        {
            return result;
        }

        if (targetType == typeof(Guid))
        {
            if (value is BsonNull)
            {
                return Guid.Empty;
            }

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
                    schemaProperty.Reference!.Type :
                    schemaProperty.Type;

        if (type.HasFlag(JsonObjectType.Array) && value is IEnumerable enumerable)
        {
            var convertedArray = new BsonArray();
            foreach (var item in enumerable)
            {
                convertedArray.Add(item.ToBsonValueBasedOnSchemaPropertyType(schemaProperty.Item!));
            }
            return convertedArray;
        }

        if (type.HasFlag(JsonObjectType.Null))
        {
            type ^= JsonObjectType.Null;
        }

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

    static bool TryConvertNullPrimitive(Type targetType, out object? result)
    {
        result = null;

        switch (Type.GetTypeCode(targetType))
        {
            case TypeCode.Int16:
                result = (short)0;
                break;

            case TypeCode.UInt16:
                result = (ushort)0;
                break;

            case TypeCode.Int32:
                result = 0;
                break;

            case TypeCode.UInt32:
                result = 0U;
                break;

            case TypeCode.Int64:
                result = 0L;
                break;

            case TypeCode.UInt64:
                result = 0UL;
                break;

            case TypeCode.Single:
                result = 0F;
                break;

            case TypeCode.Double:
                result = 0D;
                break;

            case TypeCode.Decimal:
                result = 0M;
                break;

            case TypeCode.DateTime:
                result = DateTime.MinValue;
                break;

            case TypeCode.Byte:
                result = (byte)0;
                break;
        }

        return result is not null;
    }

    static bool TryConvertPrimitive(BsonValue value, Type targetType, out object? result)
    {
        result = null;

        switch (Type.GetTypeCode(targetType))
        {
            case TypeCode.Int16:
                result = (short)value.ToInt32();
                break;

            case TypeCode.UInt16:
                result = (ushort)value.ToInt32();
                break;

            case TypeCode.Int32:
                result = value.ToInt32();
                break;

            case TypeCode.UInt32:
                result = (uint)value.ToInt32();
                break;

            case TypeCode.Int64:
                result = value.ToInt64();
                break;

            case TypeCode.UInt64:
                result = (ulong)value.ToInt64();
                break;

            case TypeCode.Single:
                result = (float)value.ToDouble();
                break;

            case TypeCode.Double:
                result = value.ToDouble();
                break;

            case TypeCode.Decimal:
                result = value.ToDecimal();
                break;

            case TypeCode.DateTime:
                result = value.ToUniversalTime();
                break;

            case TypeCode.Byte:
                result = (byte)value.ToInt32();
                break;
        }

        return result is not null;
    }
}
