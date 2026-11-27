// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_live_notifications_arrive_under_the_repair_barrier : given.an_observer_behind_a_repair_barrier
{
    async Task Because()
    {
        await _observer.Handle("P", [Event(14UL), Event(15UL)]);
        await _observer.Handle("Q", [Event(16UL)]);
        await _observer.HandlePage([new ObserverPartitionBatch((Key)"P", [Event(14UL)])]);
    }

    [Fact] void should_not_deliver_live_events() => _delivered.Count.ShouldEqual(0);
    [Fact] void should_not_advance_the_covered_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_not_queue_held_back_deliveries() => CountEntries(_observer, "_heldBackDeliveries", typeof(Observer)).ShouldEqual(0);
    [Fact] void should_not_queue_missed_deliveries() => CountEntries(_observer, "_missedDeliveries", typeof(Observer)).ShouldEqual(0);
    [Fact] void should_not_queue_skip_tracker_entries() => CountEntries(typeof(Observer).GetField("_skipDetector", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_observer), "_pending", typeof(EventSkipDetector)).ShouldEqual(0);

    static int CountEntries(object? target, string name, Type type) => target is null ? 0 : ((IDictionary)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!).Count;
}
