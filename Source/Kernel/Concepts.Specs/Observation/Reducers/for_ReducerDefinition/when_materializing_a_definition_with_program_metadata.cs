// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Observation.Reducers.for_ReducerDefinition;

public class when_materializing_a_definition_with_program_metadata : Specification
{
    static readonly JsonSerializerOptions _options = new() { Converters = { new ConceptAsJsonConverterFactory() } };

    ReducerDefinition _definition;

    void Because() => _definition = JsonSerializer.Deserialize<ReducerDefinition>(
        """
        { "Identifier": "reducer", "EventSequenceId": "event-log", "EventTypes": [], "ReadModel": "model", "ProgramIdentifier": "orders" }
        """,
        _options)!;

    [Fact] void should_retain_the_program() => _definition.ProgramIdentifier.ShouldEqual(new ProgramIdentifier("orders"));
}
