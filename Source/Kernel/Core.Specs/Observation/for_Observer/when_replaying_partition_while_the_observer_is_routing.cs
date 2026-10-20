// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A partition is replayed - which subtracts its handled counts from the totals - while routing awaits. Routing returns
/// the state it was handed before the subtraction, which must not give the partition's counts back (#4972).
/// </summary>
public class when_replaying_partition_while_the_observer_is_routing : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "partition";

    TaskCompletionSource<EventSequenceNumber> _tail;

    async Task Establish()
    {
        _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.GetTailSequenceNumber().Returns(_ => _tail.Task);
        _observerHandledCountsStorage
            .GetFor(_observerId, _partition)
            .Returns(new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 40UL });
        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 100UL }, JobStepId.New(), 1UL);
    }

    async Task Because()
    {
        var routing = _observer.TransitionTo<Routing>();

        await _observer.ReplayPartition(_partition);

        _tail.SetResult(EventSequenceNumber.Unavailable);
        await routing;
    }

    [Fact] void should_keep_the_subtraction() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)60UL);
    [Fact] void should_keep_the_subtraction_for_the_event_type() => _stateStorage.State.HandledEventCountPerEventType[event_type.Id].ShouldEqual((EventCount)60UL);
}
