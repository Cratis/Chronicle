// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_revising_queued;

public class and_a_queued_revision_targets_the_system_event_store : given.a_reactor
{
    async Task Because()
    {
        await _reactor.Revised(new EventRevised(Concepts.EventSequences.WellKnownEventSequences.EventLog, 1UL, EventType, "{}"), ContextFor("System"));
        await _reactor.Revised(new EventRevised(Concepts.EventSequences.WellKnownEventSequences.EventLog, 1UL, EventType, "{}"), ContextFor("system"));
        await _reactor.Revised(new EventRevised(Concepts.EventSequences.WellKnownEventSequences.EventLog, 1UL, EventType, "{}"), ContextFor("SYSTEM"));
    }

    [Fact] void should_not_get_any_event_sequence_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_look_up_any_event_store() => _storage.ReceivedCalls().ShouldBeEmpty();
}
