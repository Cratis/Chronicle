// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Jobs;
using Cratis.Kernel.Grains.Jobs;
using Cratis.Kernel.Grains.Observation.Jobs;
using Cratis.Kernel.Storage.Jobs;
using Cratis.Kernel.Storage.Observation;
using Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the catch up state of an observer.
/// </summary>
public class CatchUp : BaseObserverState
{
    readonly ObserverId _observerId;
    readonly ObserverKey _observerKey;
    readonly IJobsManager _jobsManager;
    readonly ILogger<CatchUp> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> for the observer.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
    /// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
    /// <param name="logger">Logger for logging.</param>
    public CatchUp(
        ObserverId observerId,
        ObserverKey observerKey,
        IJobsManager jobsManager,
        ILogger<CatchUp> logger)
    {
        _observerId = observerId;
        _observerKey = observerKey;
        _jobsManager = jobsManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.CatchingUp;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Replay),
        typeof(Indexing),
        typeof(Routing),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = _logger.BeginCatchUpScope(state.ObserverId, _observerKey);

        var subscription = await Observer.GetSubscription();

        var jobs = await _jobsManager.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>();
        var jobsForThisObserver = jobs.Where(IsJobForThisObserver);
        if (jobs.Any(_ => _.Status == JobStatus.Running))
        {
            _logger.FinishingExistingCatchUpJob();
            return state;
        }

        var pausedJob = jobs.FirstOrDefault(_ => _.Status == JobStatus.Paused);

        if (pausedJob is not null)
        {
            _logger.ResumingCatchUpJob();
            await _jobsManager.Resume(pausedJob.Id);
        }
        else
        {
            _logger.StartCatchUpJob(state.NextEventSequenceNumber);
            await _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(
                JobId.New(),
                new CatchUpObserverRequest(
                    state.ObserverId,
                    _observerKey,
                    subscription,
                    state.NextEventSequenceNumber,
                    state.EventTypes));
        }

        return state;
    }

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        return Task.FromResult(state);
    }

    bool IsJobForThisObserver(JobState jobState) =>
        ((ReplayObserverRequest)jobState.Request).ObserverId == _observerId &&
        ((ReplayObserverRequest)jobState.Request).ObserverKey == _observerKey;
}
