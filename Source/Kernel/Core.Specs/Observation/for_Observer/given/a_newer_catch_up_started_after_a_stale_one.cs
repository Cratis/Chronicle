// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// An observer that started a newer observer-wide catch-up, while a catch-up job it started earlier may still report back.
/// </summary>
public class a_newer_catch_up_started_after_a_stale_one : an_observer_with_subscription
{
    protected JobId _newerJob;
    protected JobId _staleJob;
    protected ObserverWideClaim? _newerClaim;
    protected EventSequenceNumber _lastHandledBefore;
    protected EventSequenceNumber _nextBefore;
    protected int _writesBefore;

    void Establish()
    {
        _newerJob = JobId.New();
        _staleJob = JobId.New();
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_newerJob)));
    }

    protected async Task StartNewerCatchUp()
    {
        await _observer.CatchUp();
        _newerClaim = _stateStorage.State.ObserverWideClaim;
        _lastHandledBefore = _stateStorage.State.LastHandledEventSequenceNumber;
        _nextBefore = _stateStorage.State.NextEventSequenceNumber;
        _jobsManager.ClearReceivedCalls();
    }

    protected Task ReportStaleCatchUp()
    {
        _writesBefore = _storageStats.Writes;
        return _observer.CaughtUp(_staleJob, 90UL, 90UL);
    }
}
