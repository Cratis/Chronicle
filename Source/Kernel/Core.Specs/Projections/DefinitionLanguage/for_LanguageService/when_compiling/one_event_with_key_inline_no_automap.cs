// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class one_event_with_key_inline_no_automap : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.TransportRouteReadModel>
{
    const string Declaration = """
        projection TransportRoute => TransportRouteReadModel
          from HubRouteAdded key id
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.HubRouteAdded)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_hub_route_added() => _result.From.ContainsKey((EventType)"HubRouteAdded").ShouldBeTrue();
    [Fact] void should_have_key() => _result.From[(EventType)"HubRouteAdded"].Key.Value.ShouldEqual("id");
}
