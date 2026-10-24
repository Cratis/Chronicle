// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_the_stale_catch_up_retry_timer_fires : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)100L);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_route_the_observer_again() => _eventSequence.Received(1).GetTailSequenceNumber();
}
