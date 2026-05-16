// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_any_affected_observer_has_failed_partitions : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.IsSubscribed().Returns(true);

        var observerDefinition = new ObserverDefinition(
            "observer-1",
            [],
            Concepts.EventSequences.EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);
        var observerState = new ObserverState(
            "observer-1",
            12UL,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            false);
        var failedPartitions = new Concepts.Observation.FailedPartitions
        {
            Partitions =
            [
                new()
                {
                    ObserverId = "observer-1",
                    Partition = Concepts.Keys.Key.Undefined
                }
            ]
        };

        _observerDefinitionsStorage.GetAll().Returns([observerDefinition]);
        _observerStateStorage.GetAll().Returns([observerState]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(failedPartitions);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(new WaitForObserverCompletionRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        TailEventSequenceNumber = 42UL
    });

    [Fact] void should_be_unsuccessful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_failed_partitions() => _result.FailedPartitions.Count().ShouldEqual(1);
}
