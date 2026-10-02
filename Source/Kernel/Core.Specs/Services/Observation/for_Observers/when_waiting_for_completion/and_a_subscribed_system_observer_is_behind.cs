// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_a_subscribed_system_observer_is_behind : given.application_and_system_observers
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _systemObserver.GetSubscription().Returns(new ObserverSubscription(
            SystemObserverId,
            new ObserverKey(SystemObserverId, _request.EventStore, _request.Namespace, _request.EventSequenceId),
            [new EventType("a-recorded", 1)],
            typeof(IObserverSubscriber),
            SiloAddress.Zero));
        _observerStateStorage.GetAll().Returns(
        [
            new ObserverState { Identifier = ApplicationObserverId, LastHandledEventSequenceNumber = 0UL },
            new ObserverState { Identifier = SystemObserverId, RunningState = Concepts.Observation.ObserverRunningState.Disconnected }
        ]);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(_request);

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_time_out() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_wait_for_the_subscribed_system_observer_regardless_of_running_state() => _result.OutstandingObservers.ShouldContainOnly(SystemObserverId);
}
