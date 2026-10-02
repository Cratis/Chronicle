// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_application_observer_is_disconnected : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(observer);
        _observerDefinitionsStorage.GetAll().Returns([
            new ObserverDefinition("application-observer", [new EventType("customer-named", EventTypeGeneration.First)], EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true)
        ]);
        _observerStateStorage.GetAll().Returns([
            new ObserverState
            {
                Identifier = "application-observer",
                RunningState = Concepts.Observation.ObserverRunningState.Disconnected,
                LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable
            }
        ]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new()
    {
        EventStore = "event-store",
        Namespace = "new-namespace",
        EventSequenceId = EventSequenceId.Log,
        TailEventSequenceNumber = EventSequenceNumber.First,
        EventTypeTails = [new() { EventType = new() { Id = "customer-named", Generation = 1 }, SequenceNumber = EventSequenceNumber.First }],
        TimeoutMilliseconds = 1
    });

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_time_out() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_report_the_disconnected_consumer() => _result.OutstandingObservers.ShouldContainOnly("application-observer");
}
