// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_checking_applied_through;

public class and_the_observer_id_does_not_exist : given.all_dependencies
{
    AppliedThroughResponse _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns([]);
        _observerStateStorage.GetAll().Returns([]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
    }

    async Task Because() => _result = await _observers.AppliedThrough(new AppliedThroughRequest
    {
        EventStore = "event-store",
        Namespace = "event-store-namespace",
        EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
        ObserverIds = ["observer-that-does-not-exist"],
        TargetEventSequenceNumber = 42UL
    });

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_observer_as_unavailable() => _result.Results.Single().Outcome.ShouldEqual(AppliedThroughOutcome.Unavailable);
}
