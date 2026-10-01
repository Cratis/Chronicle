// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantined_preparation_has_no_remaining_gap : given.a_quarantined_observer
{
    async Task Establish()
    {
        await _observer.CatchUp();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(42UL));
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(EventType.Unknown, 42UL)]);
    }

    [Fact] async Task should_lower_preparation() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_be_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_deliver_the_live_event() => _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_record_live_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_start_unneeded_catchup() => ShouldNotHaveStartedCatchup();
}
