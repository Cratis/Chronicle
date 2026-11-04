// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// A newer catch-up claim is granted and persisted before its job starts, and names no job until the start returns. A
/// job started under an older epoch reporting back in that window must not release it, or the newer job is refused.
/// </summary>
public class and_a_stale_job_reports_before_the_newer_claim_is_attached : given.an_observer_with_skip_detection
{
    JobId _staleJob;

    async Task Establish()
    {
        _staleJob = JobId.New();
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 11UL,
            NextEventSequenceNumber = 12UL,
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, null, new(2), 12UL, []),
            OwnershipRevision = new(2)
        };
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _jobStorage.GetJob(_staleJob).Returns(Task.FromResult<Catch<JobState, Cratis.Choreography.Storage.Jobs.JobError>>(new JobState
        {
            Id = _staleJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, 1UL, []) { OwnershipEpoch = 1 }
        }));
        await Crash();
    }

    async Task Because() => await _observer.CaughtUp(_staleJob, 12UL, 12UL);

    [Fact] void should_keep_the_newer_claim() => _stateStorage.State.ObserverWideClaim!.Epoch.ShouldEqual(new ClaimEpoch(2));
    [Fact] void should_keep_it_held_by_catch_up() => _stateStorage.State.ObserverWideClaim!.Holder.ShouldEqual(ClaimHolderKind.ObserverCatchUp);
    [Fact] void should_keep_admitting_the_newer_job() =>
        _observer.IsObsolete(new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, 12UL, []) { OwnershipEpoch = 2 }).ShouldBeFalse();
}
