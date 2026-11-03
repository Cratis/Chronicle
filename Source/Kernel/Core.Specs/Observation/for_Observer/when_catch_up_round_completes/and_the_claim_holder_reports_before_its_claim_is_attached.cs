// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// The job started under the current claim releases it, even when the attachment to the job was never persisted.
/// </summary>
public class and_the_claim_holder_reports_before_its_claim_is_attached : given.an_observer_with_skip_detection
{
    JobId _job;

    async Task Establish()
    {
        _job = JobId.New();
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
        _jobStorage.GetJob(_job).Returns(Task.FromResult<Catch<JobState, Cratis.Orleans.Storage.Jobs.JobError>>(new JobState
        {
            Id = _job,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, 12UL, []) { OwnershipEpoch = 2 }
        }));
        await Crash();
    }

    async Task Because() => await _observer.CaughtUp(_job, 12UL, 12UL);

    [Fact] void should_release_the_claim() => _stateStorage.State.ObserverWideClaim.ShouldBeNull();
}
