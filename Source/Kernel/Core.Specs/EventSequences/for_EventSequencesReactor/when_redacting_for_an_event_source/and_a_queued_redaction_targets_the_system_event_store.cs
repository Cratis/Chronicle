// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_redacting_for_an_event_source;

public class and_a_queued_redaction_targets_the_system_event_store : given.a_reactor
{
    async Task Because()
    {
        await _reactor.RedactedForEventSource(new EventsRedactedForEventSource(Concepts.EventSequences.WellKnownEventSequences.EventLog, "some-source", [EventType], "some reason"), ContextFor("System"));
        await _reactor.RedactedForEventSource(new EventsRedactedForEventSource(Concepts.EventSequences.WellKnownEventSequences.EventLog, "some-source", [EventType], "some reason"), ContextFor("system"));
        await _reactor.RedactedForEventSource(new EventsRedactedForEventSource(Concepts.EventSequences.WellKnownEventSequences.EventLog, "some-source", [EventType], "some reason"), ContextFor("SYSTEM"));
    }

    [Fact] void should_not_get_any_event_sequence_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_look_up_any_event_store() => _storage.ReceivedCalls().ShouldBeEmpty();
}
