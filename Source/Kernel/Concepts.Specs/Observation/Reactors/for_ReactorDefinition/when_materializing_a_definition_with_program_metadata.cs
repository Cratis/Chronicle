// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Observation.Reactors.for_ReactorDefinition;

public class when_materializing_a_definition_with_program_metadata : Specification
{
    ReactorDefinition _definition;

    void Because() => _definition = JsonSerializer.Deserialize<ReactorDefinition>(
        """
        { "Identifier": "reactor", "EventSequenceId": "event-log", "EventTypes": [], "IsReplayable": true, "StartAtTail": false, "MaximumBatchSize": 100, "ProgramIdentifier": "orders" }
        """,
        new JsonSerializerOptions { Converters = { new ConceptAsJsonConverterFactory() } })!;

    [Fact] void should_retain_the_program() => _definition.ProgramIdentifier.ShouldEqual(new ProgramIdentifier("orders"));
}
