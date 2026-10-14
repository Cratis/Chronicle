// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_cancelled_read_throws_a_provider_error : given.a_replay
{
    CancellationTokenSource _budget;

    void Establish()
    {
        Event(0, "source", "Claimed", "value");
        _budget = new();
        _sequence.Configure().GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await _budget.CancelAsync();
            throw new IOException("provider cancelled the read");
        });
        Start();
    }

    async Task Because() => _result = await _replay.Continue(cancellationToken: _budget.Token);

    void Destroy() => _budget.Dispose();

    [Fact] void should_pause_without_failure() => _result.Failed.ShouldBeEmpty();
    [Fact] void should_not_report_completion() => _result.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_write_partial_state() => _writes.ShouldBeEmpty();
}
