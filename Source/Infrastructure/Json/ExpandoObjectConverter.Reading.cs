// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json;

public partial class ExpandoObjectConverter
{
    ExpandoObject ReadObject(ref Utf8JsonReader reader, JsonSchema schema)
    {
        var schemaProperties = schema.FlattenedProperties;

        // A schema without properties preserves everything in the document, which needs it as a whole.
        if (schemaProperties.Count == 0)
        {
            return ToExpandoObject(JsonNode.Parse(ref reader)!.AsObject(), schema);
        }

        var read = ReadProperties(ref reader, schema);
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = (IDictionary<string, object?>)expandoObject;
        foreach (var property in schemaProperties)
        {
            read.TryGetValue(property.Name, out var entry);
            var value = entry.Value ?? property.GetDefaultValue(typeFormats);
            if (value is not null)
            {
                expandoObjectAsDictionary[property.Name] = value;
            }
        }

        return expandoObject;
    }

    Dictionary<string, (object? Value, bool IsExactMatch)> ReadProperties(ref Utf8JsonReader reader, JsonSchema schema)
    {
        var exact = schema.FlattenedPropertiesByExactName;
        var ignoringCase = schema.FlattenedPropertiesByName;
        var read = new Dictionary<string, (object? Value, bool IsExactMatch)>(StringComparer.Ordinal);

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var documentName = reader.GetString()!;
            var isExactMatch = exact.TryGetValue(documentName, out var property);
            if (!isExactMatch && !ignoringCase.TryGetValue(documentName, out property))
            {
                reader.Read();
                reader.Skip();
                continue;
            }

            reader.Read();
            var value = ReadValue(ref reader, property!);

            // A document can carry both casings of a name; the exact one wins, as it does when reading a node.
            if (!read.TryGetValue(property!.Name, out var existing) || !existing.IsExactMatch || isExactMatch)
            {
                read[property.Name] = (value, isExactMatch);
            }
        }

        return read;
    }

    object? ReadValue(ref Utf8JsonReader reader, JsonSchema schema)
    {
        var tokenType = reader.TokenType;
        if (tokenType == JsonTokenType.StartObject && !schema.IsDictionary && !typeFormats.IsKnown(ConverterSchemas.FormatOf(schema)))
        {
            return ReadObject(ref reader, ConverterSchemas.ObjectSchemaOf(schema));
        }

        if (tokenType == JsonTokenType.StartArray && schema.Item is not null)
        {
            return ReadArray(ref reader, schema.Item);
        }

        if (tokenType == JsonTokenType.String && _stringKinds.ReadsStringsAsTheyAre(schema))
        {
            return reader.GetString();
        }

        if (tokenType == JsonTokenType.StartObject && schema.IsDictionary)
        {
            return ReadDictionary(ref reader);
        }

        if (tokenType == JsonTokenType.String && TryReadStringScalar(ref reader, schema, out var scalar))
        {
            return scalar;
        }

        return ConvertFromJsonNode(JsonNode.Parse(ref reader), schema);
    }

    object?[] ReadArray(ref Utf8JsonReader reader, JsonSchema itemSchema)
    {
        var items = new List<object?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            items.Add(ReadValue(ref reader, itemSchema));
        }

        return items.ToArray();
    }

    bool TryReadStringScalar(ref Utf8JsonReader reader, JsonSchema schema, out object? scalar)
    {
        // A value that does not parse is left to the general path, which knows what to do with an encrypted one.
        switch (_stringKinds.ScalarKindOf(schema))
        {
            case StringScalar.Guid when reader.TryGetGuid(out var guid):
                scalar = guid;
                return true;
            case StringScalar.DateTime when reader.TryGetDateTime(out var dateTime):
                scalar = DateTimeValues.FromReader(dateTime, ref reader);
                return true;
            default:
                scalar = null;
                return false;
        }
    }

    Dictionary<string, object> ReadDictionary(ref Utf8JsonReader reader)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var key = reader.GetString()!;
            reader.Read();
            dictionary[key] = ReadUnknown(ref reader)!;
        }

        return dictionary;
    }

    object? ReadUnknown(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.String => reader.GetString(),
        JsonTokenType.Number => reader.GetDouble(),
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        JsonTokenType.StartObject => ReadUnknownObject(ref reader),
        JsonTokenType.StartArray => ReadUnknownArray(ref reader),
        _ => null
    };

    ExpandoObject ReadUnknownObject(ref Utf8JsonReader reader)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = (IDictionary<string, object?>)expandoObject;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var property = reader.GetString()!;
            reader.Read();
            expandoObjectAsDictionary[property] = ReadUnknown(ref reader);
        }

        return expandoObject;
    }

    object?[] ReadUnknownArray(ref Utf8JsonReader reader)
    {
        var items = new List<object?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            items.Add(ReadUnknown(ref reader));
        }

        return items.ToArray();
    }
}
