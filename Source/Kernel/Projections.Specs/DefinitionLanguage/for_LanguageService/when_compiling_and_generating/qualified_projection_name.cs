// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class qualified_projection_name : given.a_language_service
{
    const string definition = """
        projection Core.Simulations.Simulation => Simulation
          from SimulationAdded
            key $eventSourceId
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(definition, "Simulation");

    [Fact] void should_have_simulation_added_event() => _result.From.ContainsKey((EventType)"SimulationAdded").ShouldBeTrue();
    [Fact] void should_have_event_source_id_key() => _result.From[(EventType)"SimulationAdded"].Key.Value.ShouldEqual("$eventSourceId");
}
