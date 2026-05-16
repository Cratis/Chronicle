// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_other_affected_observers_are_still_processing : given.all_dependencies
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.IsSubscribed().Returns(true);

        _observerDefinitionsStorage.GetAll().Returns(
        [
            new(
                "observer-1",
                [],
                Concepts.EventSequences.EventSequenceId.Log,
                Concepts.Observation.ObserverType.Reactor,
                Concepts.Observation.ObserverOwner.Client,
                true),
            new(
                "observer-2",
                [],
                Concepts.EventSequences.EventSequenceId.Log,
                Concepts.Observation.ObserverType.Reactor,
                Concepts.Observation.ObserverOwner.Client,
                true)
        ]);
        _observerStateStorage.GetAll().Returns(
        [
            new(
                "observer-1",
                12UL,
                Concepts.Observation.ObserverRunningState.Active,
                new HashSet<Concepts.Keys.Key>(),
                new HashSet<Concepts.Keys.Key>(),
                [],
                false),
            new(
                "observer-2",
                12UL,
                Concepts.Observation.ObserverRunningState.Active,
                new HashSet<Concepts.Keys.Key>(),
                new HashSet<Concepts.Keys.Key>(),
                [],
                false)
        ]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(
            new Concepts.Observation.FailedPartitions
            {
                Partitions =
                [
                    new()
                    {
                        ObserverId = "observer-1",
                        Partition = "partition-1"
                    }
                ]
            },
            new Concepts.Observation.FailedPartitions
            {
                Partitions =
                [
                    new()
                    {
                        ObserverId = "observer-1",
                        Partition = "partition-1"
                    },
                    new()
                    {
                        ObserverId = "observer-2",
                        Partition = "partition-2"
                    }
                ]
            });
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
    [Fact] void should_have_all_failed_partitions() => _result.FailedPartitions.Count().ShouldEqual(2);
    [Fact] void should_continue_polling_until_all_observers_are_complete_or_failed() =>
        _failedPartitionsStorage.Received(2).GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>());
}
