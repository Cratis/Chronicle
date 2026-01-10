// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class qualified_projection_name : given.a_language_service_with_schemas<given.Simulation>
{
    const string Definition = """
        projection Core.Simulations.Simulation => Simulation
          from SimulationAdded
            key $eventSourceId
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.SimulationAdded)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_simulation_added_event() => _result.From.ContainsKey((EventType)"SimulationAdded").ShouldBeTrue();
    [Fact] void should_not_have_key() => _result.From[(EventType)"SimulationAdded"].Key.IsSet().ShouldBeFalse();
}
