// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_redacting;

public class and_a_queued_redaction_targets_a_user_event_store : given.a_reactor
{
    async Task Because()
    {
        await _reactor.RedactionRequested(new EventRedactionRequested(Concepts.EventSequences.WellKnownEventSequences.EventLog, 1UL, "some reason"), ContextFor("user-store"));
    }

    [Fact] void should_execute_through_the_event_sequence() => _sequence.ReceivedCalls().ShouldNotBeEmpty();
}
