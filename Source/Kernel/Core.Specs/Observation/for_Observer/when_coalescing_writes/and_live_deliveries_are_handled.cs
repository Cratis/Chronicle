// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

/// <summary>
/// Live deliveries keep the running totals on the observer state current straight away, and hold back only the
/// per-partition breakdown, which is written for all of them at once instead of once per delivery.
/// </summary>
public class and_live_deliveries_are_handled : given.an_observer_with_subscription_for_specific_event_type
{
    Key _partition;
    Key _otherPartition;
    int _incrementsBeforeFlush;
    EventCount _totalBeforeFlush;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 600 };

    void Establish()
    {
        _partition = new Key("partition", ArrayIndexers.NoIndexers);
        _otherPartition = new Key("other", ArrayIndexers.NoIndexers);
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(callInfo => ObserverSubscriberResult.Ok(callInfo.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber));
        _observerHandledCountsStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 0UL)]);
        await _observer.Handle(_otherPartition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 1UL)]);
        await _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 2UL)]);

        _incrementsBeforeFlush = _observerHandledCountsStorage.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_observerHandledCountsStorage.Increment));
        _totalBeforeFlush = _stateStorage.State.HandledEventCount;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_write_the_per_partition_counts_per_delivery() => _incrementsBeforeFlush.ShouldEqual(0);
    [Fact] void should_have_the_running_total_on_the_state_straight_away() => _totalBeforeFlush.ShouldEqual((EventCount)3UL);
    [Fact] void should_write_the_per_partition_counts_once_for_all_deliveries() => _observerHandledCountsStorage.Received(1).Increment(
        _observerId,
        Arg.Is<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>(counts =>
            counts.Count == 2 &&
            counts.Single(_ => _.Key == _partition).Value[event_type.Id] == 2UL &&
            counts.Single(_ => _.Key == _otherPartition).Value[event_type.Id] == 1UL));
    [Fact] void should_not_add_the_flushed_counts_to_the_running_total_again() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)3UL);
}
