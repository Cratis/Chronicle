// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json;

public partial class ExpandoObjectConverter
{
    void WriteObject(Utf8JsonWriter writer, ExpandoObject expandoObject, JsonSchema schema)
    {
        var expandoObjectAsDictionary = (IDictionary<string, object?>)expandoObject;
        var schemaProperties = schema.FlattenedProperties;

        writer.WriteStartObject();

        // A schema without properties preserves everything in the document.
        if (schemaProperties.Count == 0)
        {
            foreach (var (key, value) in expandoObjectAsDictionary)
            {
                WriteProperty(writer, key, UnknownValueConverter.ToJsonNode(value));
            }
        }
        else
        {
            foreach (var property in schemaProperties)
            {
                WriteSchemaProperty(writer, FindSourceValue(expandoObjectAsDictionary, property.Name), property);
            }
        }

        writer.WriteEndObject();
    }

    void WriteSchemaProperty(Utf8JsonWriter writer, object? sourceValue, JsonSchemaProperty property)
    {
        var name = property.Name;

        // Documents and collections of documents are written as they are walked; they are what a large state is made of.
        if (sourceValue is not null && IsStructured(sourceValue, property))
        {
            writer.WritePropertyName(name);
            WriteStructured(writer, sourceValue, property);
        }
        else if (sourceValue is string text && _stringKinds.WritesStringsAsTheyAre(property))
        {
            writer.WriteString(name, text);
        }
        else
        {
            WriteProperty(writer, name, ConvertToJsonNodeOrDefault(sourceValue, property));
        }
    }

    void WriteProperty(Utf8JsonWriter writer, string name, JsonNode? node)
    {
        if (node is null)
        {
            return;
        }

        writer.WritePropertyName(name);
        node.WriteTo(writer, SerializerOptions);
    }

    bool IsStructured(object value, JsonSchema schema) =>
        value is not string &&
        !schema.IsDictionary &&
        (value is ExpandoObject || (schema.Type.HasFlag(JsonObjectType.Array) && value is IEnumerable));

    void WriteStructured(Utf8JsonWriter writer, object value, JsonSchema schema)
    {
        if (value is ExpandoObject expando)
        {
            WriteObject(writer, expando, ConverterSchemas.ObjectSchemaOf(schema));
            return;
        }

        writer.WriteStartArray();
        var itemSchema = schema.Item?.Reference ?? schema.Item;
        foreach (var item in (IEnumerable)value)
        {
            WriteArrayItem(writer, item, itemSchema);
        }

        writer.WriteEndArray();
    }

    void WriteArrayItem(Utf8JsonWriter writer, object? item, JsonSchema? itemSchema)
    {
        if (item is not null && itemSchema is not null && IsStructured(item, itemSchema))
        {
            WriteStructured(writer, item, itemSchema);
        }
        else if (item is string text && itemSchema is not null && _stringKinds.WritesStringsAsTheyAre(itemSchema))
        {
            writer.WriteStringValue(text);
        }
        else
        {
            var node = itemSchema is not null ? ConvertToJsonNode(item, itemSchema) : UnknownValueConverter.ToJsonNode(item);
            if (node is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                node.WriteTo(writer, SerializerOptions);
            }
        }
    }
}
