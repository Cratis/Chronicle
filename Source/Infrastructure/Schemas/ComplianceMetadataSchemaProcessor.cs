// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;
using Cratis.Collections;
using Cratis.Strings;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ISchemaProcessor"/> for handling compliance metadata.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ComplianceMetadataSchemaProcessor"/> class.
/// </remarks>
/// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
public class ComplianceMetadataSchemaProcessor(IComplianceMetadataResolver metadataResolver) : ISchemaProcessor
{
    /// <inheritdoc/>
    public void Process(SchemaProcessorContext context)
    {
        if (metadataResolver.HasMetadataFor(context.ContextualType.Type))
        {
            AddMetadataToSchema(context.Schema, metadataResolver.GetMetadataFor(context.ContextualType.Type));
        }

        foreach (var (key, property) in context.Schema.Properties)
        {
            var propertyName = key.ToPascalCase();
            var clrProperty = context.ContextualType.Type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (clrProperty != default && metadataResolver.HasMetadataFor(clrProperty))
            {
                AddMetadataToSchema(property, metadataResolver.GetMetadataFor(clrProperty));
            }
        }
    }

    void AddMetadataToSchema(JsonSchema schema, IEnumerable<ComplianceMetadata> metadata) => metadata.ForEach(_ => EnsureMetadata(schema).Add(new ComplianceSchemaMetadata(_.MetadataType.Value, _.Details)));

    List<ComplianceSchemaMetadata> EnsureMetadata(JsonSchema schema)
    {
        EnsureExtensionData(schema);

        if (!schema.ExtensionData!.TryGetValue(JsonSchemaGenerator.ComplianceKey, out var value))
        {
            value = new List<ComplianceSchemaMetadata>();
            schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] = value;
        }

        return (value as List<ComplianceSchemaMetadata>)!;
    }

    void EnsureExtensionData(JsonSchema schema)
    {
        schema.ExtensionData ??= new Dictionary<string, object?>();
    }
}
