// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_observer_information;

public class and_running_state_changed_at_is_unknown : given.all_dependencies
{
    Contracts.Observation.ObserverInformation _response;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.GetDefinition().Returns(new ObserverDefinition());
        observer.GetState().Returns(new ObserverState());
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _response = await _observers.GetObserverInformation(new GetObserverInformationRequest
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = "event-log"
    });

    [Fact] void should_leave_the_timestamp_unknown() => _response.RunningStateChangedAt.ShouldBeNull();
}
