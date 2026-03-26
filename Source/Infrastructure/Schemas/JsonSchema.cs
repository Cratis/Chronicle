// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using System.Linq;

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

    readonly JsonObject _node;
    readonly JsonSchema? _root;

    // Lazy caches
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
        _node = new JsonObject();
        _root = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchema"/> class from a JSON object.
    /// </summary>
    /// <param name="node">The JSON object representing the schema.</param>
    /// <param name="root">Optional root schema for $ref resolution.</param>
    public JsonSchema(JsonObject node, JsonSchema? root = null)
    {
        _node = node;
        _root = root;
    }

    /// <summary>
    /// Gets the root schema for $ref resolution.
    /// </summary>
    internal JsonSchema Root => _root ?? this;

    /// <summary>
    /// Gets the internal JSON node.
    /// </summary>
    internal JsonObject Node => _node;

    /// <summary>
    /// Gets or sets the JSON object type.
    /// </summary>
    public JsonObjectType Type
    {
        get => ParseTypeFromNode(_node);
        set => SetTypeOnNode(_node, value);
    }

    /// <summary>
    /// Gets or sets the format string.
    /// </summary>
    public string? Format
    {
        get => _node["format"]?.GetValue<string>();
        set
        {
            if (value is null)
            {
                _node.Remove("format");
            }
            else
            {
                _node["format"] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the title of the schema.
    /// </summary>
    public string? Title
    {
        get => _node["title"]?.GetValue<string>();
        set
        {
            if (value is null)
            {
                _node.Remove("title");
            }
            else
            {
                _node["title"] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the description of the schema.
    /// </summary>
    public string? Description
    {
        get => _node["description"]?.GetValue<string>();
        set
        {
            if (value is null)
            {
                _node.Remove("description");
            }
            else
            {
                _node["description"] = value;
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
                _propertiesCache = new SyncedPropertiesDictionary(_node);
                if (_node["properties"] is JsonObject propsNode)
                {
                    foreach (var (key, value) in propsNode)
                    {
                        if (value?.AsObject() is JsonObject propObj)
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
    public bool HasReference => _node["$ref"] is not null;

    /// <summary>
    /// Gets the resolved $ref schema.
    /// </summary>
    public JsonSchema? Reference => HasReference ? ResolveRef() : null;

    /// <summary>
    /// Gets the item schema (for arrays).
    /// </summary>
    public JsonSchemaProperty? Item
    {
        get
        {
            if (_node["items"] is JsonObject itemNode)
            {
                return new JsonSchemaProperty(string.Empty, (JsonObject)itemNode.DeepClone(), Root);
            }
            return null;
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
                var defsNode = (_node["$defs"] ?? _node["definitions"]) as JsonObject;
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
            if (_node["additionalProperties"] is JsonObject addlObj)
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
        _node["additionalProperties"] is not null &&
        (_node["properties"] is null ||
            (_node["properties"] is JsonObject propsObj && propsObj.Count == 0));

    /// <summary>
    /// Gets whether this schema defines an enumeration.
    /// </summary>
    public bool IsEnumeration => _node["enum"] is not null;

    /// <summary>
    /// Gets the enumeration names (string values from "enum").
    /// </summary>
    public IList<string> EnumerationNames
    {
        get
        {
            if (_node["enum"] is JsonArray arr)
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
            if (_node["enum"] is JsonArray arr)
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
                var inlineSchema = allOf.FirstOrDefault(s => !s.HasReference && s._node["properties"] is not null);
                if (inlineSchema is not null) return inlineSchema;
                return allOf[0].HasReference ? allOf[0].Reference ?? this : allOf[0];
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
    /// Gets or sets the extension data (custom JSON keys beyond the schema vocabulary).
    /// </summary>
    public IDictionary<string, object?>? ExtensionData
    {
        get => _extensionDataCache ??= new ExtensionDataDictionary(_node);
        set
        {
            if (value is null) return;
            _extensionDataCache = value as ExtensionDataDictionary;
            if (_extensionDataCache is null)
            {
                _extensionDataCache = new ExtensionDataDictionary(_node);
                foreach (var (key, val) in value)
                {
                    _extensionDataCache[key] = val;
                }
            }
        }
    }

    /// <summary>
    /// Serializes this schema to a JSON string.
    /// </summary>
    /// <returns>The JSON string representation of this schema.</returns>
    public string ToJson() => _node.ToJsonString();

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
            if (_node["required"] is JsonArray required)
            {
                foreach (var propName in required
                    .Select(req => req?.GetValue<string>())
                    .Where(propName => propName is not null))
                {
                    if (!obj.ContainsKey(propName))
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
    }

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
    public static Task<JsonSchema> FromJsonAsync(string json) => Task.FromResult(FromJson(json));

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
        var exporterOptions = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true };
        var node = JsonSchemaExporter.GetJsonSchemaAsNode(options, type, exporterOptions);
        return new JsonSchema(node.AsObject());
    }

    List<JsonSchema> BuildSchemaList(string key)
    {
        var list = new List<JsonSchema>();
        if (_node[key] is JsonArray arr)
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
        var refValue = _node["$ref"]?.GetValue<string>();
        if (refValue is null || !refValue.StartsWith('#')) return null;

        var parts = refValue.TrimStart('#').TrimStart('/').Split('/');
        if (parts.Length < 2) return null;

        var root = Root;
        var defsKey = parts[0]; // "$defs" or "definitions"
        var typeName = string.Join("/", parts.Skip(1));

        if (root._node[defsKey] is JsonObject defs && defs[typeName] is JsonObject defObj)
        {
            return new JsonSchema((JsonObject)defObj.DeepClone(), root);
        }

        return null;
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

    /// <summary>
    /// A dictionary that syncs property changes back to the underlying JSON node.
    /// </summary>
    sealed class SyncedPropertiesDictionary : Dictionary<string, JsonSchemaProperty>
    {
        readonly JsonObject _parentNode;

        public SyncedPropertiesDictionary(JsonObject parentNode) : base(StringComparer.Ordinal)
        {
            _parentNode = parentNode;
        }

        /// <summary>
        /// Loads a property into the cache without updating the parent node (used during initialization from existing JSON).
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The property schema.</param>
        internal void LoadWithoutSync(string key, JsonSchemaProperty value) => base[key] = value;

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
            if (result && _parentNode["properties"] is JsonObject propsObj)
            {
                propsObj.Remove(key);
            }
            return result;
        }

        public new void Clear()
        {
            base.Clear();
            _parentNode.Remove("properties");
        }

        JsonObject EnsurePropertiesNode()
        {
            if (_parentNode["properties"] is not JsonObject propsObj)
            {
                propsObj = new JsonObject();
                _parentNode["properties"] = propsObj;
            }
            return propsObj;
        }
    }

    /// <summary>
    /// A dictionary that provides access to extension data stored in the JSON node.
    /// </summary>
    sealed class ExtensionDataDictionary : IDictionary<string, object?>
    {
        readonly JsonObject _node;

        public ExtensionDataDictionary(JsonObject node)
        {
            _node = node;
        }

        public object? this[string key]
        {
            get
            {
                if (_node[key] is JsonNode nodeVal)
                {
                    return DeserializeValue(nodeVal);
                }
                return null;
            }
            set => _node[key] = SerializeValue(value);
        }

        public ICollection<string> Keys =>
            [.. _node.Select(kvp => kvp.Key).Where(k => !_knownSchemaKeys.Contains(k))];

        public ICollection<object?> Values => [.. Keys.Select(k => this[k])];
        public int Count => Keys.Count;
        public bool IsReadOnly => false;

        public void Add(string key, object? value) => this[key] = value;
        public void Add(KeyValuePair<string, object?> item) => this[item.Key] = item.Value;

        public void Clear()
        {
            foreach (var key in Keys.ToList())
            {
                _node.Remove(key);
            }
        }

        public bool Contains(KeyValuePair<string, object?> item) => ContainsKey(item.Key);
        public bool ContainsKey(string key) => _node[key] is not null && !_knownSchemaKeys.Contains(key);

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            foreach (var key in Keys)
            {
                array[arrayIndex++] = new KeyValuePair<string, object?>(key, this[key]);
            }
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
            Keys.Select(k => new KeyValuePair<string, object?>(k, this[k])).GetEnumerator();

        public bool Remove(string key) => _node.Remove(key);

        public bool Remove(KeyValuePair<string, object?> item) => Remove(item.Key);

        public bool TryGetValue(string key, out object? value)
        {
            if (_node[key] is JsonNode nodeVal)
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
            if (value is JsonNode node) return node.DeepClone();
            return JsonSerializer.SerializeToNode(value);
        }

        static object? DeserializeValue(JsonNode node)
        {
            if (node is JsonValue val)
            {
                if (val.TryGetValue<string>(out var s)) return s;
                if (val.TryGetValue<bool>(out var b)) return b;
                if (val.TryGetValue<int>(out var i)) return i;
                if (val.TryGetValue<long>(out var l)) return l;
                if (val.TryGetValue<double>(out var d)) return d;
            }
            return node;
        }
    }
}
