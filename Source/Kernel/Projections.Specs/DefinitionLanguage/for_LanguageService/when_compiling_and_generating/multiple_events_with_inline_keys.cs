// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class multiple_events_with_inline_keys : given.a_language_service_with_schemas<given.TransportRouteReadModel>
{
    const string Definition = """
        projection TransportRoute => TransportRouteReadModel
          from HubRouteAddedToSimulationConfiguration
            key simulationConfigurationId
            transportTypeId = transportTypeId
            sourceHubId = sourceHubId
            destinationHubId = destinationHubId
          from WarehouseRouteAddedToSimulationConfiguration
            key simulationConfigurationId
            transportTypeId = transportTypeId
            sourceWarehouseId = sourceWarehouseId
            destinationHubId = destinationHubId
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.HubRouteAddedToSimulationConfiguration), typeof(given.WarehouseRouteAddedToSimulationConfiguration)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAddedToSimulationConfiguration").ShouldBeTrue();
    [Fact] void should_have_from_warehouse_route_added() => _result.From.ContainsKey((EventType)"WarehouseRouteAddedToSimulationConfiguration").ShouldBeTrue();
    [Fact] void should_have_hub_route_key() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Key.Value.ShouldEqual("simulationConfigurationId");
    [Fact] void should_have_warehouse_route_key() => _result.From[(EventType)"WarehouseRouteAddedToSimulationConfiguration"].Key.Value.ShouldEqual("simulationConfigurationId");
    [Fact] void should_have_source_hub_id_in_hub_route() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Properties.ContainsKey(new PropertyPath("sourceHubId")).ShouldBeTrue();
    [Fact] void should_have_source_warehouse_id_in_warehouse_route() => _result.From[(EventType)"WarehouseRouteAddedToSimulationConfiguration"].Properties.ContainsKey(new PropertyPath("sourceWarehouseId")).ShouldBeTrue();
}
