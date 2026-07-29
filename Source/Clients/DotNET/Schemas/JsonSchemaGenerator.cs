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

    /// <summary>
    /// Adds compliance metadata to a schema node, descending into an object's properties so that the
    /// metadata always lands on the leaves that actually hold a value.
    /// </summary>
    /// <param name="schema">The schema node to add to.</param>
    /// <param name="metadata">The <see cref="ComplianceMetadata"/> to add.</param>
    /// <remarks>
    /// A compliance marker can be declared on something that is not a single value: a <c>[PII]</c> attribute
    /// on a composite value-object type, or on a property whose type is such an object. Compliance is applied
    /// per value, so leaving the marker on the container would make Chronicle hand the whole JSON object to the
    /// value handler and store one opaque ciphertext string where the schema still says "object". Releasing that
    /// gives back a string, not an object, and the read model then fails to materialize. Pushing the metadata
    /// down to every leaf keeps encryption symmetric with the release walk, keeps each value independently
    /// encrypted, and preserves the document shape.
    /// <para>
    /// An array-typed node is deliberately left as a container: coarse compliance on a whole collection is an
    /// established, separately handled behavior (the collection is blob-encrypted and its shape restored on
    /// release). Object members reached *through* an array's item schema are still descended into.
    /// </para>
    /// </remarks>
    static void AddComplianceMetadata(JsonObject schema, IEnumerable<ComplianceMetadata> metadata)
    {
        var metadataAsArray = metadata as IReadOnlyCollection<ComplianceMetadata> ?? [.. metadata];

        if (schema["properties"] is JsonObject properties && properties.Count > 0)
        {
            foreach (var (_, propertySchema) in properties.ToArray())
            {
                if (propertySchema is JsonObject propertySchemaObject)
                {
                    AddComplianceMetadata(propertySchemaObject, metadataAsArray);
                }
            }

            return;
        }

        if (!schema.ContainsKey(ComplianceJsonSchemaExtensions.ComplianceKey))
        {
            schema[ComplianceJsonSchemaExtensions.ComplianceKey] = new JsonArray();
        }

        var complianceArr = schema[ComplianceJsonSchemaExtensions.ComplianceKey]!.AsArray();
        foreach (var item in metadataAsArray.Where(item => !HasMetadataOfType(complianceArr, item.MetadataType.Value)))
        {
            complianceArr.Add(JsonSerializer.SerializeToNode(
                new ComplianceSchemaMetadata(item.MetadataType.Value, item.Details)));
        }
    }

    /// <summary>
    /// Checks whether a compliance array already carries metadata of a given type.
    /// </summary>
    /// <param name="complianceArray">The compliance array to check.</param>
    /// <param name="metadataType">The metadata type to look for.</param>
    /// <returns>True when the metadata type is already present, false if not.</returns>
    /// <remarks>
    /// A leaf can be reached by more than one marker — for example a <c>[PII]</c> concept inside a value object
    /// whose type is itself marked <c>[PII]</c>. Recording the same metadata type twice adds nothing and makes
    /// the generated schema noisier to read and to diff.
    /// </remarks>
    static bool HasMetadataOfType(JsonArray complianceArray, string metadataType) =>
        complianceArray
            .OfType<JsonObject>()
            .Any(_ => _[nameof(ComplianceSchemaMetadata.metadataType)]?.GetValue<string>() == metadataType);

    static bool IsAnnotatedNullable(JsonSchemaExporterContext context)
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

    static bool PropertyIsNullable(Type type, JsonSchemaExporterContext context) =>
        Nullable.GetUnderlyingType(type) is not null || IsAnnotatedNullable(context);

    static void ThrowIfSelfReferencing(Type type, Type representedAs)
    {
        if (representedAs == type)
        {
            throw new SelfReferencingJsonSchemaType(type);
        }
    }

    JsonNode TransformNode(JsonSchemaExporterContext context, JsonNode schema)
    {
        var type = context.TypeInfo.Type;
        var formatType = Nullable.GetUnderlyingType(type) ?? type;

        // An explicit [JsonSchemaType] declaration states what a type's own JsonConverter actually produces.
        // System.Text.Json's schema exporter cannot introspect a custom converter, so without this the schema
        // describes the CLR shape while the wire carries something else entirely — and the value stops
        // round-tripping through the sink. It is resolved before the concept branch so an explicit declaration
        // always wins over an inferred representation, and after the Nullable<> unwrap so that a nullable
        // value type is recognized as its adorned underlying type.
        if (formatType.GetCustomAttribute<JsonSchemaTypeAttribute>() is { } jsonSchemaType)
        {
            ThrowIfSelfReferencing(formatType, jsonSchemaType.Type);
            return RepresentAs(jsonSchemaType.Type, type, context);
        }

        // Handle concept types - redirect to the underlying primitive type's schema
        if (type.IsConcept())
        {
            return RepresentAs(type.GetConceptValueType(), type, context);
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

                // The element concept's own compliance metadata has to be carried onto the item schema. This
                // branch bypasses the scalar-concept path above, so without it a [PII] concept loses its
                // classification the moment it is put in a list — and a value that is encrypted as a scalar
                // would be persisted in the clear as a list element.
                if (itemSchema is JsonObject itemSchemaObject && _metadataResolver.HasMetadataFor(elementType))
                {
                    AddComplianceMetadata(itemSchemaObject, _metadataResolver.GetMetadataFor(elementType));
                }

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
            var format = _typeFormats.GetFormatForType(formatType);

            // Preserve the nullable marker for known value types (e.g. DateTimeOffset?, int?). STJ's
            // JsonSchemaExporter does not carry the Nullable<T>/NRT marker into the format, so a nullable
            // scalar would otherwise share its non-nullable form's format — making IsNullable() false and
            // GetDefaultValue() synthesize a type-default sentinel (e.g. 0001-01-01 for DateTimeOffset) for an
            // unset optional at read time. Appending '?' makes IsNullable() return true so the value
            // materializes as null/absent instead. Symmetric with the nullable-concept handling above.
            if (PropertyIsNullable(type, context) && !format.EndsWith('?'))
            {
                format += "?";
            }

            schemaObj["format"] = format;
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

    /// <summary>
    /// Produces the schema of a different type in place of the declared type's own schema.
    /// </summary>
    /// <param name="representedAs">The <see cref="Type"/> whose schema describes what actually goes on the wire.</param>
    /// <param name="declaredType">The declared <see cref="Type"/> being represented.</param>
    /// <param name="context">The <see cref="JsonSchemaExporterContext"/> of the node being transformed.</param>
    /// <returns>The schema node for <paramref name="representedAs"/>, carrying the declared type's metadata.</returns>
    /// <remarks>
    /// The declared type's compliance metadata has to travel onto the substituted schema — the classification
    /// belongs to the value, not to the shape it happens to serialize as, and losing it here would persist a
    /// <c>[PII]</c> value in the clear.
    /// <para>
    /// The nullable marker has to be re-applied for the same reason: System.Text.Json's schema exporter does not
    /// propagate NRT nullable markers through custom converters, so the actual property nullability is read via
    /// <see cref="NullabilityInfoContext"/>. When the property is nullable, '?' is appended to the format so that
    /// <c>IsNullable()</c> returns true and <c>GetDefaultValue()</c> returns null rather than the primitive
    /// default (e.g. 0 for ulong).
    /// </para>
    /// </remarks>
    JsonNode RepresentAs(Type representedAs, Type declaredType, JsonSchemaExporterContext context)
    {
        var representedSchema = context.TypeInfo.Options.GetJsonSchemaAsNode(representedAs, _exporterOptions);
        if (representedSchema is not JsonObject representedSchemaObject) return representedSchema;

        if (_metadataResolver.HasMetadataFor(declaredType))
        {
            AddComplianceMetadata(representedSchemaObject, _metadataResolver.GetMetadataFor(declaredType));
        }

        if (PropertyIsNullable(declaredType, context) &&
            representedSchemaObject.TryGetPropertyValue("format", out var format))
        {
            var formatValue = format!.GetValue<string>();
            if (!formatValue.EndsWith('?'))
            {
                representedSchemaObject["format"] = formatValue + "?";
            }
        }

        return representedSchema;
    }
}
