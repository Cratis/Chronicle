// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_only_some_conflicting_keys_become_durable : given.an_idempotent_batch
{
    void Establish()
    {
        CheckedScopes();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(_ =>
            {
                DurableAt(0, 2);
                return Task.FromResult<EventSequenceNumber>(4);
            });
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_refuse_the_unrecorded_remainder() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_only_the_remaining_violations() => _batchResult.Result.ConcurrencyViolations.Select(violation => violation.EventSourceId).ShouldEqual(_events.Skip(1).Select(@event => @event.EventSourceId));
    [Fact] void should_not_report_an_append_error() => _batchResult.Result.Errors.ShouldBeEmpty();
    [Fact] void should_report_the_performed_check() => _batchResult.Result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
