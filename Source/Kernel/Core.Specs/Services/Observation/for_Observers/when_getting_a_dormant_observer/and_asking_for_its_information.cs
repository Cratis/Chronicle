// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_a_dormant_observer;

public class and_asking_for_its_information : given_a_dormant_observer
{
    Contracts.Observation.ObserverInformation _response;

    async Task Because() => _response = await _service.GetObserverInformation(new GetObserverInformationRequest
    {
        EventStore = "store",
        Namespace = "cratis",
        ObserverId = "observer",
        EventSequenceId = "event-log"
    });

    [Fact] void should_not_wake_the_observer() => _observer.DidNotReceive().GetInformation();
    [Fact] void should_report_it_as_subscribed() => _response.IsSubscribed.ShouldBeTrue();
    [Fact] void should_report_the_stored_position() => _response.NextEventSequenceNumber.ShouldEqual(42ul);
    [Fact] void should_report_when_the_running_state_changed() => ((DateTimeOffset?)_response.RunningStateChangedAt).ShouldEqual(_changedAt);
}
