// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClientEventSequenceNumber = Cratis.Chronicle.Events.EventSequenceNumber;

namespace Cratis.Chronicle.Projections.for_Projections;

public class when_asking_for_the_state_of_a_projection : given.a_discovered_projection
{
    ProjectionState _result;

    void Establish() =>
        _observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>())
            .Returns(new ObserverInformation
            {
                RunningState = ObserverRunningState.Active,
                IsSubscribed = true,
                NextEventSequenceNumber = 43,
                LastHandledEventSequenceNumber = 42,
                TailEventSequenceNumber = 42
            });

    async Task Because() => _result = await _projections.GetStateFor<TheProjection>();

    [Fact] void should_ask_the_observer_for_the_projection() => _observers.Received(1).GetObserverInformation(Arg.Is<GetObserverInformationRequest>(_ => _.ObserverId == _projections.GetProjectionIdFor<TheProjection>().Value));
    [Fact] void should_report_it_as_subscribed() => _result.IsSubscribed.ShouldBeTrue();
    [Fact] void should_report_the_next_event_sequence_number() => _result.NextEventSequenceNumber.ShouldEqual(new ClientEventSequenceNumber(43));
}
