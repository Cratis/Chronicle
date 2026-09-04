// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_checking_applied_through;

/// <summary>
/// A caller's deadline elapsing must be reported as a typed outcome for whichever observer had not resolved yet,
/// never as an unstructured cancellation fault out of the call.
/// </summary>
public class and_the_callers_deadline_has_already_elapsed : given.all_dependencies
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

        _observerDefinitionsStorage.GetAll().Returns([observerDefinition]);
        _observerStateStorage.GetAll().Returns([observerState]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<Concepts.Observation.ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
    }

    async Task Because() => _result = await _observers.AppliedThrough(
        new AppliedThroughRequest
        {
            EventStore = "event-store",
            Namespace = "event-store-namespace",
            EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
            ObserverIds = ["observer-1"],
            TargetEventSequenceNumber = 42UL
        },
        new CallContext(new CallOptions(cancellationToken: new CancellationToken(true))));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_observer_as_timed_out() => _result.Results.Single().Outcome.ShouldEqual(AppliedThroughOutcome.TimedOut);
}
