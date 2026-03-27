// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IJsonSchemaGenerator"/>.
/// </summary>
[Singleton]
public class JsonSchemaGenerator : IJsonSchemaGenerator
{
    readonly JsonSerializerOptions _serializerOptions;
    readonly JsonSchemaExporterOptions _exporterOptions;
    readonly IComplianceMetadataResolver _metadataResolver;
    readonly TypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.
    /// </summary>
    /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> to use for converting names during serialization.</param>
    public JsonSchemaGenerator(IComplianceMetadataResolver metadataResolver, INamingPolicy namingPolicy)
    {
        _metadataResolver = metadataResolver;
        _typeFormats = new TypeFormats();

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = namingPolicy.JsonPropertyNamingPolicy,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
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
            if (IsNullableConceptProperty(context) &&
                conceptSchema is JsonObject conceptSchemaObj &&
                conceptSchemaObj.TryGetPropertyValue("format", out var format))
            {
                var formatStr = format!.GetValue<string>();
                if (!formatStr.EndsWith('?'))
                {
                    conceptSchemaObj["format"] = formatStr + "?";
                }
            }

            return conceptSchema;
        }

        if (schema is not JsonObject schemaObj) return schema;

        // Add format for known types
        if (_typeFormats.IsKnown(type))
        {
            schemaObj["format"] = _typeFormats.GetFormatForType(type);
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

        // Add compensation metadata — only applies to top-level type schema (no property context)
        if (context.PropertyInfo is null)
        {
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
