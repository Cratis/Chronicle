// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_every_block : given.a_language_service
{
    const string Definition = """
        projection Test => Model
          every
            LastUpdated = $eventContext.occurred
            EventSourceId = $eventContext.eventSourceId
            exclude children
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition, "UserReadModel");

    [Fact] void should_have_from_every() => _result.FromEvery.ShouldNotBeNull();
    [Fact] void should_have_two_properties() => _result.FromEvery.Properties.Count.ShouldEqual(2);
    [Fact] void should_map_last_updated() => _result.FromEvery.Properties[new PropertyPath("LastUpdated")].ShouldEqual("$eventContext(occurred)");
    [Fact] void should_map_event_source_id() => _result.FromEvery.Properties[new PropertyPath("EventSourceId")].ShouldEqual("$eventContext(eventSourceId)");
    [Fact] void should_exclude_children() => _result.FromEvery.IncludeChildren.ShouldBeFalse();
}
