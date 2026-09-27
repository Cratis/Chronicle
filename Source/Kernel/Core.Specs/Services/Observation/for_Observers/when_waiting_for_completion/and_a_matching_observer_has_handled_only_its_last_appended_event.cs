// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_a_matching_observer_has_handled_only_its_last_appended_event : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(
                "observer-a",
                [new Concepts.Events.EventType("a-recorded", 1)],
                Concepts.EventSequences.EventSequenceId.Log,
                Concepts.Observation.ObserverType.Reactor,
                Concepts.Observation.ObserverOwner.Client,
                true)
        ]);
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "observer-a", LastHandledEventSequenceNumber = 40UL }]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        TailEventSequenceNumber = 42UL,
        EventTypeTails =
        [
            new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 40UL },
            new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "b-recorded", Generation = 1 }, SequenceNumber = 42UL }
        ],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_complete_after_the_last_matching_event() => _result.IsSuccess.ShouldBeTrue();
}
