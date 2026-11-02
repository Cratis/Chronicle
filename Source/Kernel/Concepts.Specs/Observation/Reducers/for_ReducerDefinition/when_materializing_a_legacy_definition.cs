// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Observation.Reducers.for_ReducerDefinition;

public class when_materializing_a_legacy_definition : Specification
{
    ReducerDefinition _definition;

    void Because() => _definition = JsonSerializer.Deserialize<ReducerDefinition>(
        """
        { "Identifier": "reducer", "EventSequenceId": "event-log", "EventTypes": [], "ReadModel": "model" }
        """,
        new JsonSerializerOptions { Converters = { new ConceptAsJsonConverterFactory() } })!;

    [Fact] void should_default_to_an_unknown_program() => _definition.ProgramIdentifier.ShouldEqual(ProgramIdentifier.Unknown);
}
