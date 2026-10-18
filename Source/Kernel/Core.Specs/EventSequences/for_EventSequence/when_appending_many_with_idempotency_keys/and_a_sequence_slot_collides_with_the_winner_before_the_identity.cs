// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_a_sequence_slot_collides_with_the_winner_before_the_identity : given.an_idempotent_batch
{
    void Establish()
    {
        CheckedScopes();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(_ => Task.FromResult(_durable.Count == 0 ? EventSequenceNumber.Unavailable : (EventSequenceNumber)4));
        _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(_ =>
        {
            DurableAt(0, 2);
            DurableAt(1, 3);
            DurableAt(2, 4);
            return (Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>)(AppendManyStorageFailure)new DuplicateEventSequenceNumber(5UL);
        });
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_settle_after_renumber_validation() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_the_losing_check() => _batchResult.Result.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] async Task should_only_attempt_storage_once() => await _publications.Received(1).AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
