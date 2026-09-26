// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_no_event_type_tails_are_given_with_a_short_server_timeout : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(
                "observer-b",
                [new Concepts.Events.EventType("b-recorded", 1)],
                Concepts.EventSequences.EventSequenceId.Log,
                Concepts.Observation.ObserverType.Reactor,
                Concepts.Observation.ObserverOwner.Client,
                true)
        ]);
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = "observer-b", LastHandledEventSequenceNumber = 12UL }]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        TailEventSequenceNumber = 42UL,
        TimeoutMilliseconds = 1
    });

    [Fact] void should_time_out_waiting_for_all_observers() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_name_the_outstanding_observer() => _result.OutstandingObservers.ShouldContain("observer-b");
}
