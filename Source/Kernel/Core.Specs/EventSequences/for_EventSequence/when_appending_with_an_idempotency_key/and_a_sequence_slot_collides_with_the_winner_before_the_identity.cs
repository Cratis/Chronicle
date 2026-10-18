// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_a_sequence_slot_collides_with_the_winner_before_the_identity : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        _scope = new(EventSequenceNumber.BeforeFirst, true, null, null, null, null);
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(_ => _durable.Count == 0 ? EventSequenceNumber.Unavailable : EventSequenceNumber.First);
        _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(_ =>
        {
            WinnerAppears();
            return (Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>)(AppendManyStorageFailure)new DuplicateEventSequenceNumber(1UL);
        });
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_settle_after_renumber_validation_fails() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_not_report_the_losing_concurrency_check() => _result.Result.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] async Task should_only_attempt_storage_once() => await _publications.Received(1).AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
