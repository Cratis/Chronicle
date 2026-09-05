// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_checking_applied_through;

public class and_the_observer_is_quarantined : given.all_dependencies
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

        // Quarantined despite having already handled past the target - the running state must decide the
        // outcome, not the position, since a quarantined observer will not resume on its own.
        var observerState = new ObserverState
        {
            Identifier = "observer-1",
            LastHandledEventSequenceNumber = 100UL,
            RunningState = Concepts.Observation.ObserverRunningState.Quarantined,
            ReplayingPartitions = new HashSet<Concepts.Keys.Key>(),
            CatchingUpPartitions = new HashSet<Concepts.Keys.Key>(),
            FailedPartitions = [],
            IsReplaying = false
        };

        _observerDefinitionsStorage.GetAll().Returns([observerDefinition]);
        _observerStateStorage.GetAll().Returns([observerState]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
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
    [Fact] void should_report_the_observer_as_quarantined() => _result.Results.Single().Outcome.ShouldEqual(AppliedThroughOutcome.Quarantined);
}
