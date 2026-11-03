// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// A catch-up job reporting back after the observer-wide claim was attached to a newer job does not release it.
/// </summary>
public class and_a_stale_job_reports_after_the_newer_claim_is_attached : given.an_observer_with_skip_detection
{
    JobId _newerJob;

    async Task Establish()
    {
        _newerJob = JobId.New();
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 11UL,
            NextEventSequenceNumber = 12UL,
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, _newerJob, new(2), 12UL, []),
            OwnershipRevision = new(2)
        };
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        await Crash();
    }

    async Task Because() => await _observer.CaughtUp(JobId.New(), 12UL, 12UL);

    [Fact] void should_keep_the_newer_claim() => _stateStorage.State.ObserverWideClaim!.Job.ShouldEqual(_newerJob);
    [Fact] void should_keep_its_epoch() => _stateStorage.State.ObserverWideClaim!.Epoch.ShouldEqual(new ClaimEpoch(2));
    [Fact] void should_keep_admitting_the_newer_job() =>
        _observer.IsObsolete(new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, 12UL, []) { OwnershipEpoch = 2 }).ShouldBeFalse();
}
