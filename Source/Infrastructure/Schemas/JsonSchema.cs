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

    /// <summary>
    /// Lazy caches for parsed and derived schema components.
    /// </summary>
    /// <remarks>
    /// A single <see cref="JsonSchema"/> instance is cached and shared (for example the event-type schemas held by the
    /// client <c>EventTypes</c>) and read concurrently — projections, reducers, key resolvers, the MongoDB converter,
    /// constraint registration, schema validation, and compliance handling all read the same instance. Because a schema
    /// is effectively immutable once built (it is parsed from stored JSON and then only read), the derived answers below
    /// — the flattened property set, whether the schema carries compliance metadata, and the resolved <c>$ref</c>/item
    /// schemas — are memoized on the instance rather than recomputed per read; there is no cluster-wide invalidation
    /// concern because nothing mutates a published schema, and each memo is bounded by the schema's own size. The caches
    /// are published with release semantics: each is built fully into a local before the <c>volatile</c> field is
    /// assigned, so a concurrent reader never observes a half-populated dictionary or list (which previously surfaced as
    /// a property "not existing").
    /// </remarks>
    volatile SyncedPropertiesDictionary? _propertiesCache;
    volatile List<JsonSchema>? _allOfCache;
    volatile List<JsonSchema>? _anyOfCache;
    volatile List<JsonSchema>? _oneOfCache;
    volatile Dictionary<string, JsonSchema>? _definitionsCache;
    volatile ExtensionDataDictionary? _extensionDataCache;
    volatile IReadOnlyList<JsonSchemaProperty>? _flattenedPropertiesCache;
    volatile Dictionary<string, JsonSchemaProperty>? _flattenedPropertiesByNameCache;
    volatile Cached<bool>? _hasComplianceMetadataCache;
    volatile Cached<JsonSchema?>? _referenceCache;
    volatile Cached<JsonSchema?>? _itemCache;

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
            var cached = _propertiesCache;
            if (cached is not null)
            {
                return cached;
            }

            // Build the dictionary fully into a local before publishing it, so a concurrent reader never sees a
            // partially-populated cache (which would make a present property appear to be missing).
            var properties = new SyncedPropertiesDictionary(Node);
            if (Node["properties"] is JsonObject propsNode)
            {
                foreach (var (key, value) in propsNode)
                {
                    if (value is JsonObject propObj)
                    {
                        var propNode = (JsonObject)propObj.DeepClone();
                        var prop = new JsonSchemaProperty(key, propNode, Root);
                        properties.LoadWithoutSync(key, prop);
                    }
                }
            }

            return _propertiesCache = properties;
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
    public JsonSchema? Reference
    {
        get
        {
            var cached = _referenceCache;
            if (cached is not null)
            {
                return cached.Value;
            }

            var resolved = HasReference ? ResolveRef() : null;
            return (_referenceCache = new Cached<JsonSchema?>(resolved)).Value;
        }
    }

    /// <summary>
    /// Gets or sets the item schema (for arrays).
    /// </summary>
    public JsonSchema? Item
    {
        get
        {
            var cached = _itemCache;
            if (cached is not null)
            {
                return cached.Value;
            }

            var resolved = Node["items"] is JsonObject itemNode
                ? new JsonSchemaProperty(string.Empty, (JsonObject)itemNode.DeepClone(), Root)
                : null;
            return (_itemCache = new Cached<JsonSchema?>(resolved)).Value;
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

            _itemCache = null;
        }
    }

    /// <summary>
    /// Gets the definitions ($defs or definitions).
    /// </summary>
    public IDictionary<string, JsonSchema> Definitions
    {
        get
        {
            var cached = _definitionsCache;
            if (cached is not null)
            {
                return cached;
            }

            // Build the dictionary fully into a local before publishing it, so a concurrent reader never sees a
            // partially-populated cache.
            var definitions = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
            var defsNode = (Node["$defs"] ?? Node["definitions"]) as JsonObject;
            if (defsNode is not null)
            {
                foreach (var (key, value) in defsNode)
                {
                    if (value is JsonObject defObj)
                    {
                        definitions[key] = new JsonSchema((JsonObject)defObj.DeepClone(), this);
                    }
                }
            }

            return _definitionsCache = definitions;
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
    /// Gets the enumeration names (string values from "x-enumNames" extension, or "enum" array if string-valued).
    /// </summary>
    public IList<string> EnumerationNames
    {
        get
        {
            if (Node["x-enumNames"] is JsonArray namesArr)
            {
                return [.. namesArr.Select(v => v?.GetValue<string>() ?? string.Empty)];
            }
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
                if (nonNull is not null)
                {
                    return nonNull.HasReference ? (nonNull.Reference ?? nonNull) : nonNull;
                }

                return this;
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
    /// Gets the flattened properties of this schema, including inherited properties resolved through <c>allOf</c> references.
    /// </summary>
    internal IReadOnlyList<JsonSchemaProperty> FlattenedProperties => _flattenedPropertiesCache ??= BuildFlattenedProperties();

    /// <summary>
    /// Gets the flattened properties keyed by name, using case-insensitive lookup.
    /// </summary>
    internal IReadOnlyDictionary<string, JsonSchemaProperty> FlattenedPropertiesByName => _flattenedPropertiesByNameCache ??= BuildFlattenedPropertiesByName();

    /// <summary>
    /// Gets or sets the memoized answer to whether this schema carries compliance metadata.
    /// A getter value of <see langword="null"/> means it has not been computed yet.
    /// </summary>
    internal bool? CachedHasComplianceMetadata
    {
        get => _hasComplianceMetadataCache is { } cache ? cache.Value : null;
        set => _hasComplianceMetadataCache = value is { } computed ? new Cached<bool>(computed) : null;
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

                    if (context.TypeInfo.Kind == JsonTypeInfoKind.Object)
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
            ValidateContent(JsonNode.Parse(json), errors);
        }
        catch (JsonException ex)
        {
            errors.Add(new JsonSchemaValidationError(null, JsonSchemaValidationErrorKind.Unknown, ex.Message));
        }

        return errors;
    }

    /// <summary>
    /// Validates an already-parsed JSON object against this schema (required property and value type checks).
    /// </summary>
    /// <param name="content">The <see cref="JsonObject"/> to validate.</param>
    /// <returns>A list of <see cref="JsonSchemaValidationError"/> describing any validation errors.</returns>
    /// <remarks>
    /// Equivalent to <see cref="Validate(string)"/> for object content, but avoids re-parsing content the caller already holds.
    /// </remarks>
    public IList<JsonSchemaValidationError> Validate(JsonObject content)
    {
        var errors = new List<JsonSchemaValidationError>();
        ValidateContent(content, errors);
        return errors;
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
    /// Validates the declared properties of a schema against the values supplied for them.
    /// </summary>
    /// <param name="schema">The schema declaring the properties.</param>
    /// <param name="content">The content holding the values.</param>
    /// <param name="path">The dotted path to <paramref name="content"/>, or <see langword="null"/> at the root.</param>
    /// <param name="errors">The list to collect errors into.</param>
    /// <remarks>
    /// Only properties the schema declares and the content supplies are considered. A property the schema does not
    /// declare is left alone - JSON Schema treats an undeclared property as unconstrained unless the schema says
    /// otherwise, and the append path is the wrong place to invent a rule the schema did not state.
    /// </remarks>
    static void ValidatePropertyTypes(JsonSchema schema, JsonObject content, string? path, List<JsonSchemaValidationError> errors)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in schema.FlattenedProperties)
        {
            if (!visited.Add(property.Name)) continue;
            if (!content.TryGetPropertyValue(property.Name, out var value)) continue;

            ValidateValue(property, value, CombinePath(path, property.Name), errors);
        }
    }

    /// <summary>
    /// Validates a single value against the schema declared for it, recursing into objects and array items.
    /// </summary>
    /// <param name="schema">The schema the value must conform to.</param>
    /// <param name="value">The value, which is <see langword="null"/> for a JSON null.</param>
    /// <param name="path">The dotted path identifying the value, or <see langword="null"/> at the root.</param>
    /// <param name="errors">The list to collect errors into.</param>
    static void ValidateValue(JsonSchema schema, JsonNode? value, string? path, List<JsonSchemaValidationError> errors)
    {
        var effective = Effective(schema);
        var branches = UnionBranches(effective);
        if (branches.Count > 0)
        {
            ValidateAgainstUnion(effective, branches, value, path, errors);
            return;
        }

        if (!KindIsAllowed(effective, value))
        {
            errors.Add(Mismatch(effective, value, path));
            return;
        }

        ValidateStructure(effective, value, path, errors);
    }

    /// <summary>
    /// Validates a value against a union of schemas expressed as <c>anyOf</c> or <c>oneOf</c>.
    /// </summary>
    /// <param name="schema">The schema declaring the union.</param>
    /// <param name="branches">The union branches.</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="path">The dotted path identifying the value.</param>
    /// <param name="errors">The list to collect errors into.</param>
    /// <remarks>
    /// A value that matches no branch by kind is a genuine mismatch. When several branches accept the kind - a
    /// discriminated union of object shapes, for instance - there is no way to know which branch the writer meant, so
    /// a clean match against any of them is accepted and an unclean one is passed over in silence rather than reported
    /// against an arbitrarily chosen branch.
    /// </remarks>
    static void ValidateAgainstUnion(JsonSchema schema, IReadOnlyList<JsonSchema> branches, JsonNode? value, string? path, List<JsonSchemaValidationError> errors)
    {
        var candidates = branches.Select(Effective).Where(branch => KindIsAllowed(branch, value)).ToList();
        if (candidates.Count == 0)
        {
            errors.Add(Mismatch(schema, value, path));
        }
        else if (candidates.Count == 1)
        {
            ValidateStructure(candidates[0], value, path, errors);
        }

        // More than one branch accepts this value's kind, so there is no single branch to blame and the value
        // is accepted. Deliberately without descending into the candidates: the outcome is the same whether a
        // branch validates or none does, so probing them decides nothing - and because a probe is itself a full
        // recursive validation, a nested union would branch again at every level. That is exponential work on
        // the append path for an answer that is fixed in advance.
    }

    /// <summary>
    /// Recurses into the members of a value whose kind the schema already accepted.
    /// </summary>
    /// <param name="schema">The schema the value conforms to.</param>
    /// <param name="value">The value to recurse into.</param>
    /// <param name="path">The dotted path identifying the value.</param>
    /// <param name="errors">The list to collect errors into.</param>
    static void ValidateStructure(JsonSchema schema, JsonNode? value, string? path, List<JsonSchemaValidationError> errors)
    {
        switch (value)
        {
            case JsonObject nested:
                ValidatePropertyTypes(schema, nested, path, errors);
                break;

            case JsonArray array when schema.Item is { } itemSchema:
                for (var index = 0; index < array.Count; index++)
                {
                    ValidateValue(itemSchema, array[index], $"{path}[{index}]", errors);
                }

                break;
        }
    }

    /// <summary>
    /// Gets whether the kind of a value is one the schema declares.
    /// </summary>
    /// <param name="schema">The schema to check against.</param>
    /// <param name="value">The value, which is <see langword="null"/> for a JSON null.</param>
    /// <returns><see langword="true"/> when the kind is allowed; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// A schema with no declared type constrains nothing, so an absent type is never a mismatch. A whole-valued number
    /// satisfies <c>integer</c>, and any number satisfies <c>number</c> - JSON does not distinguish the two on the wire.
    /// </remarks>
    static bool KindIsAllowed(JsonSchema schema, JsonNode? value)
    {
        var type = schema.Type;
        if (type == JsonObjectType.None) return true;
        if (value is null) return AllowsNull(schema);

        return value.GetValueKind() switch
        {
            JsonValueKind.Object => type.HasFlag(JsonObjectType.Object),
            JsonValueKind.Array => type.HasFlag(JsonObjectType.Array),
            JsonValueKind.String => type.HasFlag(JsonObjectType.String),
            JsonValueKind.True or JsonValueKind.False => type.HasFlag(JsonObjectType.Boolean),
            JsonValueKind.Number => type.HasFlag(JsonObjectType.Number) || (type.HasFlag(JsonObjectType.Integer) && IsWholeNumber(value)),
            JsonValueKind.Null => AllowsNull(schema),
            _ => true
        };
    }

    /// <summary>
    /// Gets whether a schema accepts a JSON null.
    /// </summary>
    /// <param name="schema">The schema to check.</param>
    /// <returns><see langword="true"/> when null is allowed; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// This is the schema-level form of <c>JsonSchemaExtensions.IsNullable</c>: nullability is declared either as a
    /// <c>"null"</c> member of the type, or - for a formatted type that has nowhere else to put the marker - as a
    /// trailing <c>?</c> on the format.
    /// </remarks>
    static bool AllowsNull(JsonSchema schema) =>
        schema.Type.HasFlag(JsonObjectType.Null) ||
        (schema.Format?.EndsWith('?') ?? false);

    /// <summary>
    /// Gets whether a JSON number has no fractional part.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> when the number is whole, or when it cannot be read as one of the numeric types tried.</returns>
    /// <remarks>
    /// A number whose representation cannot be read is reported as whole. Refusing to append a payload because a
    /// number could not be inspected is a worse outcome than letting an exotic one through.
    /// </remarks>
    static bool IsWholeNumber(JsonNode value)
    {
        if (value is not JsonValue jsonValue) return true;
        if (jsonValue.TryGetValue<decimal>(out var asDecimal)) return asDecimal == decimal.Truncate(asDecimal);
        if (jsonValue.TryGetValue<double>(out var asDouble) && double.IsFinite(asDouble)) return asDouble == Math.Truncate(asDouble);
        return true;
    }

    /// <summary>
    /// Resolves a schema to the one that carries its type information, following a <c>$ref</c>.
    /// </summary>
    /// <param name="schema">The schema to resolve.</param>
    /// <returns>The resolved schema, or the original when there is nothing to resolve.</returns>
    /// <remarks>
    /// An unresolvable <c>$ref</c> falls back to the referencing node, which declares no type of its own and therefore
    /// constrains nothing - a reference that cannot be followed must not become a rejection.
    /// </remarks>
    static JsonSchema Effective(JsonSchema schema) => schema.HasReference ? schema.Reference ?? schema : schema;

    static IReadOnlyList<JsonSchema> UnionBranches(JsonSchema schema)
    {
        var anyOf = schema.AnyOf;
        var oneOf = schema.OneOf;
        if (anyOf.Count == 0 && oneOf.Count == 0) return [];
        return [.. anyOf, .. oneOf];
    }

    static JsonSchemaValidationError Mismatch(JsonSchema schema, JsonNode? value, string? path)
    {
        var expected = DescribeType(schema.Type);
        var actual = DescribeValue(value);
        var message = path is null
            ? $"Expected {expected} but got {actual}."
            : $"Property '{path}' expected {expected} but got {actual}.";

        return new JsonSchemaValidationError(path, JsonSchemaValidationErrorKind.WrongPropertyType, message);
    }

    static string DescribeType(JsonObjectType type)
    {
        if (type == JsonObjectType.None) return "any";

        var names = new List<string>();
        if (type.HasFlag(JsonObjectType.Array)) names.Add("array");
        if (type.HasFlag(JsonObjectType.Boolean)) names.Add("boolean");
        if (type.HasFlag(JsonObjectType.Integer)) names.Add("integer");
        if (type.HasFlag(JsonObjectType.Null)) names.Add("null");
        if (type.HasFlag(JsonObjectType.Number)) names.Add("number");
        if (type.HasFlag(JsonObjectType.Object)) names.Add("object");
        if (type.HasFlag(JsonObjectType.String)) names.Add("string");

        return string.Join(" or ", names);
    }

    static string DescribeValue(JsonNode? value) => value?.GetValueKind() switch
    {
        null or JsonValueKind.Null => "null",
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        JsonValueKind.Number => "number",
        _ => "unknown"
    };

    static string CombinePath(string? path, string name) => path is null ? name : $"{path}.{name}";

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

    List<JsonSchemaProperty> BuildFlattenedProperties()
    {
        var properties = new List<JsonSchemaProperty>();
        CollectPropertiesInto(properties);
        return properties;
    }

    void CollectPropertiesInto(List<JsonSchemaProperty> properties)
    {
        // Direct properties on this schema.
        properties.AddRange(Properties.Values);

        // Traverse allOf schemas (handles both inheritance refs and inline property groups).
        foreach (var allOfSchema in AllOf)
        {
            if (allOfSchema.HasReference)
            {
                allOfSchema.Reference?.CollectPropertiesInto(properties);
            }
            else
            {
                allOfSchema.CollectPropertiesInto(properties);
            }
        }
    }

    Dictionary<string, JsonSchemaProperty> BuildFlattenedPropertiesByName() =>
        FlattenedProperties.ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);

    JsonSchema? ResolveRef()
    {
        var refValue = Node["$ref"]?.GetValue<string>();
        if (refValue?.StartsWith('#') != true) return null;

        var root = Root;
        var fragment = refValue[1..];

        // A bare "#" (or "#/") references the whole root document.
        if (fragment.Length == 0 || fragment == "/")
        {
            return root;
        }

        // Resolve the fragment as a JSON Pointer (RFC 6901) into the root document. This covers both
        // definition references (#/$defs/<name>, #/definitions/<name>) and the in-document pointers that
        // System.Text.Json emits for recurring and self-referential types (e.g. #/properties/Features/items),
        // which would otherwise fail to resolve and leave the referenced schema empty.
        JsonNode? current = root.Node;
        foreach (var rawSegment in fragment.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = rawSegment.Replace("~1", "/").Replace("~0", "~");
            switch (current)
            {
                case JsonObject obj when obj.TryGetPropertyValue(segment, out var next):
                    current = next;
                    break;
                case JsonArray array when int.TryParse(segment, out var index) && index >= 0 && index < array.Count:
                    current = array[index];
                    break;
                default:
                    return null;
            }
        }

        return current is JsonObject resolved ? new JsonSchema((JsonObject)resolved.DeepClone(), root) : null;
    }

    void ValidateRequiredProperties(JsonObject content, List<JsonSchemaValidationError> errors)
    {
        var schemaProperties = FlattenedPropertiesByName;

        if (Node["required"] is JsonArray required)
        {
            foreach (var propName in required
                .Select(req => req?.GetValue<string>())
                .Where(propName => propName is not null))
            {
                if (!content.ContainsKey(propName!))
                {
                    if (schemaProperties.TryGetValue(propName!, out var schemaProperty) &&
                        (schemaProperty.Type.HasFlag(JsonObjectType.Null) || schemaProperty.IsNullable()))
                    {
                        continue;
                    }

                    errors.Add(new JsonSchemaValidationError(propName, JsonSchemaValidationErrorKind.PropertyRequired, $"Property '{propName}' is required."));
                }
            }
        }
    }

    /// <summary>
    /// Validates content against this schema, from whichever overload the caller reached.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <param name="errors">The list to collect errors into.</param>
    /// <remarks>
    /// Both public overloads funnel through here so they agree. The kind of the root content is checked against the
    /// schema's own declared type, and required properties are only checked once the content is known to be an object
    /// the schema accepts - reporting a missing property on content that is not even the right shape says nothing useful.
    /// </remarks>
    void ValidateContent(JsonNode? content, List<JsonSchemaValidationError> errors)
    {
        if (content is JsonObject rootObject && KindIsAllowed(Effective(this), rootObject))
        {
            ValidateRequiredProperties(rootObject, errors);
        }

        ValidateValue(this, content, null, errors);
    }

    /// <summary>
    /// A write-once holder for a memoized value, published to a <see langword="volatile"/> field so a concurrent reader
    /// distinguishes an absent holder (not yet computed) from a computed value that may itself be <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The type of the memoized value.</typeparam>
    /// <param name="value">The value to hold.</param>
    sealed class Cached<T>(T value)
    {
        public T Value { get; } = value;
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
