// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Execution;
using Aksio.Strings;
using Cratis.Compliance;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IJsonSchemaGenerator"/>.
/// </summary>
[Singleton]
public class JsonSchemaGenerator : IJsonSchemaGenerator
{
    /// <summary>
    /// The key of the compliance extension data.
    /// </summary>
    public const string ComplianceKey = "compliance";

    readonly NJsonSchemaGenerator _generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.
    /// </summary>
    /// <param name="metadataResolver"><see cref="IComplianceMetadataResolver"/> for resolving metadata.</param>
    public JsonSchemaGenerator(IComplianceMetadataResolver metadataResolver)
    {
        var settings = new JsonSchemaGeneratorSettings
        {
            AllowReferencesWithProperties = true,
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
            ReflectionService = new ReflectionService(),
        };
        settings.SchemaProcessors.Add(new ComplianceMetadataSchemaProcessor(metadataResolver));
        settings.SchemaProcessors.Add(new TypeFormatSchemaProcessor(new TypeFormats()));
        _generator = new NJsonSchemaGenerator(settings);
    }

    /// <inheritdoc/>
    public JsonSchema Generate(Type type)
    {
        var schema = _generator.Generate(type);

        // Note: NJsonSchema will ignore the camel case instruction when a complex type with properties implements IEnumerable
        // All the properties within it will then just be as original.
        ForceSchemaToBeCamelCase(schema);
        return schema;
    }

    void ForceSchemaToBeCamelCase(JsonSchema schema)
    {
        var properties = schema.Properties.ToDictionary(kvp => kvp.Key.ToCamelCase(), kvp => kvp.Value);
        schema.Properties.Clear();
        foreach (var kvp in properties)
        {
            schema.Properties.Add(kvp);
        }

        foreach (var allOfSchema in schema.AllOf)
        {
            ForceSchemaToBeCamelCase(allOfSchema);
        }

        if (schema.HasReference)
        {
            ForceSchemaToBeCamelCase(schema.Reference);
        }
    }
}
