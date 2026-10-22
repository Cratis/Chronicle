// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_flushing_handled_counts_fails : given.an_observer_coalescing_writes
{
    bool _completedBeforeRetry;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _observer.ReportHandledEvents("partition", new Dictionary<EventTypeId, EventCount> { ["event"] = 3UL }, JobStepId.New(), 3UL);
        _observerHandledCountsStorage.Increment(_observerId, Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>())
            .Returns(Task.FromException(new InvalidOperationException("storage unavailable")), Task.CompletedTask);
    }

    async Task Because()
    {
        await _observer.Replayed(3UL);
        _completedBeforeRetry = !_stateStorage.State.IsReplaying;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_complete_replay_before_retrying_the_counts() => _completedBeforeRetry.ShouldBeTrue();
    [Fact] void should_keep_the_replay_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_retry_the_pending_counts() => _observerHandledCountsStorage.Received(2).Increment(_observerId, (Key)"partition", Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(counts => counts[(EventTypeId)"event"] == (EventCount)3UL));
    [Fact] void should_eventually_record_the_counts_once() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)3UL);
}
