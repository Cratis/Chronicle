// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents a JSON Schema document, backed by a <see cref="JsonObject"/>.
/// </summary>
public class JsonSchema
{
    static readonly string[] _knownSchemaKeys =
    [
        "type", "properties", "allOf", "anyOf", "oneOf", "$ref", "format",
        "items", "$defs", "definitions", "additionalProperties", "title",
        "description", "required", "enum", "minimum", "maximum",
        "minLength", "maxLength", "pattern", "default"
    ];
    static readonly TypeFormats _typeFormats = new();
    readonly JsonSchema? _root;

    /// <summary>Lazy caches for parsed schema components.</summary>
    SyncedPropertiesDictionary? _propertiesCache;
    List<JsonSchema>? _allOfCache;
    List<JsonSchema>? _anyOfCache;
    List<JsonSchema>? _oneOfCache;
    Dictionary<string, JsonSchema>? _definitionsCache;
    ExtensionDataDictionary? _extensionDataCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchema"/> class (empty schema).
    /// </summary>
    public JsonSchema()
    {
        Node = new JsonObject();
        _root = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchema"/> class from a JSON object.
    /// </summary>
    /// <param name="node">The JSON object representing the schema.</param>
    /// <param name="root">Optional root schema for $ref resolution.</param>
    public JsonSchema(JsonObject node, JsonSchema? root = null)
    {
        Node = node;
        _root = root;
    }

    /// <summary>
    /// Gets or sets the JSON object type.
    /// </summary>
    public JsonObjectType Type
    {
        get => ParseTypeFromNode(Node);
        set => SetTypeOnNode(Node, value);
    }

    /// <summary>
    /// Gets or sets the format string.
    /// </summary>
    public string? Format
    {
        get => Node["format"]?.GetValue<string>();
        set
        {
            if (value is null)
            {
                Node.Remove("format");
            }
            else
            {
                Node["format"] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the title of the schema.
    /// </summary>
    public string? Title
    {
        get => Node["title"]?.GetValue<string>();
        set
        {
            if (value is null)
            {
                Node.Remove("title");
            }
            else
            {
                Node["title"] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the description of the schema.
    /// </summary>
    public string? Description
    {
        get => Node["description"]?.GetValue<string>();
        set
        {
            if (value is null)
            {
                Node.Remove("description");
            }
            else
            {
                Node["description"] = value;
            }
        }
    }

    /// <summary>
    /// Gets the mutable properties dictionary for this schema. Changes are synchronized to the underlying JSON node.
    /// </summary>
    public IDictionary<string, JsonSchemaProperty> Properties
    {
        get
        {
            if (_propertiesCache is null)
            {
                _propertiesCache = new SyncedPropertiesDictionary(Node);
                if (Node["properties"] is JsonObject propsNode)
                {
                    foreach (var (key, value) in propsNode)
                    {
                        if (value is JsonObject propObj)
                        {
                            var propNode = (JsonObject)propObj.DeepClone();
                            var prop = new JsonSchemaProperty(key, propNode, Root);
                            _propertiesCache.LoadWithoutSync(key, prop);
                        }
                    }
                }
            }
            return _propertiesCache;
        }
    }

    /// <summary>
    /// Gets the actual (effective) properties including resolved references.
    /// </summary>
    public IReadOnlyDictionary<string, JsonSchemaProperty> ActualProperties =>
        new Dictionary<string, JsonSchemaProperty>(Properties, StringComparer.Ordinal);

    /// <summary>
    /// Gets the inherited schema (first resolved $ref in allOf, if any).
    /// </summary>
    public JsonSchema? InheritedSchema =>
        AllOf.FirstOrDefault(s => s.HasReference)?.Reference;

    /// <summary>
    /// Gets the AllOf schemas.
    /// </summary>
    public IList<JsonSchema> AllOf => _allOfCache ??= BuildSchemaList("allOf");

    /// <summary>
    /// Gets the AnyOf schemas.
    /// </summary>
    public IList<JsonSchema> AnyOf => _anyOfCache ??= BuildSchemaList("anyOf");

    /// <summary>
    /// Gets the OneOf schemas.
    /// </summary>
    public IList<JsonSchema> OneOf => _oneOfCache ??= BuildSchemaList("oneOf");

    /// <summary>
    /// Gets whether this schema is a $ref.
    /// </summary>
    public bool HasReference => Node["$ref"] is not null;

    /// <summary>
    /// Gets the resolved $ref schema.
    /// </summary>
    public JsonSchema? Reference => HasReference ? ResolveRef() : null;

    /// <summary>
    /// Gets or sets the item schema (for arrays).
    /// </summary>
    public JsonSchema? Item
    {
        get
        {
            if (Node["items"] is JsonObject itemNode)
            {
                return new JsonSchemaProperty(string.Empty, (JsonObject)itemNode.DeepClone(), Root);
            }
            return null;
        }
        set
        {
            if (value is null)
            {
                Node.Remove("items");
            }
            else
            {
                Node["items"] = value.Node.DeepClone();
            }
        }
    }

    /// <summary>
    /// Gets the definitions ($defs or definitions).
    /// </summary>
    public IDictionary<string, JsonSchema> Definitions
    {
        get
        {
            if (_definitionsCache is null)
            {
                _definitionsCache = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
                var defsNode = (Node["$defs"] ?? Node["definitions"]) as JsonObject;
                if (defsNode is not null)
                {
                    foreach (var (key, value) in defsNode)
                    {
                        if (value is JsonObject defObj)
                        {
                            _definitionsCache[key] = new JsonSchema((JsonObject)defObj.DeepClone(), this);
                        }
                    }
                }
            }
            return _definitionsCache;
        }
    }

    /// <summary>
    /// Gets the additional properties schema.
    /// </summary>
    public JsonSchema? AdditionalPropertiesSchema
    {
        get
        {
            if (Node["additionalProperties"] is JsonObject addlObj)
            {
                return new JsonSchema((JsonObject)addlObj.DeepClone(), Root);
            }
            return null;
        }
    }

    /// <summary>
    /// Gets whether this schema represents an array type.
    /// </summary>
    public bool IsArray => Type.HasFlag(JsonObjectType.Array);

    /// <summary>
    /// Gets whether this schema represents a dictionary (additionalProperties present, no explicit properties).
    /// </summary>
    public bool IsDictionary =>
        Node["additionalProperties"] is not null &&
        (Node["properties"] is null ||
            (Node["properties"] is JsonObject propsObj && propsObj.Count == 0));

    /// <summary>
    /// Gets whether this schema defines an enumeration.
    /// </summary>
    public bool IsEnumeration => Node["enum"] is not null;

    /// <summary>
    /// Gets the enumeration names (string values from "enum").
    /// </summary>
    public IList<string> EnumerationNames
    {
        get
        {
            if (Node["enum"] is JsonArray arr)
            {
                return [.. arr.Select(v => v?.GetValue<string>() ?? string.Empty)];
            }
            return [];
        }
    }

    /// <summary>
    /// Gets the enumeration values (integer indices or string values from "enum").
    /// </summary>
    public IList<object> Enumeration
    {
        get
        {
            if (Node["enum"] is JsonArray arr)
            {
                return [.. arr.Select<JsonNode?, object>(v =>
                {
                    if (v is null) return 0;
                    if (v is JsonValue val)
                    {
                        if (val.TryGetValue<int>(out var i)) return i;
                        if (val.TryGetValue<string>(out var s)) return s;
                    }
                    return 0;
                })];
            }
            return [];
        }
    }

    /// <summary>
    /// Gets the actual resolved type schema for this schema, following references and allOf structures.
    /// </summary>
    public JsonSchema ActualTypeSchema
    {
        get
        {
            if (HasReference)
            {
                return Reference ?? this;
            }

            var allOf = AllOf;
            if (allOf.Count > 0)
            {
                return allOf.FirstOrDefault(s => !s.HasReference && s.Node["properties"] is not null)
                    ?? (allOf[0].HasReference ? allOf[0].Reference ?? this : allOf[0]);
            }

            var anyOf = AnyOf;
            if (anyOf.Count > 0)
            {
                var nonNull = anyOf.FirstOrDefault(s =>
                    s.Type != JsonObjectType.Null &&
                    !(s.HasReference && s.Reference?.Type == JsonObjectType.Null));
                return nonNull ?? this;
            }

            return this;
        }
    }

    /// <summary>
    /// Gets the actual resolved schema (alias for <see cref="ActualTypeSchema"/>).
    /// </summary>
    public JsonSchema ActualSchema => ActualTypeSchema;

    /// <summary>
    /// Gets or sets the extension data (custom JSON keys beyond the schema vocabulary).
    /// </summary>
    public IDictionary<string, object?>? ExtensionData
    {
        get => _extensionDataCache ??= new ExtensionDataDictionary(Node);
        set
        {
            if (value is null) return;
            _extensionDataCache = value as ExtensionDataDictionary;
            if (_extensionDataCache is null)
            {
                _extensionDataCache = new ExtensionDataDictionary(Node);
                foreach (var (key, val) in value)
                {
                    _extensionDataCache[key] = val;
                }
            }
        }
    }

    /// <summary>
    /// Gets the root schema for $ref resolution.
    /// </summary>
    internal JsonSchema Root => _root ?? this;

    /// <summary>
    /// Gets the internal JSON node.
    /// </summary>
    internal JsonObject Node { get; }

    /// <summary>
    /// Parses a JSON Schema from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The parsed <see cref="JsonSchema"/>.</returns>
    public static JsonSchema FromJson(string json)
    {
        var node = JsonNode.Parse(json)!.AsObject();
        return new JsonSchema(node);
    }

    /// <summary>
    /// Parses a JSON Schema from a JSON string (async-compatible, runs synchronously).
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>A completed task containing the parsed <see cref="JsonSchema"/>.</returns>
    public static Task<JsonSchema> FromJsonAsync(string json)
    {
        var node = JsonNode.Parse(json)!.AsObject();
        return Task.FromResult(new JsonSchema(node));
    }

    /// <summary>
    /// Generates a JSON Schema for the given CLR type using camelCase naming.
    /// </summary>
    /// <typeparam name="T">The CLR type to generate a schema for.</typeparam>
    /// <returns>A <see cref="JsonSchema"/> representing the type.</returns>
    public static JsonSchema FromType<T>() => FromType(typeof(T));

    /// <summary>
    /// Generates a JSON Schema for the given CLR type using camelCase naming.
    /// </summary>
    /// <param name="type">The CLR type to generate a schema for.</param>
    /// <returns>A <see cref="JsonSchema"/> representing the type.</returns>
    public static JsonSchema FromType(Type type)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        return FromType(type, options);
    }

    /// <summary>
    /// Generates a JSON Schema for the given CLR type using the specified serializer options.
    /// </summary>
    /// <param name="type">The CLR type to generate a schema for.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> controlling naming and serialization behavior.</param>
    /// <returns>A <see cref="JsonSchema"/> representing the type.</returns>
    public static JsonSchema FromType(Type type, JsonSerializerOptions options)
    {
        if (options.TypeInfoResolver is null)
        {
            options = new JsonSerializerOptions(options) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        }
        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, schema) =>
            {
                if (schema is JsonObject schemaObj)
                {
                    if (_typeFormats.IsKnown(context.TypeInfo.Type))
                    {
                        schemaObj["format"] = _typeFormats.GetFormatForType(context.TypeInfo.Type);
                    }

                    if (context.TypeInfo.Kind == System.Text.Json.Serialization.Metadata.JsonTypeInfoKind.Object)
                    {
                        schemaObj["title"] = context.TypeInfo.Type.Name;
                    }
                }
                return schema;
            }
        };
        var node = options.GetJsonSchemaAsNode(type, exporterOptions);
        return new JsonSchema(node.AsObject())
        {
            Title = type.Name
        };
    }

    /// <summary>
    /// Serializes this schema to a JSON string.
    /// </summary>
    /// <returns>The JSON string representation of this schema.</returns>
    public string ToJson() => Node.ToJsonString();

    /// <summary>
    /// Validates a JSON string against this schema (basic type and required property checks).
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>A list of <see cref="JsonSchemaValidationError"/> describing any validation errors.</returns>
    public IList<JsonSchemaValidationError> Validate(string json)
    {
        var errors = new List<JsonSchemaValidationError>();
        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj)
            {
                if (Type != JsonObjectType.None && !Type.HasFlag(JsonObjectType.Object))
                {
                    errors.Add(new JsonSchemaValidationError(null, JsonSchemaValidationErrorKind.WrongPropertyType, $"Expected object but got {node?.GetType().Name ?? "null"}."));
                }

                return errors;
            }

            // Check required properties
            if (Node["required"] is JsonArray required)
            {
                foreach (var propName in required
                    .Select(req => req?.GetValue<string>())
                    .Where(propName => propName is not null))
                {
                    if (!obj.ContainsKey(propName!) && !PropertyAllowsNull(propName!))
                    {
                        errors.Add(new JsonSchemaValidationError(propName, JsonSchemaValidationErrorKind.PropertyRequired, $"Property '{propName}' is required."));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            errors.Add(new JsonSchemaValidationError(null, JsonSchemaValidationErrorKind.Unknown, ex.Message));
        }

        return errors;

        bool PropertyAllowsNull(string propName)
        {
            if (!Properties.TryGetValue(propName, out var propSchema))
            {
                return false;
            }

            if (propSchema.Type.HasFlag(JsonObjectType.Null))
            {
                return true;
            }

            return propSchema.AnyOf.Any(s => s.Type == JsonObjectType.Null);
        }
    }

    static JsonObjectType ParseTypeFromNode(JsonObject node)
    {
        var typeNode = node["type"];
        if (typeNode is null) return JsonObjectType.None;

        if (typeNode is JsonValue strValue && strValue.TryGetValue<string>(out var typeStr))
        {
            return ParseTypeSingle(typeStr);
        }

        if (typeNode is JsonArray arr)
        {
            var result = JsonObjectType.None;
            foreach (var item in arr)
            {
                if (item is JsonValue val && val.TryGetValue<string>(out var s))
                {
                    result |= ParseTypeSingle(s);
                }
            }
            return result;
        }

        return JsonObjectType.None;
    }

    static JsonObjectType ParseTypeSingle(string type) => type switch
    {
        "array" => JsonObjectType.Array,
        "boolean" => JsonObjectType.Boolean,
        "integer" => JsonObjectType.Integer,
        "null" => JsonObjectType.Null,
        "number" => JsonObjectType.Number,
        "object" => JsonObjectType.Object,
        "string" => JsonObjectType.String,
        _ => JsonObjectType.None
    };

    static void SetTypeOnNode(JsonObject node, JsonObjectType type)
    {
        if (type == JsonObjectType.None)
        {
            node.Remove("type");
            return;
        }

        var types = new List<string>();
        if (type.HasFlag(JsonObjectType.Array)) types.Add("array");
        if (type.HasFlag(JsonObjectType.Boolean)) types.Add("boolean");
        if (type.HasFlag(JsonObjectType.Integer)) types.Add("integer");
        if (type.HasFlag(JsonObjectType.Null)) types.Add("null");
        if (type.HasFlag(JsonObjectType.Number)) types.Add("number");
        if (type.HasFlag(JsonObjectType.Object)) types.Add("object");
        if (type.HasFlag(JsonObjectType.String)) types.Add("string");

        node["type"] = types.Count == 1
            ? (JsonNode)types[0]
            : new JsonArray([.. types.Select(t => (JsonNode)t)]);
    }

    List<JsonSchema> BuildSchemaList(string key)
    {
        var list = new List<JsonSchema>();
        if (Node[key] is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is JsonObject obj)
                {
                    list.Add(new JsonSchema((JsonObject)obj.DeepClone(), Root));
                }
            }
        }
        return list;
    }

    JsonSchema? ResolveRef()
    {
        var refValue = Node["$ref"]?.GetValue<string>();
        if (refValue?.StartsWith('#') != true) return null;

        var parts = refValue.TrimStart('#').TrimStart('/').Split('/');
        if (parts.Length < 2) return null;

        var root = Root;
        var defsKey = parts[0]; // "$defs" or "definitions"
        var typeName = string.Join('/', parts.Skip(1));

        if (root.Node[defsKey] is JsonObject defs && defs[typeName] is JsonObject defObj)
        {
            return new JsonSchema((JsonObject)defObj.DeepClone(), root);
        }

        return null;
    }

    /// <summary>
    /// A dictionary that syncs property changes back to the underlying JSON node.
    /// </summary>
    /// <param name="parentNode">The parent JSON object node to sync property changes to.</param>
    sealed class SyncedPropertiesDictionary(JsonObject parentNode) : Dictionary<string, JsonSchemaProperty>(StringComparer.Ordinal)
    {
        public new JsonSchemaProperty this[string key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                EnsurePropertiesNode()[key] = value.Node.DeepClone();
            }
        }

        public new void Add(string key, JsonSchemaProperty value)
        {
            base.Add(key, value);
            EnsurePropertiesNode()[key] = value.Node.DeepClone();
        }

        public void Add(KeyValuePair<string, JsonSchemaProperty> kvp)
        {
            base.Add(kvp.Key, kvp.Value);
            EnsurePropertiesNode()[kvp.Key] = kvp.Value.Node.DeepClone();
        }

        public new bool Remove(string key)
        {
            var result = base.Remove(key);
            if (result && parentNode["properties"] is JsonObject propsObj)
            {
                propsObj.Remove(key);
            }
            return result;
        }

        public new void Clear()
        {
            base.Clear();
            parentNode.Remove("properties");
        }

        /// <summary>
        /// Loads a property into the cache without updating the parent node (used during initialization from existing JSON).
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The property schema.</param>
        internal void LoadWithoutSync(string key, JsonSchemaProperty value) => base[key] = value;

        JsonObject EnsurePropertiesNode()
        {
            if (parentNode["properties"] is not JsonObject propsObj)
            {
                propsObj = new JsonObject();
                parentNode["properties"] = propsObj;
            }
            return propsObj;
        }
    }

    /// <summary>
    /// A dictionary that provides access to extension data stored in the JSON node.
    /// </summary>
    /// <param name="node">The JSON object node containing the extension data.</param>
    sealed class ExtensionDataDictionary(JsonObject node) : IDictionary<string, object?>
    {
        public ICollection<string> Keys =>
            [.. node.Select(kvp => kvp.Key).Where(k => !_knownSchemaKeys.Contains(k))];

        public ICollection<object?> Values => [.. Keys.Select(k => this[k])];
        public int Count => Keys.Count;
        public bool IsReadOnly => false;

        public object? this[string key]
        {
            get
            {
                if (node[key] is JsonNode nodeVal)
                {
                    return DeserializeValue(nodeVal);
                }
                return null;
            }
            set => node[key] = SerializeValue(value);
        }

        public void Add(string key, object? value) => this[key] = value;
        public void Add(KeyValuePair<string, object?> item) => this[item.Key] = item.Value;

        public void Clear()
        {
            foreach (var key in Keys.ToList())
            {
                node.Remove(key);
            }
        }

        public bool Contains(KeyValuePair<string, object?> item) => ContainsKey(item.Key);
        public bool ContainsKey(string key) => node[key] is not null && !_knownSchemaKeys.Contains(key);

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            foreach (var k in Keys)
            {
                array[arrayIndex++] = new KeyValuePair<string, object?>(k, this[k]);
            }
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
            Keys.Select(k => new KeyValuePair<string, object?>(k, this[k])).GetEnumerator();

        public bool Remove(string key) => node.Remove(key);

        public bool Remove(KeyValuePair<string, object?> item) => Remove(item.Key);

        public bool TryGetValue(string key, out object? value)
        {
            if (node[key] is JsonNode nodeVal)
            {
                value = DeserializeValue(nodeVal);
                return true;
            }
            value = null;
            return false;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        static JsonNode? SerializeValue(object? value)
        {
            if (value is null) return null;
            if (value is JsonNode jsonNode) return jsonNode.DeepClone();
            return JsonSerializer.SerializeToNode(value);
        }

        static object? DeserializeValue(JsonNode jsonNode)
        {
            if (jsonNode is JsonValue val)
            {
                if (val.TryGetValue<string>(out var s)) return s;
                if (val.TryGetValue<bool>(out var b)) return b;
                if (val.TryGetValue<int>(out var i)) return i;
                if (val.TryGetValue<long>(out var l)) return l;
                if (val.TryGetValue<double>(out var d)) return d;
            }
            return jsonNode;
        }
    }
}
