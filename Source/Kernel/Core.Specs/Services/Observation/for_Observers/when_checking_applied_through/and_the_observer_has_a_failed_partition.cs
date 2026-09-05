// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_checking_applied_through;

public class and_the_observer_has_a_failed_partition : given.all_dependencies
{
    AppliedThroughResponse _result;

    void Establish()
    {
        var observerDefinition = new ObserverDefinition(
            "observer-1",
            [],
            Concepts.EventSequences.EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);
        var observerState = new ObserverState
        {
            Identifier = "observer-1",
            LastHandledEventSequenceNumber = 12UL,
            RunningState = Concepts.Observation.ObserverRunningState.Active,
            ReplayingPartitions = new HashSet<Concepts.Keys.Key>(),
            CatchingUpPartitions = new HashSet<Concepts.Keys.Key>(),
            FailedPartitions = [],
            IsReplaying = false
        };
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
    }

    async Task Because() => _result = await _observers.AppliedThrough(new AppliedThroughRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        ObserverIds = ["observer-1"],
        TargetEventSequenceNumber = 42UL
    });

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_observer_as_failed() => _result.Results.Single().Outcome.ShouldEqual(AppliedThroughOutcome.Failed);
}
