// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the catch up state of an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CatchUp"/> class.
/// </remarks>
/// <param name="observerId">The <see cref="ObserverId"/> for the observer.</param>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
/// <param name="logger">Logger for logging.</param>
public class CatchUp(
    ObserverId observerId,
    ObserverKey observerKey,
    IJobsManager jobsManager,
    ILogger<CatchUp> logger) : BaseObserverState
{
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
        using var scope = logger.BeginCatchUpScope(state.ObserverId, observerKey);

        var subscription = await Observer.GetSubscription();

        var jobs = await jobsManager.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>();
        var jobsForThisObserver = jobs.Where(IsJobForThisObserver);
        if (jobs.Any(_ => _.Status == JobStatus.Running))
        {
            logger.FinishingExistingCatchUpJob();
            return state;
        }

        var pausedJob = jobs.FirstOrDefault(_ => _.Status == JobStatus.Paused);

        if (pausedJob is not null)
        {
            logger.ResumingCatchUpJob();
            await jobsManager.Resume(pausedJob.Id);
        }
        else
        {
            logger.StartCatchUpJob(state.NextEventSequenceNumber);
            await jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(
                JobId.New(),
                new CatchUpObserverRequest(
                    state.ObserverId,
                    observerKey,
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
        ((ReplayObserverRequest)jobState.Request).ObserverId == observerId &&
        ((ReplayObserverRequest)jobState.Request).ObserverKey == observerKey;
}
