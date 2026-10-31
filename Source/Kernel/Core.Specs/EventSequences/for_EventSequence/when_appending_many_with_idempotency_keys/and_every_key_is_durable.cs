// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_every_key_is_durable : given.an_idempotent_batch
{
    void Establish()
    {
        DurableAt(0, 7);
        DurableAt(1, 3);
        DurableAt(2, 5);
        CheckedScopes();
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_succeed() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_request_order() => _batchResult.Result.SequenceNumbers.ShouldEqual([7, 3, 5]);
    [Fact] void should_report_each_hit() => _batchResult.Outcomes.ShouldEqual([AppendOutcome.AlreadyAppended, AppendOutcome.AlreadyAppended, AppendOutcome.AlreadyAppended]);
    [Fact] void should_not_check_concurrency() => _batchResult.Result.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] void should_use_one_lookup() => _lookups.ShouldEqual(1);
    [Fact] async Task should_not_write() => await ShouldNotWrite();
    [Fact] async Task should_not_enqueue() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
