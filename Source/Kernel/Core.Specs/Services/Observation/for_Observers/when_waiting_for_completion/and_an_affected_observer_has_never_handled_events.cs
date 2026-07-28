// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_affected_observer_has_never_handled_events : given.all_dependencies
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

        // A registered-but-never-run observer carries EventSequenceNumber.Unavailable
        // (ulong.MaxValue) as its last handled sequence number.
        var observerState = new ObserverState
        {
            Identifier = "observer-1",
            LastHandledEventSequenceNumber = Concepts.Events.EventSequenceNumber.Unavailable,
            RunningState = Concepts.Observation.ObserverRunningState.Active,
            ReplayingPartitions = new HashSet<Concepts.Keys.Key>(),
            CatchingUpPartitions = new HashSet<Concepts.Keys.Key>(),
            FailedPartitions = [],
            IsReplaying = false
        };

        _observerDefinitionsStorage.GetAll().Returns([observerDefinition]);
        _observerStateStorage.GetAll().Returns([observerState]);

        // First poll has no failed partitions - a never-handled observer must NOT be
        // reported as caught up here (it would prematurely succeed before the fix).
        // The second poll surfaces the observer's failed partition so the loop terminates.
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(
            new Concepts.Observation.FailedPartitions(),
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

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_report_completion_before_the_observer_has_handled_events() =>
        _failedPartitionsStorage.Received(2).GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>());
}
