// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Globalization;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using MongoDB.Bson;
using NJsonSchema;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IExpandoObjectConverter"/>.
/// </summary>
public class ExpandoObjectConverter : IExpandoObjectConverter
{
    readonly ITypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
    public ExpandoObjectConverter(ITypeFormats typeFormats) =>
        _typeFormats = typeFormats;

    /// <inheritdoc/>
    public BsonDocument ToBson(ExpandoObject expandoObject, JsonSchema schema)
    {
        var document = new BsonDocument();
        foreach (var keyValue in expandoObject as IDictionary<string, object?>)
        {
            BsonValue value = BsonNull.Value;

            var name = keyValue.Key;
            if (!schema.ActualProperties.ContainsKey(name))
            {
                ConvertUnknownSchemaTypeToBsonValue(keyValue.Value);
            }
            else
            {
                var schemaProperty = schema.ActualProperties[name];
                value = ConvertToBsonValue(keyValue.Value, schemaProperty);
            }

            name = GetNameForPropertyInBsonDocument(name);
            document[name] = value;
        }

        return document;
    }

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(BsonDocument document, JsonSchema schema)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object?>;

        foreach (var element in document.Elements)
        {
            object? value;
            var name = GetNameForPropertyInExpandoObject(element);

            if (!schema.ActualProperties.ContainsKey(name))
            {
                value = ConvertUnknownSchemaTypeToClrType(element.Value);
            }
            else
            {
                var schemaProperty = schema.ActualProperties[name];
                value = ConvertFromBsonValue(element.Value, schemaProperty);
            }
            expandoObjectAsDictionary[name] = value;
        }

        return expandoObject;
    }

    BsonValue ConvertToBsonValue(object? value, JsonSchemaProperty schemaProperty)
    {
        if (value is ExpandoObject expando)
        {
            return ToBson(expando, schemaProperty.IsArray ?
                schemaProperty.Item.Reference ?? schemaProperty.Item :
                schemaProperty.ActualTypeSchema);
        }

        BsonValue result;
        if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            result = ConvertToBsonValueBasedOnSchemaType(value, schemaProperty);
        }
        else
        {
            result = ConvertToBsonValueFromUnknownFormat(value, schemaProperty);
        }

        if (result == BsonNull.Value && value is IEnumerable enumerable)
        {
            var items = new List<BsonValue>();
            foreach (var item in enumerable)
            {
                items.Add(ConvertToBsonValue(item, schemaProperty));
            }
            return new BsonArray(items);
        }
        return result;
    }

    object? ConvertFromBsonValue(BsonValue bsonValue, JsonSchemaProperty schemaProperty)
    {
        if (bsonValue is BsonDocument childDocument)
        {
            return ToExpandoObject(childDocument, schemaProperty.IsArray ?
                schemaProperty.Item.Reference ?? schemaProperty.Item :
                schemaProperty.ActualTypeSchema);
        }
        else if (bsonValue is BsonArray array)
        {
            return array.Select(_ => ConvertFromBsonValue(_, schemaProperty)).ToArray();
        }
        else if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            return ConvertBsonValueToSchemaType(bsonValue, schemaProperty);
        }
        else
        {
            return ConvertBsonValueFromUnknownFormat(bsonValue, schemaProperty);
        }
    }

    BsonValue ConvertUnknownSchemaTypeToBsonValue(object? value)
    {
        if (value is ExpandoObject expandoObject)
        {
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            var document = new BsonDocument();

            foreach (var kvp in expandoObjectAsDictionary)
            {
                document[GetNameForPropertyInBsonDocument(kvp.Key)] = ConvertUnknownSchemaTypeToBsonValue(kvp.Value);
            }

            return document;
        }

        if (value is IEnumerable enumerable)
        {
            var array = new BsonArray();

            foreach (var item in enumerable)
            {
                array.Add(ConvertUnknownSchemaTypeToBsonValue(item));
            }

            return array;
        }

        return ConvertToBsonValueFromClrType(value);
    }

    object? ConvertUnknownSchemaTypeToClrType(BsonValue value)
    {
        if (value is BsonDocument document)
        {
            var expandoObject = new ExpandoObject();
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            foreach (var element in document.Elements)
            {
                expandoObjectAsDictionary[GetNameForPropertyInExpandoObject(element)] = ConvertUnknownSchemaTypeToClrType(element.Value)!;
            }
            return expandoObject;
        }

        if (value is BsonArray array)
        {
            return array.Select(_ => ConvertUnknownSchemaTypeToClrType(value)).ToArray();
        }

        switch (value.BsonType)
        {
            case BsonType.Double:
                return value.AsDouble;
            case BsonType.String:
                return value.AsString;
            case BsonType.ObjectId:
                return value.AsObjectId;
            case BsonType.Boolean:
                return value.AsBoolean;
            case BsonType.DateTime:
                return value.ToUniversalTime();
            case BsonType.Int32:
                return value.AsInt32;
            case BsonType.Int64:
                return value.AsInt64;
            case BsonType.Decimal128:
                return value.AsDecimal128;
        }

        return null;
    }

    BsonValue ConvertToBsonValueFromUnknownFormat(object? value, JsonSchemaProperty schemaProperty)
    {
        if (value is null)
        {
            return BsonNull.Value;
        }

        switch (schemaProperty.Type)
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

    object? ConvertBsonValueFromUnknownFormat(BsonValue value, JsonSchemaProperty schemaProperty)
    {
        switch (schemaProperty.Type)
        {
            case JsonObjectType.String:
                return schemaProperty.Format == "guid" ?
                        Guid.Parse(value.ToString()!) :
                        value.ToString();

            case JsonObjectType.Boolean:
                return value.ToBoolean();

            case JsonObjectType.Integer:
                return value.ToInt32();

            case JsonObjectType.Number:
                return value.ToDouble();
        }

        return null!;
    }

    BsonValue ConvertToBsonValueBasedOnSchemaType(object? input, JsonSchemaProperty schemaProperty)
    {
        if (input is null)
        {
            return BsonNull.Value;
        }

        var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
        input = TypeConversion.Convert(targetType, input);

        return ConvertToBsonValueFromClrType(input);
    }

    BsonValue ConvertToBsonValueFromClrType(object? input)
    {
        if (input is null)
        {
            return BsonNull.Value;
        }

        if (input.IsConcept())
        {
            input = input.GetConceptValue();
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

    object? ConvertBsonValueToSchemaType(BsonValue value, JsonSchemaProperty schemaProperty)
    {
        var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);

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

    string GetNameForPropertyInExpandoObject(BsonElement element)
    {
        var name = element.Name;
        if (name == "_id")
        {
            name = "id";
        }
        return name;
    }

    string GetNameForPropertyInBsonDocument(string name)
    {
        if (name == "id")
        {
            return "_id";
        }
        return name;
    }
}
