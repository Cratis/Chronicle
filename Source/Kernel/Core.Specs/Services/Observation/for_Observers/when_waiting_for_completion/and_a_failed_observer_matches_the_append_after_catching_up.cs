// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_a_failed_observer_matches_the_append_after_catching_up : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        _appendedEvent = _appendedEvent with { Context = _appendedEvent.Context with { Tags = [new Tag("priority")] } };
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = 53UL }]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions
        {
            Partitions = [new Concepts.Observation.FailedPartition { ObserverId = "filtered-observer", Partition = Key.Undefined }]
        });
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 53UL,
        TailEventSequenceNumber = 53UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 53UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_report_the_relevant_failure() => _result.FailedPartitions.Count().ShouldEqual(1);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
}
