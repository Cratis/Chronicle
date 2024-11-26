// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Schemas.for_ComplianceMetadataSchemaProcessor.given;

public class a_processor_and_a_context_for<T> : Specification
    where T : new()
{
    protected IComplianceMetadataResolver _resolver;
    protected ComplianceMetadataSchemaProcessor _processor;
    protected SchemaProcessorContext _context;

    void Establish()
    {
        _resolver = Substitute.For<IComplianceMetadataResolver>();
        _processor = new(_resolver);
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };

        var generator = new NJsonSchemaGenerator(settings);
        var schema = generator.Generate(typeof(T));

        var instance = new T();
        _context = new(
            typeof(T).ToContextualType(),
            schema,
            new JsonSchemaResolver(instance, settings),
            generator,
            settings
        );
    }
}
