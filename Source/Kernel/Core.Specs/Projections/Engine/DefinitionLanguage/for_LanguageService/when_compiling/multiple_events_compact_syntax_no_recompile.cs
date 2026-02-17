// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling;

public class multiple_events_compact_syntax_no_recompile : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.TransportRouteReadModel>
{
    const string Declaration = """
        projection TransportRoute => TransportRouteReadModel
          automap
          from HubRouteAddedToSimulationConfiguration key id, WarehouseRouteAddedToSimulationConfiguration key id
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.HubRouteAddedToSimulationConfiguration), typeof(for_LanguageService.given.WarehouseRouteAddedToSimulationConfiguration)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAddedToSimulationConfiguration").ShouldBeTrue();
    [Fact] void should_have_from_warehouse_route_added() => _result.From.ContainsKey((EventType)"WarehouseRouteAddedToSimulationConfiguration").ShouldBeTrue();
}
