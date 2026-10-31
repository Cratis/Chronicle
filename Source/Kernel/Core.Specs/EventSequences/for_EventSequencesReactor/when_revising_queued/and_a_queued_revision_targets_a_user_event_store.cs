// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_revising_queued;

public class and_a_queued_revision_targets_a_user_event_store : given.a_reactor
{
    async Task Because()
    {
        await _reactor.Revised(new EventRevised(Concepts.EventSequences.WellKnownEventSequences.EventLog, 1UL, EventType, "{}"), ContextFor("user-store"));
    }

    [Fact] void should_execute_through_the_event_sequence() => _sequence.ReceivedCalls().ShouldNotBeEmpty();
}
