// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Schemas;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.given;

public class an_expando_object_converter_with_nested_children_schema : Specification
{
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;

    void Establish()
    {
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }
        };
        var typeFormats = new TypeFormats();
        settings.SchemaProcessors.Add(new TypeFormatSchemaProcessor(typeFormats));
        var generator = new NJsonSchemaGenerator(settings);
        schema = generator.Generate(typeof(ParentWithNestedChildren));
        converter = new(typeFormats);
    }
}
