// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Compliance;
using Cratis.Strings;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Cratis.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemaProcessor"/> for handling compliance metadata.
    /// </summary>
    public class ComplianceMetadataSchemaProcessor : ISchemaProcessor
    {
        const string ComplianceKey = "compliance";
        readonly IComplianceMetadataResolver _metadataResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplianceMetadataSchemaProcessor"/> class.
        /// </summary>
        /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
        public ComplianceMetadataSchemaProcessor(IComplianceMetadataResolver metadataResolver)
        {
            _metadataResolver = metadataResolver;
        }

        /// <inheritdoc/>
        public void Process(SchemaProcessorContext context)
        {
            if (_metadataResolver.HasMetadataFor(context.Type))
            {
                AddMetadataToSchema(context.Schema, _metadataResolver.GetMetadataFor(context.Type));
            }

            foreach (var (key, property) in context.Schema.Properties)
            {
                var propertyName = key.ToPascalCase();
                var clrProperty = context.Type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (clrProperty != default && _metadataResolver.HasMetadataFor(clrProperty))
                {
                    AddMetadataToSchema(property, _metadataResolver.GetMetadataFor(clrProperty));
                }
            }
        }

        void AddMetadataToSchema(JsonSchema schema, ComplianceMetadata metadata) => EnsureMetadata(schema).Add(new { type = metadata.Type.Value, details = metadata.Details });

        List<object> EnsureMetadata(JsonSchema schema)
        {
            EnsureExtensionData(schema);

            if (!schema.ExtensionData.ContainsKey(ComplianceKey))
            {
                schema.ExtensionData[ComplianceKey] = new List<object>();
            }

            return (schema.ExtensionData[ComplianceKey] as List<object>)!;
        }

        void EnsureExtensionData(JsonSchema schema)
        {
            if (schema.ExtensionData == null) schema.ExtensionData = new Dictionary<string, object>();
        }
    }
}
