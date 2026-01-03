// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class multiple_events_compact_syntax : given.a_language_service
{
    const string Definition = """
        projection TransportRoute => TransportRouteReadModel
          automap
          from HubRouteAddedToSimulationConfiguration key id, WarehouseRouteAddedToSimulationConfiguration key id
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition, "TransportRouteReadModel");

    [Fact] void should_have_automap_at_projection_level() => _result.FromEvery.AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAddedToSimulationConfiguration").ShouldBeTrue();
    [Fact] void should_have_from_warehouse_route_added() => _result.From.ContainsKey((EventType)"WarehouseRouteAddedToSimulationConfiguration").ShouldBeTrue();
    [Fact] void should_have_hub_route_key() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Key.Value.ShouldEqual("id");
    [Fact] void should_have_warehouse_route_key() => _result.From[(EventType)"WarehouseRouteAddedToSimulationConfiguration"].Key.Value.ShouldEqual("id");
}
