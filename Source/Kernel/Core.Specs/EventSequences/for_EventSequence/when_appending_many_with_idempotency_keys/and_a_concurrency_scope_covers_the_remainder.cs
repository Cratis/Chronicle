// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_a_concurrency_scope_covers_the_remainder : given.an_idempotent_batch
{
    void Establish()
    {
        DurableAt(0, 4);
        CheckedScopes();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(call => Task.FromResult<EventSequenceNumber>(call.ArgAt<EventSourceId?>(1) == _events[0].EventSourceId ? 4 : EventSequenceNumber.Unavailable));
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_ignore_the_already_done_sources_scope() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_checking_the_remainder() => _batchResult.Result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_write_only_the_remainder() => _written.Count.ShouldEqual(2);
}
