// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class event_context_expressions : given.a_language_service_with_schemas<given.ActivityReadModel>
{
    const string Declaration = """
        projection Activity => ActivityReadModel
          from ActivityLogged
            key $eventSourceId
            occurred = $eventContext.occurred
            sequenceNumber = $eventContext.sequenceNumber
            correlationId = $eventContext.correlationId
            eventSourceId = $eventContext.eventSourceId
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.ActivityLogged)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_activity_logged() => _result.From.ContainsKey((EventType)"ActivityLogged").ShouldBeTrue();
    [Fact] void should_not_have_key() => _result.From[(EventType)"ActivityLogged"].Key.IsSet().ShouldBeFalse();
    [Fact] void should_have_four_properties() => _result.From[(EventType)"ActivityLogged"].Properties.Count.ShouldEqual(4);
    [Fact] void should_map_occurred() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("occurred")].ShouldEqual($"{WellKnownExpressions.EventContext}(occurred)");
    [Fact] void should_map_sequence_number() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("sequenceNumber")].ShouldEqual($"{WellKnownExpressions.EventContext}(sequenceNumber)");
    [Fact] void should_map_correlation_id() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("correlationId")].ShouldEqual($"{WellKnownExpressions.EventContext}(correlationId)");
    [Fact] void should_map_event_source_id() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("eventSourceId")].ShouldEqual($"{WellKnownExpressions.EventContext}(eventSourceId)");
}
