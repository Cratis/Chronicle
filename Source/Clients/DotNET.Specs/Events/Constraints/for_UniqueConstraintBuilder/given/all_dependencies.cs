// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.given;

public class all_dependencies : Specification
{
    protected IEventTypes _eventTypes;
    protected NJsonSchemaGenerator _generator;

    void Establish()
    {
        _generator = new NJsonSchemaGenerator(new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        });

        _eventTypes = Substitute.For<IEventTypes>();
    }
}
