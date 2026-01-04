// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_event_with_inline_key : given.a_language_service_with_schemas<given.TransportRouteReadModel>
{
    const string Definition = """
        projection TransportRoute => TransportRouteReadModel
          from HubRouteAddedToSimulationConfiguration
            key simulationConfigurationId
            transportTypeId = transportTypeId
            sourceHubId = sourceHubId
            destinationHubId = destinationHubId
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.HubRouteAddedToSimulationConfiguration)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAddedToSimulationConfiguration").ShouldBeTrue();
    [Fact] void should_have_key() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Key.ShouldNotBeNull();
    [Fact] void should_have_key_value() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Key.Value.ShouldEqual("simulationConfigurationId");
    [Fact] void should_have_transport_type_id_property() => _result.From[(EventType)"HubRouteAddedToSimulationConfiguration"].Properties.ContainsKey(new PropertyPath("transportTypeId")).ShouldBeTrue();
}
