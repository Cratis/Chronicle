// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catch_up_is_deferred_for_capacity_and_its_retry_timer_fires : given.an_observer_with_subscription
{
    ICatchUpAdmission _admission;

    protected override ICatchUpAdmission CreateCatchUpAdmission()
    {
        _admission = Substitute.For<ICatchUpAdmission>();
        _admission.TryAdmit(Arg.Any<ObserverKey>(), Arg.Any<int>(), Arg.Any<ulong>()).Returns(Task.FromResult<IDisposable?>(null));
        return _admission;
    }

    async Task Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)10UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)3UL);

        // Subscribing routes the observer, which is behind, so its catch-up is deferred and the retry timer is set.
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        _admission.ClearReceivedCalls();
    }

    async Task Because()
    {
        // The watchdog is a minute away, so only the retry timer can ask the admission again within the deferral delay.
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_ask_for_admission_again() => await _admission.Received().TryAdmit(Arg.Any<ObserverKey>(), Arg.Any<int>(), Arg.Any<ulong>());
}
