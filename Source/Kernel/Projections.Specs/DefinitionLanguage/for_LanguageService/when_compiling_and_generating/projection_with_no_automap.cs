// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using EventTypes = Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_with_no_automap : given.a_language_service_with_schemas<given.TransportRouteReadModel>
{
    const string Definition = """
        projection TransportRoute => TransportRouteReadModel
            from HubRouteAdded key id
                no automap
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(EventTypes.HubRouteAdded)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAdded").ShouldBeTrue();
    [Fact] void should_have_automap_disabled() => _result.From[(EventType)"HubRouteAdded"].AutoMap.ShouldEqual(AutoMap.Disabled);
}
