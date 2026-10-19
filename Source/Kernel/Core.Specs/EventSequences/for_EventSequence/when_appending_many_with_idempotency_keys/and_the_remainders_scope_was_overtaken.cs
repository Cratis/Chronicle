// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_the_remainders_scope_was_overtaken : given.an_idempotent_batch
{
    void Establish()
    {
        _events[1] = _events[1] with { EventSourceId = _events[0].EventSourceId };
        DurableAt(0, 4);
        _batchScopes = new(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [_events[0].EventSourceId] = new(EventSequenceNumber.First, true, null, null, null, null)
        });
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(Task.FromResult<EventSequenceNumber>(4));
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_refuse_the_remainder() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_concurrency_violation() => _batchResult.Result.ConcurrencyViolations.ShouldNotBeEmpty();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
