// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_winner_appears_during_validation : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        _scope = new(EventSequenceNumber.BeforeFirst, true, null, null, null, null);
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(_ =>
            {
                WinnerAppears();
                return EventSequenceNumber.First;
            });
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_settle_again_before_refusing() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_not_report_a_losing_concurrency_check() => _result.Result.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
