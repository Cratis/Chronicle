// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_only_the_matching_failed_observer_is_reported : given.an_observer_with_filtered_event
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        SetFilters(new Concepts.Observation.ObserverFilters(["priority"]));
        _appendedEvent = _appendedEvent with { Context = _appendedEvent.Context with { Tags = [new Tag("priority")] } };
        _cursor.MoveNext().Returns(true, false, true, false);
        var eventTypes = new[] { new EventType("a-recorded", 1) };
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition("filtered-observer", eventTypes, Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true),
            new ObserverDefinition("other-observer", eventTypes, Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true)
        ]);
        _observerStateStorage.GetAll().Returns(
        [
            new ObserverState { Identifier = "filtered-observer", LastHandledEventSequenceNumber = 53UL },
            new ObserverState { Identifier = "other-observer", LastHandledEventSequenceNumber = 53UL }
        ]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions
        {
            Partitions =
            [
                new Concepts.Observation.FailedPartition { ObserverId = "filtered-observer", Partition = Key.Undefined },
                new Concepts.Observation.FailedPartition { ObserverId = "other-observer", Partition = "other" }
            ]
        });
        var other = Substitute.For<IObserver>();
        other.GetSubscription().Returns(new ObserverSubscription(
            "other-observer",
            new ObserverKey("other-observer", "event-store", "event-store-namespace", Concepts.EventSequences.EventSequenceId.Log),
            eventTypes,
            typeof(IObserverSubscriber),
            SiloAddress.Zero,
            Filters: new Concepts.Observation.ObserverFilters(["other"])));
        _grainFactory.GetGrain<IObserver>((string)new ObserverKey("other-observer", "event-store", "event-store-namespace", Concepts.EventSequences.EventSequenceId.Log)).Returns(other);
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

    [Fact] void should_fail_for_the_relevant_observer() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_only_report_the_matching_failure() => _result.FailedPartitions.Single().ObserverId.ShouldEqual("filtered-observer");
}
