// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_every_block : given.a_language_service_with_schemas<given.Model>
{
    const string Definition = """
        projection Test => Model
          every
            lastUpdated = $eventContext.occurred
            eventSourceId = $eventContext.eventSourceId
            exclude children
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.ActivityLogged)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_every() => _result.FromEvery.ShouldNotBeNull();
    [Fact] void should_have_two_properties() => _result.FromEvery.Properties.Count.ShouldEqual(2);
    [Fact] void should_map_last_updated() => _result.FromEvery.Properties[new PropertyPath("lastUpdated")].ShouldEqual($"{WellKnownExpressions.EventContext}(occurred)");
    [Fact] void should_map_event_source_id() => _result.FromEvery.Properties[new PropertyPath("eventSourceId")].ShouldEqual($"{WellKnownExpressions.EventContext}(eventSourceId)");
    [Fact] void should_exclude_children() => _result.FromEvery.IncludeChildren.ShouldBeFalse();
}
