// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_all_events_observer_is_behind : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(
                "all-events-observer",
                [],
                Concepts.EventSequences.EventSequenceId.Log,
                Concepts.Observation.ObserverType.Projection,
                Concepts.Observation.ObserverOwner.Kernel,
                true)
        ]);
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "all-events-observer", LastHandledEventSequenceNumber = 40UL }]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
        var observer = Substitute.For<IObserver>();
        observer.GetSubscription().Returns(new ObserverSubscription(
            "all-events-observer",
            new Concepts.Observation.ObserverKey("all-events-observer", "event-store", "event-store-namespace", Concepts.EventSequences.EventSequenceId.Log),
            [],
            typeof(IObserverSubscriber),
            SiloAddress.Zero));
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        TailEventSequenceNumber = 42UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 42UL }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_time_out_waiting_for_the_all_events_observer() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_name_the_outstanding_observer() => _result.OutstandingObservers.ShouldContain("all-events-observer");
}
