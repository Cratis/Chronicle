// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_unsubscribed_kernel_observer_handles_the_event_after_the_first_poll : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;
    int _stateReads;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(observer);
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(
                PatternCapture.ObserverIdentifier,
                [new EventType("a-recorded", 1)],
                Concepts.EventSequences.EventSequenceId.Log,
                Concepts.Observation.ObserverType.Reactor,
                Concepts.Observation.ObserverOwner.Kernel,
                false)
        ]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
        _observerStateStorage.GetAll().Returns(_ => ++_stateReads == 1
            ? []
            : new[] { new ObserverState { Identifier = PatternCapture.ObserverIdentifier, LastHandledEventSequenceNumber = 0UL } });
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "second-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        FirstEventSequenceNumber = 0UL,
        TailEventSequenceNumber = 0UL,
        EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 0UL }],
        TimeoutMilliseconds = 1000
    });

    [Fact] void should_wait_until_pattern_capture_has_handled_the_first_event() => _stateReads.ShouldBeGreaterThan(1);
    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_time_out() => _result.TimedOut.ShouldBeFalse();
}
