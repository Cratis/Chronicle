// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_live_delivery_budget_is_disabled : given.an_observer_with_a_live_delivery_batch
{
    protected override Observers CreateObserversConfig() => new() { LiveDeliveryBudget = 0, WriteCoalescingInterval = 0 };

    async Task Because() => await _observer.Handle(Partition, _events);

    [Fact] void should_deliver_the_large_batch_live() => _subscriber.Received(1).OnNext(Partition, Arg.Is<IEnumerable<AppendedEvent>>(events => events.Count() == BatchSize), Arg.Any<ObserverSubscriberContext>());
}
