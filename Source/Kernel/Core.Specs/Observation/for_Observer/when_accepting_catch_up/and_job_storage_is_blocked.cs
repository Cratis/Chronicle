// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_job_storage_is_blocked : given.an_observer_accepting_a_catch_up_handover
{
    readonly given.held_storage_operation<IImmutableList<JobState>> _lookup = new();
    ObserverState _accepted;
    bool _queriedInline;

    void Establish()
    {
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => _lookup.Invoke());
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because()
    {
        Task? continuation = null;
        try
        {
            await _observer.CaughtUp(JobId.New(), 12UL, 12UL).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _accepted = _writes.PersistedState;
            _queriedInline = _jobsManager.ReceivedCalls().Any();
            continuation = _silo.RunCatchUpContinuation();
            await _lookup.WaitForStart();
        }
        finally
        {
            _lookup.Complete(ImmutableList<JobState>.Empty);
            if (continuation is not null) await continuation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_acknowledge_before_job_queries() => _queriedInline.ShouldBeFalse();
    [Fact] void should_accept_the_cursor_while_jobs_are_unavailable() => (_accepted.NextEventSequenceNumber <= 10UL).ShouldBeTrue();
}
