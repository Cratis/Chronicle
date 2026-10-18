// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_it_is_cancelled : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    readonly CancellationTokenSource _cancel = new();

    void Establish()
    {
        _cancellationToken = _cancel.Token;
        _uniqueStorage.Save(Arg.Any<EventSourceId>(), _unique, EventSequenceNumber.First, Arg.Any<UniqueConstraintValue>(), Arg.Any<string>()).Returns(_ =>
            _cancel.CancelAsync());
    }

    async Task Because() => await Perform();
    void Destroy() => _cancel.Dispose();

    [Fact] void should_fail_the_cancelled_step() => _result.TryGetException(out _).ShouldBeTrue();
    [Fact] async Task should_report_confirmed_progress_despite_cancellation() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Any<IEnumerable<ConstraintName>>(), new ConstraintIndexRebuildOutcome(true, 0));
}
