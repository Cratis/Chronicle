// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the replay state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
/// <param name="logger">Logger for logging.</param>
public class Replay(
    ObserverKey observerKey,
    IJobsManager jobsManager,
    ILogger<Replay> logger) : BaseObserverState
{
    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Replaying;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginReplayScope(state.Id, observerKey);
        logger.Entering();

        var subscription = await Observer.GetSubscription();

        await jobsManager.StartOrResumeObserverJobFor<IReplayObserver, ReplayObserverRequest>(
            logger,
            new(observerKey, state.Type, state.EventTypes),
            requestPredicate: null,
            () =>
            {
                logger.FinishingExistingReplayJob();
                return Task.CompletedTask;
            },
            () =>
            {
                logger.ResumingReplayJob();
                return Task.CompletedTask;
            },
            () =>
            {
                logger.StartReplayJob();
                return Task.CompletedTask;
            });
        return state with { IsReplaying = true };
    }
}
