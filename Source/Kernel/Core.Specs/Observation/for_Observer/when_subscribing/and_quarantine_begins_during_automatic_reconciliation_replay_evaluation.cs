// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_begins_during_automatic_reconciliation_replay_evaluation : given.an_observer_automatically_reconciled_during_a_probe
{
    readonly TaskCompletionSource<RecommendationId> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)42UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
        {
            // A changed definition makes replay evaluation reach its recommendation await.
            _definitionStorage.State = _definitionStorage.State with { EventTypes = [] };
            return Result<EventSequenceNumber, GetSequenceNumberError>.Success(42UL);
        });
        var recommendations = Substitute.For<IRecommendationsManager>();
        recommendations.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(Arg.Any<RecommendationDescription>(), Arg.Any<ReplayCandidateRequest>()).Returns(_ =>
        {
            _probeEntered.TrySetResult();
            return _query.Task;
        });
        _silo.AddProbe(_ => recommendations);
    }

    async Task Because() => await QuarantineDuringProbe(ReconcileSubscription(), () => _query.SetResult(new(Guid.NewGuid())));

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_resume_jobs() => ShouldNotResumeJobs();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
