// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Schemas;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.for_ExpandoObjectConverter.given;

public class an_expando_object_converter : Specification
{
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;

    void Establish()
    {
        var settings = new JsonSchemaGeneratorSettings
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
            ReflectionService = new ReflectionService(),
        };
        var typeFormats = new TypeFormats();
        settings.SchemaProcessors.Add(new TypeFormatSchemaProcessor(typeFormats));
        var generator = new NJsonSchemaGenerator(settings);
        schema = generator.Generate(typeof(TargetType));
        converter = new(typeFormats);
    }
}
