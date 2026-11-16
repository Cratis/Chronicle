// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// When the short retry fires the observer is routed again (#5351).
/// </summary>
public class when_the_stale_catch_up_retry_timer_fires : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)50UL);
        await _observer.CaughtUp(JobId.New(), 42L);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_route_the_observer_again() => await _eventSequence.Received().GetTailSequenceNumber();
    [Fact] async Task should_no_longer_be_backed_off() => (await _observer.IsCatchUpBackedOff()).ShouldBeFalse();
}
