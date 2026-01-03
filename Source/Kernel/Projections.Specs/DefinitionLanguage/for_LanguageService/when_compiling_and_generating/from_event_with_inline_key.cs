// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_event_with_inline_key : given.a_language_service
{
    const string Definition = """
        projection TransportRoute => TransportRouteReadModel
          from HubRouteAddedToSimulationConfiguration key id
            simulationConfigurationId = $eventContext.eventSourceId
            transportTypeId = transportTypeId
            sourceHubId = sourceHubId
            destinationHubId = destinationHubId
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition, "TransportRouteReadModel");

    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAddedToSimulationConfiguration").ShouldBeTrue();
    [Fact] void should_have_key() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Key.ShouldNotBeNull();
    [Fact] void should_have_key_value() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Key.Value.ShouldEqual("id");
    [Fact] void should_have_id_property() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Properties.ContainsKey(new PropertyPath("id")).ShouldBeTrue();
    [Fact] void should_map_id_to_event_id() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Properties[new PropertyPath("id")].ShouldEqual("id");
    [Fact] void should_have_simulation_configuration_id_property() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Properties.ContainsKey(new PropertyPath("simulationConfigurationId")).ShouldBeTrue();
    [Fact] void should_map_simulation_configuration_id_to_event_source_id() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Properties[new PropertyPath("simulationConfigurationId")].ShouldEqual("$eventContext(eventSourceId)");
    [Fact] void should_have_transport_type_id_property() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Properties.ContainsKey(new PropertyPath("transportTypeId")).ShouldBeTrue();
}
