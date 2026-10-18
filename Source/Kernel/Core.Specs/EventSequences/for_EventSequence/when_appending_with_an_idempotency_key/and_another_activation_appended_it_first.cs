// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_another_activation_appended_it_first : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(_ =>
    {
        WinnerAppears();
        return (Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>)(AppendManyStorageFailure)new IdentitiesAlreadyDurable(_durable);
    });
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_settle_as_a_hit() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] async Task should_not_enqueue_the_winners_event() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] async Task should_only_attempt_storage_once() => await _publications.Received(1).AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
