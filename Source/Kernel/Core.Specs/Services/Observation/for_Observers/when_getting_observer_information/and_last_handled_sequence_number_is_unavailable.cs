// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using static Cratis.Chronicle.Services.TypeScriptSequenceNumberCompatibility;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_observer_information;

public class and_last_handled_sequence_number_is_unavailable : given.all_dependencies
{
    ObserverInformation _result;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.GetDefinition().Returns(new ObserverDefinition(
            "observer-1",
            [new EventType("some-event-type", 1)],
            EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true));

        observer.GetState().Returns(new ObserverState(
            "observer-1",
            EventSequenceNumber.Unavailable,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            0,
            false,
            false));

        observer.IsSubscribed().Returns(true);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _result = await _observers.GetObserverInformation(new()
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        ObserverId = "observer-1",
        EventSequenceId = EventSequenceId.Log
    });

    [Fact] void should_sanitize_last_handled_sequence_number() => _result.LastHandledEventSequenceNumber.ShouldEqual(MaxSafeInteger);
}
