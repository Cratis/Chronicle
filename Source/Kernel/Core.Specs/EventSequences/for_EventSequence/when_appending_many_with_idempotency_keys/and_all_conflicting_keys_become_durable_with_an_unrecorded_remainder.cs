// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_all_conflicting_keys_become_durable_with_an_unrecorded_remainder : given.an_idempotent_batch
{
    void Establish()
    {
        CheckedScopes();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(call =>
            {
                DurableAt(0, 2);
                return Task.FromResult(call.ArgAt<EventSourceId?>(1) == _events[0].EventSourceId ? (EventSequenceNumber)2 : EventSequenceNumber.Unavailable);
            });
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_not_report_violations_for_recorded_events() => _batchResult.Result.ConcurrencyViolations.ShouldBeEmpty();
    [Fact] void should_not_report_success_for_unrecorded_events() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_unrecorded_remainder_as_an_error() => _batchResult.Result.Errors.ShouldNotBeEmpty();
    [Fact] void should_not_report_sequence_numbers() => _batchResult.Result.SequenceNumbers.ShouldBeEmpty();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
