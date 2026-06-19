// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Geospatial;
using Cratis.Json;
using Cratis.Reflection;
using Cratis.Serialization;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IJsonSchemaGenerator"/>.
/// </summary>
[Singleton]
public class JsonSchemaGenerator : IJsonSchemaGenerator
{
    static FieldInfo? _paramDefaultValueField;

    readonly JsonSerializerOptions _serializerOptions;
    readonly JsonSchemaExporterOptions _exporterOptions;
    readonly IComplianceMetadataResolver _metadataResolver;
    readonly IDerivedTypes _derivedTypes;
    readonly TypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.
    /// </summary>
    /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> to use for converting names during serialization.</param>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> used to recognize polymorphic base types adorned with <see cref="DerivedTypeAttribute"/>. Defaults to the global <see cref="DerivedTypes.Instance"/>.</param>
    public JsonSchemaGenerator(IComplianceMetadataResolver metadataResolver, INamingPolicy namingPolicy, IDerivedTypes? derivedTypes = null)
    {
        _metadataResolver = metadataResolver;
        _derivedTypes = derivedTypes ?? DerivedTypes.Instance;
        _typeFormats = new TypeFormats();

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(FixStructDefaultValues);

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = namingPolicy.JsonPropertyNamingPolicy,
            TypeInfoResolver = resolver,
            Converters =
            {
                new EnumerableConceptAsJsonConverterFactory(),
                new ConceptAsJsonConverterFactory()
            }
        };

        _exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = TransformNode
        };
    }

    /// <inheritdoc/>
    public JsonSchema Generate(Type type)
    {
        var node = _serializerOptions.GetJsonSchemaAsNode(type, _exporterOptions);
        return new JsonSchema(node.AsObject());
    }

    static FieldInfo GetParameterDefaultValueField(JsonParameterInfo paramInfo)
    {
        if (_paramDefaultValueField is not null) return _paramDefaultValueField;

        var type = paramInfo.GetType();
        while (type is not null)
        {
            var field = type.GetField(
                "<DefaultValue>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (field is not null)
            {
                _paramDefaultValueField = field;
                return field;
            }

            type = type.BaseType;
        }

        throw new InvalidOperationException("Could not find DefaultValue backing field on JsonParameterInfo.");
    }

    static void FixStructDefaultValues(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var property in typeInfo.Properties)
        {
            if (property.AssociatedParameter is not { HasDefaultValue: true, DefaultValue: null } paramInfo)
                continue;
            if (!paramInfo.ParameterType.IsValueType)
                continue;
            if (Nullable.GetUnderlyingType(paramInfo.ParameterType) is not null)
                continue;

            var field = GetParameterDefaultValueField(paramInfo);
            field.SetValue(paramInfo, Activator.CreateInstance(paramInfo.ParameterType));
        }
    }

    static void AddComplianceMetadata(JsonObject schema, IEnumerable<ComplianceMetadata> metadata)
    {
        if (!schema.ContainsKey(ComplianceJsonSchemaExtensions.ComplianceKey))
        {
            schema[ComplianceJsonSchemaExtensions.ComplianceKey] = new JsonArray();
        }

        var complianceArr = schema[ComplianceJsonSchemaExtensions.ComplianceKey]!.AsArray();
        foreach (var item in metadata)
        {
            complianceArr.Add(JsonSerializer.SerializeToNode(
                new ComplianceSchemaMetadata(item.MetadataType.Value, item.Details)));
        }
    }

    static bool IsNullableConceptProperty(JsonSchemaExporterContext context)
    {
        var nullabilityCtx = new NullabilityInfoContext();
        switch (context.PropertyInfo?.AttributeProvider)
        {
            case ParameterInfo paramInfo:
                var paramNullability = nullabilityCtx.Create(paramInfo);
                return paramNullability.ReadState == NullabilityState.Nullable ||
                       paramNullability.WriteState == NullabilityState.Nullable;
            case PropertyInfo propInfo:
                var propNullability = nullabilityCtx.Create(propInfo);
                return propNullability.ReadState == NullabilityState.Nullable ||
                       propNullability.WriteState == NullabilityState.Nullable;
            default:
                return false;
        }
    }

    JsonNode TransformNode(JsonSchemaExporterContext context, JsonNode schema)
    {
        var type = context.TypeInfo.Type;
        var formatType = Nullable.GetUnderlyingType(type) ?? type;

        // Handle concept types - redirect to the underlying primitive type's schema
        if (type.IsConcept())
        {
            var underlyingType = type.GetConceptValueType();
            var conceptSchema = context.TypeInfo.Options.GetJsonSchemaAsNode(underlyingType, _exporterOptions);

            // Preserve nullable marker for concept types (e.g. EventSequenceNumber?).
            // STJ's JsonSchemaExporter does not propagate NRT nullable markers through custom converters,
            // so we check the actual property nullability via NullabilityInfoContext. When the property
            // is nullable we append '?' to the format so that IsNullable() returns true and
            // GetDefaultValue() returns null rather than the primitive default (e.g. 0 for ulong).
            if (conceptSchema is JsonObject conceptSchemaObj)
            {
                if (_metadataResolver.HasMetadataFor(type))
                {
                    AddComplianceMetadata(conceptSchemaObj, _metadataResolver.GetMetadataFor(type));
                }

                if (IsNullableConceptProperty(context) &&
                    conceptSchemaObj.TryGetPropertyValue("format", out var format))
                {
                    var formatStr = format!.GetValue<string>();
                    if (!formatStr.EndsWith('?'))
                    {
                        conceptSchemaObj["format"] = formatStr + "?";
                    }
                }
            }

            return conceptSchema;
        }

        // Handle enumerables whose element type is a concept (e.g. IReadOnlyList<Requirement>).
        // System.Text.Json's schema exporter cannot introspect the EnumerableConceptAsJsonConverter,
        // so it emits a permissive boolean schema (`true`) for the property. A non-object schema is
        // not a JsonObject, so it is excluded from the read model's flattened properties — which
        // silently drops the property from the persisted document (it never reaches the storage sink).
        // Emit a proper array schema whose items are the concept's underlying primitive schema, the
        // same primitive mapping a scalar concept gets, so the value round-trips through the sink.
        if (type != typeof(string) && type.IsEnumerable() && !type.IsDictionary())
        {
            var elementType = type.GetEnumerableElementType();
            if (elementType?.IsConcept() == true)
            {
                var underlyingItemType = elementType.GetConceptValueType();
                var itemSchema = context.TypeInfo.Options.GetJsonSchemaAsNode(underlyingItemType, _exporterOptions);
                return new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = itemSchema
                };
            }
        }

        if (schema is not JsonObject schemaObj) return schema;

        // Polymorphic base types (those that have registered derived types via [DerivedType]) are
        // stored verbatim. System.Text.Json's schema exporter only describes the abstract base's
        // own properties, which would make the schema-driven storage conversion strip the derived
        // type discriminator (_derivedTypeId) together with every concrete subtype property. Emitting
        // an open object schema (no fixed properties) makes the ExpandoObject converter preserve the
        // full polymorphic payload so the discriminator and concrete properties round-trip intact.
        if (!schemaObj.ContainsKey("$ref") && _derivedTypes.HasDerivatives(formatType))
        {
            schemaObj.Remove("properties");
            schemaObj.Remove("required");
            schemaObj.Remove("allOf");
            schemaObj.Remove("anyOf");
            schemaObj.Remove("oneOf");
            schemaObj.Remove("additionalProperties");
            schemaObj["type"] = "object";
            return schemaObj;
        }

        // Geospatial types serialize as GeoJSON via their own converters and are materialized as the
        // typed CLR value by the schema-aware ExpandoObject converters using the format metadata
        // (set from the registered type formats in the "known types" block below). Emit a leaf schema
        // carrying only that format so the value is treated as a single typed value and not flattened
        // into sub-properties (which would drop the coordinates and surface as a JsonElement).
        if (formatType == typeof(Point) || formatType == typeof(LineString) || formatType == typeof(Polygon))
        {
            schemaObj.Remove("properties");
            schemaObj.Remove("required");
            schemaObj["type"] = "object";
        }

        // For enum types, embed the integer values and string names so that converters can
        // detect enum fields and map between integer BSON values and string enum names.
        if (formatType.IsEnum)
        {
            var enumValues = Enum.GetValuesAsUnderlyingType(formatType).Cast<int>().ToArray();
            var enumNames = Enum.GetNames(formatType);
            schemaObj["enum"] = new JsonArray([.. enumValues.Select(v => (JsonNode?)JsonValue.Create(v))]);
            schemaObj["x-enumNames"] = new JsonArray([.. enumNames.Select(n => (JsonNode?)JsonValue.Create(n))]);
        }

        // Add format for known types
        if (_typeFormats.IsKnown(formatType))
        {
            schemaObj["format"] = _typeFormats.GetFormatForType(formatType);
        }

        // Add compliance metadata for the type
        if (_metadataResolver.HasMetadataFor(type))
        {
            AddComplianceMetadata(schemaObj, _metadataResolver.GetMetadataFor(type));
        }

        // Add compliance metadata for the property
        if (context.PropertyInfo?.AttributeProvider is PropertyInfo propInfo &&
            _metadataResolver.HasMetadataFor(propInfo))
        {
            AddComplianceMetadata(schemaObj, _metadataResolver.GetMetadataFor(propInfo));
        }
        else if (context.PropertyInfo?.AttributeProvider is ParameterInfo paramInfo &&
            paramInfo.Member.DeclaringType is { } recordType)
        {
            var recordProp = recordType.GetProperty(
                paramInfo.Name ?? string.Empty,
                BindingFlags.Public | BindingFlags.Instance);
            if (recordProp is not null && _metadataResolver.HasMetadataFor(recordProp))
            {
                AddComplianceMetadata(schemaObj, _metadataResolver.GetMetadataFor(recordProp));
            }
        }

        // Add title and compensation metadata — only applies to top-level type schema (no property context)
        if (context.PropertyInfo is null)
        {
            if (context.TypeInfo.Kind == JsonTypeInfoKind.Object)
            {
                schemaObj["title"] = type.Name;
            }

            var compensationAttribute = type.GetCustomAttribute<CompensationForAttribute>();
            if (compensationAttribute is not null)
            {
                var compensatedEventType = compensationAttribute.CompensatedEventType.GetEventType();
                schemaObj[CompensationJsonSchemaExtensions.CompensationForKey] = compensatedEventType.Id.Value;
            }
        }

        return schema;
    }
}
