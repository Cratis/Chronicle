// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.DependencyInjection;
using Cratis.Json;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Represents an implementation of <see cref="IExpandoObjectConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
[Singleton]
public class ExpandoObjectConverter(ITypeFormats typeFormats) : IExpandoObjectConverter
{
    static readonly object _true = true;
    static readonly object _false = false;
    readonly JsonSerializerOptions? _jsonSerializerOptions;
    readonly ConditionalWeakTable<JsonSchema, object> _writesStringsAsTheyAre = new();
    readonly ConditionalWeakTable<JsonSchema, object> _readsStringsAsTheyAre = new();
    readonly ConditionalWeakTable<JsonSchema, object> _scalarKinds = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpandoObjectConverter"/> class with provider-specific serializer options.
    /// </summary>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for mapping type formats in a schema.</param>
    /// <param name="jsonSerializerOptions">The options to use for formatted values.</param>
    public ExpandoObjectConverter(ITypeFormats typeFormats, JsonSerializerOptions jsonSerializerOptions)
        : this(typeFormats)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    JsonSerializerOptions SerializerOptions => _jsonSerializerOptions ?? Globals.JsonSerializerOptions;

    /// <inheritdoc/>
    public JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema)
    {
        var jsonObject = new JsonObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object?>;
        var schemaProperties = schema.FlattenedProperties;

        // When schema has no properties (e.g. a placeholder empty schema), fall back to
        // unknown-type conversion so that all data in the expando object is preserved.
        if (schemaProperties.Count == 0)
        {
            foreach (var (key, value) in expandoObjectAsDictionary)
            {
                var node = ConvertUnknownSchemaTypeToJsonValue(value);
                if (node is not null)
                {
                    jsonObject[key] = node;
                }
            }

            return jsonObject;
        }

        foreach (var property in schemaProperties)
        {
            var name = property.Name;

            // Prefer an exact-case match; fall back to a case-insensitive match. The source expando
            // may carry both casings of the same name (e.g. both `Id` and `id`).
            if (!expandoObjectAsDictionary.TryGetValue(name, out var sourceValue))
            {
                sourceValue = null;
                foreach (var candidate in expandoObjectAsDictionary)
                {
                    if (candidate.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        sourceValue = candidate.Value;
                        break;
                    }
                }
            }

            var value = ConvertToJsonNode(sourceValue, property);

            if (value is null)
            {
                var defaultValue = property.GetDefaultValue(typeFormats);
                if (defaultValue is not null)
                {
                    value = ConvertToJsonNode(defaultValue, property);
                }
            }

            if (value is not null)
            {
                jsonObject[name] = value;
            }
        }

        return jsonObject;
    }

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(JsonObject document, JsonSchema schema)
    {
        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = expandoObject as IDictionary<string, object?>;

        var schemaProperties = schema.FlattenedProperties;

        // When schema has no properties (e.g. a placeholder empty schema), fall back to
        // unknown-type conversion so that all data in the document is preserved.
        if (schemaProperties.Count == 0)
        {
            foreach (var (name, sourceValue) in document)
            {
                if (sourceValue is not null)
                {
                    expandoObjectAsDictionary[name] = ConvertUnknownSchemaTypeToClrType(sourceValue);
                }
            }

            return expandoObject;
        }

        foreach (var property in schemaProperties)
        {
            var name = property.Name;
            var sourceValue = document[name];
            if (sourceValue is null)
            {
                foreach (var candidate in document)
                {
                    if (candidate.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        sourceValue = candidate.Value;
                        break;
                    }
                }
            }

            object? value = null;
            if (sourceValue is not null)
            {
                value = ConvertFromJsonNode(sourceValue, property);
            }

            value ??= property.GetDefaultValue(typeFormats);
            if (value is not null)
            {
                expandoObjectAsDictionary[name] = value;
            }
        }

        return expandoObject;
    }

    /// <inheritdoc/>
    public void WriteJson(Utf8JsonWriter writer, ExpandoObject expandoObject, JsonSchema schema) => WriteObject(writer, expandoObject, schema);

    /// <inheritdoc/>
    public ExpandoObject ToExpandoObject(ReadOnlyMemory<byte> utf8Json, JsonSchema schema)
    {
        var reader = new Utf8JsonReader(utf8Json.Span);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("The JSON of a read model state must be an object.");
        }

        return ReadObject(ref reader, schema);
    }

    /// <summary>
    /// Gets whether the schema marks its value as protected. Asked for every string value converted, so the
    /// memoized recursive answer is consulted first: when it is no, there is nothing at this level either.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>True when the value is protected.</returns>
    static bool HasProtectedValueMetadata(JsonSchema schema) =>
        (schema.HasComplianceMetadata() && schema.GetComplianceMetadata().Any()) ||
        (schema.HasSecurityMetadata() && schema.GetSecurityMetadata().Any()) ||
        (schema.Reference is { } reference &&
            ((reference.HasComplianceMetadata() && reference.GetComplianceMetadata().Any()) ||
             (reference.HasSecurityMetadata() && reference.GetSecurityMetadata().Any())));

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
                WriteProperty(writer, key, ConvertUnknownSchemaTypeToJsonValue(value));
            }

            writer.WriteEndObject();
            return;
        }

        foreach (var property in schemaProperties)
        {
            var name = property.Name;

            // Prefer an exact-case match; fall back to a case-insensitive match, as ToJsonObject does.
            if (!expandoObjectAsDictionary.TryGetValue(name, out var sourceValue))
            {
                sourceValue = null;
                foreach (var candidate in expandoObjectAsDictionary)
                {
                    if (candidate.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        sourceValue = candidate.Value;
                        break;
                    }
                }
            }

            // Documents and collections of documents are written as they are walked; they are what a large state is made of.
            if (sourceValue is not null && IsStructured(sourceValue, property))
            {
                writer.WritePropertyName(name);
                WriteStructured(writer, sourceValue, property);
                continue;
            }

            if (sourceValue is string text && WritesStringsAsTheyAre(property))
            {
                writer.WriteString(name, text);
                continue;
            }

            var node = ConvertToJsonNode(sourceValue, property);
            if (node is null)
            {
                var defaultValue = property.GetDefaultValue(typeFormats);
                if (defaultValue is not null)
                {
                    node = ConvertToJsonNode(defaultValue, property);
                }
            }

            WriteProperty(writer, name, node);
        }

        writer.WriteEndObject();
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
            WriteObject(
                writer,
                expando,
                schema.IsArray ? schema.Item!.Reference ?? schema.Item : schema.ActualTypeSchema);
            return;
        }

        writer.WriteStartArray();
        var itemSchema = schema.Item?.Reference ?? schema.Item;
        foreach (var item in (IEnumerable)value)
        {
            if (item is not null && itemSchema is not null && IsStructured(item, itemSchema))
            {
                WriteStructured(writer, item, itemSchema);
                continue;
            }

            if (item is string text && itemSchema is not null && WritesStringsAsTheyAre(itemSchema))
            {
                writer.WriteStringValue(text);
                continue;
            }

            var node = itemSchema is not null ? ConvertToJsonNode(item, itemSchema) : ConvertUnknownSchemaTypeToJsonValue(item);
            if (node is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                node.WriteTo(writer, SerializerOptions);
            }
        }

        writer.WriteEndArray();
    }

    ExpandoObject ReadObject(ref Utf8JsonReader reader, JsonSchema schema)
    {
        var schemaProperties = schema.FlattenedProperties;

        // A schema without properties preserves everything in the document, which needs it as a whole.
        if (schemaProperties.Count == 0)
        {
            return ToExpandoObject(JsonNode.Parse(ref reader)!.AsObject(), schema);
        }

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

        var expandoObject = new ExpandoObject();
        var expandoObjectAsDictionary = (IDictionary<string, object?>)expandoObject;
        foreach (var property in schemaProperties)
        {
            var name = property.Name;
            read.TryGetValue(name, out var entry);
            var value = entry.Value ?? property.GetDefaultValue(typeFormats);
            if (value is not null)
            {
                expandoObjectAsDictionary[name] = value;
            }
        }

        return expandoObject;
    }

    object? ReadValue(ref Utf8JsonReader reader, JsonSchema schema)
    {
        if (reader.TokenType == JsonTokenType.StartObject &&
            !schema.IsDictionary &&
            !typeFormats.IsKnown(schema.Format ?? schema.ActualTypeSchema.Format!))
        {
            return ReadObject(ref reader, schema.IsArray ? schema.Item!.Reference ?? schema.Item : schema.ActualTypeSchema);
        }

        if (reader.TokenType == JsonTokenType.StartArray && schema.Item is not null)
        {
            var items = new List<object?>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                items.Add(ReadValue(ref reader, schema.Item));
            }

            return items.ToArray();
        }

        if (reader.TokenType == JsonTokenType.String && ReadsStringsAsTheyAre(schema))
        {
            return reader.GetString();
        }

        if (reader.TokenType == JsonTokenType.StartObject && schema.IsDictionary)
        {
            return ReadDictionary(ref reader);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // A value that does not parse is left to the general path, which knows what to do with an encrypted one.
            switch (ScalarKindOf(schema))
            {
                case StringScalar.Guid when reader.TryGetGuid(out var guid):
                    return guid;
                case StringScalar.DateTime when reader.TryGetDateTime(out var dateTime):
                    return dateTime;
            }
        }

        return ConvertFromJsonNode(JsonNode.Parse(ref reader), schema);
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

    object? ReadUnknown(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                return reader.GetDouble();
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.StartObject:
                var expandoObject = new ExpandoObject();
                var expandoObjectAsDictionary = (IDictionary<string, object?>)expandoObject;
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    var property = reader.GetString()!;
                    reader.Read();
                    expandoObjectAsDictionary[property] = ReadUnknown(ref reader);
                }

                return expandoObject;
            case JsonTokenType.StartArray:
                var items = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    items.Add(ReadUnknown(ref reader));
                }

                return items.ToArray();
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets whether a string value is written to JSON exactly as it is when converted against a schema, which holds
    /// for most strings of a state and spares them a node of their own.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>True when the string is written as it is.</returns>
    bool WritesStringsAsTheyAre(JsonSchema schema)
    {
        if (_writesStringsAsTheyAre.TryGetValue(schema, out var known))
        {
            return ReferenceEquals(known, _true);
        }

        var answer = ComputeWritesStringsAsTheyAre(schema);
        _writesStringsAsTheyAre.TryAdd(schema, answer ? _true : _false);
        return answer;
    }

    bool ComputeWritesStringsAsTheyAre(JsonSchema schema)
    {
        // These are the branches of ConvertToJsonNode that keep a string as it is, in the order it tries them.
        if (HasProtectedValueMetadata(schema) || schema.IsDictionary || schema.Type.HasFlag(JsonObjectType.Array))
        {
            return true;
        }

        if (typeFormats.IsKnown(schema.Format ?? schema.ActualTypeSchema.Format!))
        {
            return false;
        }

        var type = EffectiveType(schema);
        return type is JsonObjectType.String or JsonObjectType.None;
    }

    /// <summary>
    /// Gets whether a string token is read as the string it is when converted against a schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>True when the string is read as it is.</returns>
    bool ReadsStringsAsTheyAre(JsonSchema schema)
    {
        if (_readsStringsAsTheyAre.TryGetValue(schema, out var known))
        {
            return ReferenceEquals(known, _true);
        }

        var answer = ComputeReadsStringsAsTheyAre(schema);
        _readsStringsAsTheyAre.TryAdd(schema, answer ? _true : _false);
        return answer;
    }

    /// <summary>
    /// Gets which scalar a string token of a schema is read directly into, because it is formatted as one.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The <see cref="StringScalar"/>.</returns>
    StringScalar ScalarKindOf(JsonSchema schema)
    {
        if (_scalarKinds.TryGetValue(schema, out var known))
        {
            return (StringScalar)known;
        }

        var kind = StringScalar.None;
        var format = schema.Format ?? schema.ActualTypeSchema.Format!;
        if (!schema.IsDictionary && typeFormats.IsKnown(format))
        {
            var type = typeFormats.GetTypeForFormat(format);
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(Guid))
            {
                kind = StringScalar.Guid;
            }
            else if (type == typeof(DateTime))
            {
                kind = StringScalar.DateTime;
            }
        }

        _scalarKinds.TryAdd(schema, kind);
        return kind;
    }

    bool ComputeReadsStringsAsTheyAre(JsonSchema schema) =>
        !schema.IsDictionary &&
        !typeFormats.IsKnown(schema.Format ?? schema.ActualTypeSchema.Format!) &&
        EffectiveType(schema) == JsonObjectType.String &&
        schema.Format != "guid" &&
        schema.OneOf.Count == 0;

    JsonObjectType EffectiveType(JsonSchema schema)
    {
        // A nullable value is a union with null; the type of the value is that of the other branch.
        if (schema.OneOf.Count > 0)
        {
            var oneOfSchema = schema.OneOf.FirstOrDefault(_ => _.Type != JsonObjectType.Null);
            if (oneOfSchema != default)
            {
                schema = oneOfSchema;
            }
        }

        var type = (schema.Type == JsonObjectType.None && schema.HasReference) ? schema.Reference!.Type : schema.Type;
        return type.HasFlag(JsonObjectType.Null) ? type ^ JsonObjectType.Null : type;
    }

    JsonNode? ConvertToJsonNode(object? value, JsonSchema schemaProperty)
    {
        // Compliance handlers replace protected scalar values with opaque strings while the registered schema
        // intentionally remains the schema of the plaintext event. Keep those strings opaque until the
        // compliance manager releases them instead of coercing ciphertext through the plaintext scalar type.
        if (value is string compliantValue && HasProtectedValueMetadata(schemaProperty))
        {
            return JsonValue.Create(compliantValue);
        }

        if (schemaProperty.IsDictionary)
        {
            return ConvertUnknownSchemaTypeToJsonValue(value);
        }

        if (value is ExpandoObject expando)
        {
            return ToJsonObject(
                expando,
                schemaProperty.IsArray ? schemaProperty.Item!.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        // A coarse [PII] value on a whole list/array is blob-encrypted to a single ciphertext string,
        // even though the schema type stays Array. Serialize it as that scalar string rather than letting
        // the array branch below treat the string as an enumerable of characters — which would shred the
        // ciphertext into per-character elements and break the encrypt -> store -> read -> release round trip.
        if (value is string ciphertext && schemaProperty.Type.HasFlag(JsonObjectType.Array))
        {
            return JsonValue.Create(ciphertext);
        }

        if (schemaProperty.Type.HasFlag(JsonObjectType.Array) && value is IEnumerable enumerable)
        {
            var items = new List<JsonNode?>();
            var itemSchema = schemaProperty.Item?.Reference ?? schemaProperty.Item;
            foreach (var item in enumerable)
            {
                items.Add(itemSchema is not null
                    ? ConvertToJsonNode(item, itemSchema)
                    : ConvertUnknownSchemaTypeToJsonValue(item));
            }
            return new JsonArray([.. items]);
        }

        if (typeFormats.IsKnown(schemaProperty.Format ?? schemaProperty.ActualTypeSchema.Format!))
        {
            // A complex formatted value (e.g. a geospatial type) serializes to a JSON object/array
            // through its registered converter (GeoJSON); a scalar formatted value (guid, date, ...)
            // serializes to a JSON value and goes through the scalar conversion path below.
            var node = JsonSerializer.SerializeToNode(value, SerializerOptions);
            if (node is JsonObject or JsonArray)
            {
                return node;
            }

            return ConvertToJsonValueBasedOnSchemaType(value, schemaProperty);
        }

        // Fall back to schema-agnostic conversion for collection types that didn't match
        // the schema-typed array branch above (e.g. when NJsonSchema doesn't set Type=Array).
        if (value is IEnumerable and not string)
        {
            return ConvertUnknownSchemaTypeToJsonValue(value);
        }

        return ConvertToJsonNodeFromUnknownFormat(value, schemaProperty);
    }

    object? ConvertFromJsonNode(JsonNode? jsonNode, JsonSchema schemaProperty)
    {
        if (jsonNode is null)
        {
            return null;
        }

        if (jsonNode is JsonObject childObject)
        {
            if (schemaProperty.IsDictionary)
            {
                return ToDictionary(childObject);
            }

            // A JSON object carrying a known format identifies a complex CLR type (e.g. a geospatial
            // type serialized as GeoJSON). Deserialize the whole object into that type so the
            // ExpandoObject holds the typed value rather than a generic nested structure.
            if (typeFormats.IsKnown(schemaProperty.Format ?? schemaProperty.ActualTypeSchema.Format!))
            {
                return jsonNode.Deserialize(
                    typeFormats.GetTypeForFormat(schemaProperty.Format ?? schemaProperty.ActualTypeSchema.Format!),
                    SerializerOptions);
            }

            return ToExpandoObject(
                childObject,
                schemaProperty.IsArray ? schemaProperty.Item!.Reference ?? schemaProperty.Item : schemaProperty.ActualTypeSchema);
        }

        if (jsonNode is JsonArray array)
        {
            // When the schema has no item definition (e.g. an any/empty schema generated for
            // types with custom converters), fall back to unknown-type conversion for each element.
            if (schemaProperty.Item is null)
            {
                return array.Select(ConvertUnknownSchemaTypeToClrType).ToArray();
            }
            var items = new object?[array.Count];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = ConvertFromJsonNode(array[i], schemaProperty.Item!);
            }

            return items;
        }

        if (typeFormats.IsKnown(schemaProperty.Format ?? schemaProperty.ActualTypeSchema.Format!))
        {
            try
            {
                return ConvertJsonValueToSchemaType(jsonNode, schemaProperty);
            }
            catch (Exception ex) when (
                HasProtectedValueMetadata(schemaProperty) &&
                jsonNode.AsValue().TryGetValue<string>(out _) &&
                ex is FormatException or InvalidCastException or InvalidOperationException or OverflowException)
            {
                // An encrypted value (or the empty marker returned after crypto-shredding) deliberately cannot
                // be parsed as the plaintext formatted type. Keep it opaque for the compliance manager.
                return jsonNode.GetValue<string>();
            }
        }
        return ConvertJsonValueFromUnknownFormat(jsonNode, schemaProperty);
    }

    Dictionary<string, object> ToDictionary(JsonObject childObject)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in childObject)
        {
            dictionary[key] = ConvertUnknownSchemaTypeToClrType(value)!;
        }

        return dictionary;
    }

    object? ConvertJsonValueFromUnknownFormat(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        if (jsonNode is null)
        {
            return null;
        }

        var value = jsonNode.AsValue();

        var isCompliant = HasProtectedValueMetadata(schemaProperty);

        // Nullable enum values are represented as a discriminated union with a null value. We need to get the actual property definition.
        // Other types could also be represented in this manner and it is therefor important to get the actual property definition.
        if (schemaProperty.OneOf.Count > 0)
        {
            var oneOfSchema = schemaProperty.OneOf.FirstOrDefault(_ => _.Type != JsonObjectType.Null);
            if (oneOfSchema != default)
            {
                schemaProperty = oneOfSchema;
            }
        }
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
                // A node created from a Guid holds the Guid itself; one parsed from JSON text holds a string.
                if (!value.TryGetValue<string>(out var valueAsString))
                {
                    if (value.TryGetValue<Guid>(out var guidValue))
                    {
                        return guidValue;
                    }

                    valueAsString = value.GetValue<string>();
                }

                return schemaProperty.Format == "guid" ?
                        Guid.Parse(valueAsString) :
                        valueAsString;

            case JsonObjectType.Boolean:
                if (isCompliant && value.TryGetValue<string>(out var booleanAsString))
                {
                    if (bool.TryParse(booleanAsString, out var booleanValue))
                    {
                        return booleanValue;
                    }
                    if (booleanAsString is not null)
                    {
                        return booleanAsString;
                    }
                }
                return value.GetValue<bool>();

            case JsonObjectType.Integer:
                JsonSchema? enumReadSchema = null;
                if (schemaProperty.Reference?.IsEnumeration == true)
                    enumReadSchema = schemaProperty.Reference;
                else if (schemaProperty.IsEnumeration)
                    enumReadSchema = schemaProperty;
                if (enumReadSchema is not null && value.TryGetValue<string>(out var enumNameValue))
                {
                    var index = enumReadSchema.EnumerationNames.IndexOf(enumNameValue);
                    if (index >= 0 && index < enumReadSchema.Enumeration.Count)
                        return TypeConversion.Convert(typeof(int), enumReadSchema.Enumeration.ToArray()[index]!);

                    if (isCompliant)
                    {
                        if (int.TryParse(enumNameValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var enumValue))
                            return enumValue;

                        return enumNameValue;
                    }
                }
                if (isCompliant && value.TryGetValue<string>(out var integerAsString))
                {
                    if (int.TryParse(integerAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
                    {
                        return integerValue;
                    }
                    if (integerAsString is not null)
                    {
                        return integerAsString;
                    }
                }
                return value.GetValue<int>();

            case JsonObjectType.Number:
                if (isCompliant && value.TryGetValue<string>(out var numberAsString))
                {
                    if (double.TryParse(numberAsString, NumberStyles.Float, CultureInfo.InvariantCulture, out var numberValue))
                    {
                        return numberValue;
                    }
                    if (numberAsString is not null)
                    {
                        return numberAsString;
                    }
                }
                if (value.TryGetValue<decimal>(out var decimalValue) && value.GetValue<object>() is decimal)
                {
                    return decimalValue;
                }
                return value.GetValue<double>();
        }

        // No type information in the schema (e.g. an any/empty schema generated for types
        // with custom converters). Fall back to extracting the raw CLR value from the node.
        return ConvertUnknownSchemaTypeToClrType(jsonNode);
    }

    JsonNode? ConvertUnknownSchemaTypeToJsonValue(object? value)
    {
        if (value is ExpandoObject expandoObject)
        {
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            var document = new JsonObject();

            foreach (var kvp in expandoObjectAsDictionary)
            {
                document[kvp.Key] = ConvertUnknownSchemaTypeToJsonValue(kvp.Value);
            }
            return document;
        }

        if (value is string text)
        {
            return JsonValue.Create(text);
        }

        if (value is IDictionary dictionaryValue)
        {
            var dictionaryDocument = new JsonObject();
            var entries = dictionaryValue.GetEnumerator();
            while (entries.MoveNext())
            {
                dictionaryDocument[entries.Key.ToString() ?? string.Empty] = ConvertUnknownSchemaTypeToJsonValue(entries.Value);
            }

            return dictionaryDocument;
        }

        if (value?.GetType() is { IsValueType: false } valueType0 && valueType0.IsDictionary())
        {
            var dictionaryType = value.GetType();
            var keyType = dictionaryType.GetKeyType();
            var valueType = dictionaryType.GetValueType();
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var keyProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Key))!;
            var valueProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Value))!;

            var dictionary = value as IEnumerable;
            var document = new JsonObject();
            foreach (var keyValuePair in dictionary!)
            {
                var key = keyProperty.GetValue(keyValuePair)?.ToString() ?? string.Empty;
                document[key] = ConvertUnknownSchemaTypeToJsonValue(valueProperty.GetValue(keyValuePair));
            }
            return document;
        }

        if (value?.GetType().IsEnum == true)
        {
            return JsonValue.Create(Convert.ToInt32(value));
        }

        var jsonValue = value.ToJsonValue();
        if (jsonValue is not null)
        {
            return jsonValue;
        }

        if (value is IEnumerable enumerable)
        {
            var array = new JsonArray();

            foreach (var item in enumerable)
            {
                array.Add(ConvertUnknownSchemaTypeToJsonValue(item));
            }

            return array;
        }

        return null;
    }

    object? ConvertUnknownSchemaTypeToClrType(JsonNode? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonObject jsonObject)
        {
            var expandoObject = new ExpandoObject();
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            foreach (var (property, sourceValue) in jsonObject)
            {
                expandoObjectAsDictionary[property] = ConvertUnknownSchemaTypeToClrType(sourceValue)!;
            }
            return expandoObject;
        }

        if (value is JsonArray array)
        {
            return array.Select(ConvertUnknownSchemaTypeToClrType).ToArray();
        }

        var jsonValue = value.GetValue<object>();
        if (jsonValue is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => null
            };
        }

        return jsonValue;
    }

    object? ConvertJsonValueToSchemaType(JsonNode jsonNode, JsonSchema schemaProperty)
    {
        var targetType = typeFormats.GetTypeForFormat(schemaProperty.Format ?? schemaProperty.ActualTypeSchema.Format!);
        if (Nullable.GetUnderlyingType(targetType) == typeof(decimal) || targetType == typeof(decimal))
        {
            if (DecimalValues.TryFromLiteral(jsonNode.AsValue(), out var decimalValue))
            {
                return decimalValue;
            }
        }
        return jsonNode.AsValue().ToTargetTypeValue(targetType);
    }

    JsonValue? ConvertToJsonValueBasedOnSchemaType(object? input, JsonSchema schemaProperty)
    {
        if (input is null)
        {
            return null;
        }

        var targetType = typeFormats.GetTypeForFormat(schemaProperty.Format ?? schemaProperty.ActualTypeSchema.Format!);
        input = (Nullable.GetUnderlyingType(targetType) ?? targetType) == typeof(decimal) ? input switch
        {
            double value => DecimalValues.FromDouble(value),
            float value => DecimalValues.FromSingle(value),
            _ => TypeConversion.Convert(targetType, input)
        } : TypeConversion.Convert(targetType, input);
        return input.ToJsonValue();
    }

    JsonNode? ConvertToJsonNodeFromUnknownFormat(object? value, JsonSchema schemaProperty)
    {
        if (value is null)
        {
            return null;
        }

        // Nullable enum values are represented as a discriminated union with a null value. We need to get the actual property definition.
        // Other types could also be represented in this manner and it is therefor important to get the actual property definition.
        if (schemaProperty.OneOf.Count > 0)
        {
            var oneOfSchema = schemaProperty.OneOf.FirstOrDefault(_ => _.Type != JsonObjectType.Null);
            if (oneOfSchema != default)
            {
                schemaProperty = oneOfSchema;
            }
        }
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
                return JsonValue.Create<string>(value is string actualString ? actualString : value.ToString()!);

            case JsonObjectType.Boolean:
                return JsonValue.Create<bool>(value is bool actualBool ? actualBool : bool.Parse(value.ToString()!));

            case JsonObjectType.Integer:
                JsonSchema? enumRef = null;
                if (schemaProperty.Reference?.IsEnumeration == true)
                    enumRef = schemaProperty.Reference;
                else if (schemaProperty.IsEnumeration)
                    enumRef = schemaProperty;
                if (enumRef is not null)
                {
                    var enumValues = enumRef.Enumeration.ToList();
                    var longValue = Convert.ToInt64(value);
                    var enumIndex = enumValues.FindIndex(v => Convert.ToInt64(v) == longValue);
                    if (enumIndex >= 0 && enumIndex < enumRef.EnumerationNames.Count)
                        return JsonValue.Create<string>(enumRef.EnumerationNames[enumIndex]);
                    return null;
                }
                return JsonValue.Create<int>(value is int actualInt ? actualInt : int.Parse(value.ToString()!));

            case JsonObjectType.Number:
                if (value is decimal decimalValue)
                {
                    return JsonValue.Create(decimalValue);
                }
                return JsonValue.Create<double>(value is double actualDouble ? actualDouble : double.Parse(value.ToString()!));

            case JsonObjectType.None:
                // A default-only property ({"default": null}) describes no type, so keep the value as it is.
                return ConvertUnknownSchemaTypeToJsonValue(value);
        }

        // The schema declares a type the value does not have - for instance a composite key that a
        // storage provider hands back flattened to a string for an object-typed property. Emitting the
        // mismatched value would make the instance unreadable for the client, so leave it out.
        return null;
    }
}
