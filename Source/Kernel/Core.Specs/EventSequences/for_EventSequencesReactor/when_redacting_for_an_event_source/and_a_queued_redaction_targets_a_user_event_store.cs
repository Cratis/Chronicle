// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_redacting_for_an_event_source;

public class and_a_queued_redaction_targets_a_user_event_store : given.a_reactor
{
    async Task Because()
    {
        await _reactor.RedactedForEventSource(new EventsRedactedForEventSource(Concepts.EventSequences.WellKnownEventSequences.EventLog, "some-source", [EventType], "some reason"), ContextFor("user-store"));
    }

    [Fact] void should_execute_through_the_event_sequence() => _sequence.ReceivedCalls().ShouldNotBeEmpty();
}
