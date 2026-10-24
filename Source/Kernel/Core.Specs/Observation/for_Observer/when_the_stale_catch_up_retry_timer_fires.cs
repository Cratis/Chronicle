// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_the_stale_catch_up_retry_timer_fires : given.an_observer_with_subscription
{
    int _retryTimer;
    bool _retryTimerDisposed;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 42L);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)100L);
        await _observer.CaughtUp(JobId.New(), 42L);

        // Fire only the retry timer the stale lap registered last; other timers route the observer too.
        _retryTimer = _silo.TimerRegistry.Mock.Invocations.Count - 1;
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        // The test timer cancels the callback token on disposal, as Orleans does.
        await _silo.TimerRegistry.FireAsync(_retryTimer);

        // Only the one-shot retry timer disposes itself, so this also proves the fired timer is the retry timer.
        try
        {
            await _silo.TimerRegistry.FireAsync(_retryTimer);
        }
        catch (ObjectDisposedException)
        {
            _retryTimerDisposed = true;
        }
    }

    [Fact] void should_dispose_the_retry_timer() => _retryTimerDisposed.ShouldBeTrue();
    [Fact] void should_route_the_observer_again() => _eventSequence.Received().GetTailSequenceNumber();
}
