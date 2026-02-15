// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_with_default_automap : given.a_language_service_with_schemas<given.TransportRouteReadModel>
{
    const string Declaration = """
        projection TransportRoute => TransportRouteReadModel
            from HubRouteAdded key id
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.HubRouteAdded)];

    given.CompilerResult _compilerResult;

    void Because() => _compilerResult = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_hub_route_added() => _compilerResult.Definition.From.ContainsKey((EventType)"HubRouteAdded").ShouldBeTrue();
    [Fact] void should_have_automap_enabled() => _compilerResult.Definition.AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_not_have_any_property_mappings() => _compilerResult.Definition.From[(EventType)"HubRouteAdded"].Properties.Count.ShouldEqual(0);
    [Fact] void should_not_generate_id_mapping() => _compilerResult.GeneratedDefinition.ShouldNotContain("id = id");
    [Fact] void should_not_generate_simulation_configuration_id_mapping() => _compilerResult.GeneratedDefinition.ShouldNotContain("simulationConfigurationId = simulationConfigurationId");
    [Fact] void should_not_generate_transport_type_id_mapping() => _compilerResult.GeneratedDefinition.ShouldNotContain("transportTypeId = transportTypeId");
    [Fact] void should_not_generate_source_hub_id_mapping() => _compilerResult.GeneratedDefinition.ShouldNotContain("sourceHubId = sourceHubId");
    [Fact] void should_not_generate_destination_hub_id_mapping() => _compilerResult.GeneratedDefinition.ShouldNotContain("destinationHubId = destinationHubId");
}

