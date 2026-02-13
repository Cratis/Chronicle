// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Cratis.Reflection;
using MongoDB.Bson;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IExpandoObjectConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
public class ExpandoObjectConverter(ITypeFormats typeFormats) : IExpandoObjectConverter
{
    /// <inheritdoc/>
    public BsonDocument ToBsonDocument(ExpandoObject expandoObject, JsonSchema schema)
    {
        var document = new BsonDocument();
        var schemaProperties = schema.GetFlattenedProperties();
        foreach (var keyValue in expandoObject as IDictionary<string, object?>)
        {
            BsonValue value = BsonNull.Value;

            var name = keyValue.Key;
            var schemaProperty = schemaProperties.SingleOrDefault(_ => _.Name == name);
            if (schemaProperty is null && name == "id")
            {
                schemaProperty = schemaProperties.SingleOrDefault(_ => _.Name == "Id");
            }

            if (schemaProperty is null)
            {
                value = ConvertUnknownSchemaTypeToBsonValue(keyValue.Value);
            }
            else
            {
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
        var schemaProperties = schema.GetFlattenedProperties();

        foreach (var element in document.Elements)
        {
            object? value;
            var name = GetNameForPropertyInExpandoObject(element);

            var schemaProperty = schemaProperties.SingleOrDefault(_ => _.Name == name);
            if (schemaProperty is null && name == "id")
            {
                schemaProperty = schemaProperties.SingleOrDefault(_ => _.Name == "Id");
            }

            if (schemaProperty is null)
            {
                value = ConvertUnknownSchemaTypeToClrType(element.Value);
            }
            else
            {
                value = ConvertFromBsonValue(element.Value, schemaProperty);
            }

            if (value is not null)
            {
                expandoObjectAsDictionary[name] = value;
            }
        }

        return expandoObject;
    }

    BsonValue ConvertToBsonValue(object? value, JsonSchema schemaProperty)
    {
        if (schemaProperty.IsDictionary)
        {
            return ConvertUnknownSchemaTypeToBsonValue(value);
        }

        if (value is ExpandoObject expando)
        {
            return ToBsonDocument(
                expando,
                schemaProperty.IsArray ? schemaProperty.Item!.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        if (schemaProperty.Type.HasFlag(JsonObjectType.Array) && value is IEnumerable enumerable)
        {
            var items = new List<BsonValue>();
            foreach (var item in enumerable)
            {
                items.Add(ConvertToBsonValue(item, schemaProperty.Item!.Reference ?? schemaProperty.Item));
            }
            return new BsonArray(items);
        }

        if (typeFormats.IsKnown(schemaProperty.Format!))
        {
            return ConvertToBsonValueBasedOnSchemaType(value, schemaProperty);
        }

        return value.ToBsonValueBasedOnSchemaPropertyType(schemaProperty);
    }

    object? ConvertFromBsonValue(BsonValue bsonValue, JsonSchema schemaProperty)
    {
        schemaProperty = schemaProperty.IsArray ? schemaProperty.Item!.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema;

        if (bsonValue is BsonDocument childDocument)
        {
            if (schemaProperty.IsDictionary)
            {
                return ToDictionary(childDocument);
            }

            return ToExpandoObject(
                childDocument,
                schemaProperty);
        }

        if (bsonValue is BsonArray array)
        {
            return array.Select(_ => ConvertFromBsonValue(_, schemaProperty)).ToArray();
        }

        if (typeFormats.IsKnown(schemaProperty.Format!))
        {
            return bsonValue.ToTargetType(typeFormats.GetTypeForFormat(schemaProperty.Format!));
        }
        return ConvertBsonValueFromUnknownFormat(bsonValue, schemaProperty);
    }

    Dictionary<object, object> ToDictionary(BsonDocument childDocument)
    {
        var dictionary = new Dictionary<object, object>();
        foreach (var element in childDocument.Elements)
        {
            dictionary[element.Name] = ConvertUnknownSchemaTypeToClrType(element.Value)!;
        }
        return dictionary;
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

        var bsonValue = value.ToBsonValue();
        if (bsonValue != BsonNull.Value)
        {
            return bsonValue;
        }

        if (value?.GetType().IsDictionary() == true)
        {
            return value.ToBsonValue();
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

        return BsonNull.Value;
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
            return array.Select(ConvertUnknownSchemaTypeToClrType).ToArray();
        }

        switch (value.BsonType)
        {
            case BsonType.Double:
                return value.AsDouble;
            case BsonType.String:
                if (Guid.TryParse(value.AsString, out var guid))
                {
                    return guid;
                }
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

    object? ConvertBsonValueFromUnknownFormat(BsonValue value, JsonSchema schemaProperty)
    {
        var type = (schemaProperty.Type == JsonObjectType.None && schemaProperty.HasReference) ?
                schemaProperty.Reference!.Type :
                schemaProperty.Type;

        if (type.HasFlag(JsonObjectType.Null))
        {
            type ^= JsonObjectType.Null;
        }

        switch (type)
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

    BsonValue ConvertToBsonValueBasedOnSchemaType(object? input, JsonSchema schemaProperty)
    {
        var targetType = typeFormats.GetTypeForFormat(schemaProperty.Format!);
        return input.ToBsonValue(targetType);
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

    string GetNameForPropertyInBsonDocument(string name) => name.ToMongoDBPropertyName();
}
