// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_observer_information;

public class and_the_observer_changes_between_reads : given.all_dependencies
{
    IObserver _observer;
    Contracts.Observation.ObserverInformation _response;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observer.GetInformation().Returns(new ObserverInformationSnapshot(new ObserverDefinition(), new ObserverState(), true, null));
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    async Task Because() => _response = await _observers.GetObserverInformation(new GetObserverInformationRequest
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = "event-log"
    });

    [Fact] void should_read_the_snapshot_once() => _observer.Received(1).GetInformation();
    [Fact] void should_not_read_the_state_separately() => _observer.DidNotReceive().GetState();
    [Fact] void should_not_read_the_subscription_separately() => _observer.DidNotReceive().IsSubscribed();
    [Fact] void should_return_the_snapshot_subscription_flag() => _response.IsSubscribed.ShouldBeTrue();
}
