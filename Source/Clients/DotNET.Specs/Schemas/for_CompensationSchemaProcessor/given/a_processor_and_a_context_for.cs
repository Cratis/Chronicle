// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Namotion.Reflection;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Schemas.for_CompensationSchemaProcessor.given;

public class a_processor_and_a_context_for<T> : Specification
    where T : new()
{
    protected CompensationSchemaProcessor _processor;
    protected SchemaProcessorContext _context;

    void Establish()
    {
        _processor = new();

        var settings = new SystemTextJsonSchemaGeneratorSettings();
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
