// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class when_compiling_event_context_expressions : given.a_language_service
{
    const string definition = """
        projection Activity => ActivityReadModel
          from ActivityLogged
            key $eventSourceId
            Occurred = $eventContext.occurred
            SequenceNumber = $eventContext.sequenceNumber
            CorrelationId = $eventContext.correlationId
            EventSourceId = $eventContext.eventSourceId
        """;

    ProjectionDefinition _result;

    void Because() => _result = Compile(definition);

    [Fact] void should_have_from_activity_logged() => _result.From.ContainsKey((EventType)"ActivityLogged").ShouldBeTrue();
    [Fact] void should_have_event_source_id_key() => _result.From[(EventType)"ActivityLogged"].Key.Value.ShouldEqual("$eventSourceId");
    [Fact] void should_have_four_properties() => _result.From[(EventType)"ActivityLogged"].Properties.Count.ShouldEqual(4);
    [Fact] void should_map_occurred() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("Occurred")].ShouldEqual("$eventContext(occurred)");
    [Fact] void should_map_sequence_number() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("SequenceNumber")].ShouldEqual("$eventContext(sequenceNumber)");
    [Fact] void should_map_correlation_id() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("CorrelationId")].ShouldEqual("$eventContext(correlationId)");
    [Fact] void should_map_event_source_id() => _result.From[(EventType)"ActivityLogged"].Properties[new PropertyPath("EventSourceId")].ShouldEqual("$eventContext(eventSourceId)");
}
