// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_redacting_for_an_event_source;

public class and_a_queued_request_targets_a_key_separator_bearing_target : given.a_reactor
{
    async Task Because()
    {
        await _reactor.RedactedForEventSource(Req("event-log"), ContextFor("x#System"));
        await _reactor.RedactedForEventSource(Req("event-log"), ContextFor("user-store", "Default#x"));
        await _reactor.RedactedForEventSource(Req("event-log#System#Default"), ContextFor("user-store"));
    }

    static EventsRedactedForEventSource Req(string sequence) => new(sequence, "some-source", [EventType], "some reason");

    [Fact] void should_not_get_any_event_sequence_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_look_up_any_event_store() => _storage.ReceivedCalls().ShouldBeEmpty();
}
