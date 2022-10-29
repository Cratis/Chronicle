// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Aksio.Cratis.Schemas;
using MongoDB.Bson;
using NJsonSchema;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IExpandoObjectConverter"/>.
/// </summary>
public class ExpandoObjectConverter : IExpandoObjectConverter
{
    readonly ITypeFormats _typeFormats;

    public ExpandoObjectConverter(ITypeFormats typeFormats) =>
        _typeFormats = typeFormats;

    /// <inheritdoc/>
    public BsonDocument ToBson(ExpandoObject instance, JsonSchema schema) => throw new NotImplementedException();

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(BsonDocument document, JsonSchema schema)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object?>;

        foreach (var element in document.Elements)
        {
            object? value;

            var name = element.Name;
            if (name == "_id")
            {
                name = "id";
            }

            if (!schema.ActualProperties.ContainsKey(name))
            {
                value = ConvertUnknownSchemaTypeToClrType(element);
            }
            else
            {
                var schemaProperty = schema.ActualProperties[name];
                value = ConvertBsonValue(element.Value, schemaProperty);
            }
            expandoObjectAsDictionary[name] = value;
        }

        return expandoObject;
    }

    object? ConvertBsonValue(BsonValue bsonValue, JsonSchemaProperty schemaProperty)
    {
        if (bsonValue is BsonDocument childDocument)
        {
            return ToExpandoObject(childDocument, schemaProperty.IsArray ?
                schemaProperty.Item.Reference ?? schemaProperty.Item :
                schemaProperty.ActualTypeSchema);
        }
        else if (bsonValue is BsonArray array)
        {
            return array.Select(_ => ConvertBsonValue(_, schemaProperty)).ToArray();
        }
        else if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            return ConvertElementToSchemaType(bsonValue, schemaProperty);
        }
        else
        {
            return ConvertUnknownFormatElementToSchemaType(bsonValue, schemaProperty);
        }
    }

    object? ConvertUnknownSchemaTypeToClrType(BsonElement element)
    {
        switch (element.Value.BsonType)
        {
            case BsonType.Double:
                return element.Value.AsDouble;
            case BsonType.String:
                return element.Value.AsString;
            case BsonType.ObjectId:
                return element.Value.AsObjectId;
            case BsonType.Boolean:
                return element.Value.AsBoolean;
            case BsonType.DateTime:
                return element.Value.ToUniversalTime();
            case BsonType.Int32:
                return element.Value.AsInt32;
            case BsonType.Int64:
                return element.Value.AsInt64;
            case BsonType.Decimal128:
                return element.Value.AsDecimal128;
        }

        return null;
    }

    object? ConvertUnknownFormatElementToSchemaType(BsonValue value, JsonSchemaProperty schemaProperty)
    {
        switch (schemaProperty.Type)
        {
            case JsonObjectType.String:
                return schemaProperty.Format == "guid" ?
                        Guid.Parse(value.ToString()!) :
                        value.ToString();

            case JsonObjectType.Boolean:
                return value.ToBoolean();
        }

        return null!;
    }

    object? ConvertElementToSchemaType(BsonValue value, JsonSchemaProperty schemaProperty)
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
            return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        return null;
    }
}
