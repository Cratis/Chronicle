// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Aksio.Cratis.Schemas.for_ComplianceMetadataSchemaProcessor.given;

public class a_processor_and_a_context_for<T> : Specification
    where T : new()
{
    protected Mock<IComplianceMetadataResolver> resolver;
    protected ComplianceMetadataSchemaProcessor processor;
    protected SchemaProcessorContext context;

    void Establish()
    {
        resolver = new();
        processor = new(resolver.Object);
        var settings = new JsonSchemaGeneratorSettings
        {
            SerializerSettings = new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateParseHandling = DateParseHandling.DateTimeOffset
            }
        };

        var generator = new NJsonSchemaGenerator(settings);
        var schema = generator.Generate(typeof(T));

        var instance = new T();
        context = new(
            typeof(T).ToContextualType(),
            schema,
            new JsonSchemaResolver(instance, settings),
            generator,
            settings
        );
    }
}
