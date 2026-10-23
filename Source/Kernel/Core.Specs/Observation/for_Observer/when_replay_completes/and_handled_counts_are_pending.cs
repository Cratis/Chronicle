// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_handled_counts_are_pending : given.an_observer_coalescing_writes
{
    async Task Because()
    {
        await _observer.ReportHandledEvents("partition", new Dictionary<EventTypeId, EventCount> { ["event"] = 3UL }, JobStepId.New(), 3UL);
        await _observer.Replayed(3UL);
    }

    [Fact] void should_expose_the_counts_when_replay_completes() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)3UL);
}
