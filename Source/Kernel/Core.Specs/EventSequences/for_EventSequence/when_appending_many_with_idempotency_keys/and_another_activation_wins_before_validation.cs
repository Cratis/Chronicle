// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_another_activation_wins_before_validation : given.an_idempotent_batch
{
    void Establish()
    {
        CheckedScopes();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>()).Returns(_ =>
        {
            DurableAt(0, 2);
            DurableAt(1, 3);
            DurableAt(2, 4);
            return Task.FromResult<EventSequenceNumber>(4);
        });
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_settle_every_key_as_done() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_the_losing_check() => _batchResult.Result.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] void should_report_only_hits() => _batchResult.Outcomes.All(outcome => outcome == AppendOutcome.AlreadyAppended).ShouldBeTrue();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
