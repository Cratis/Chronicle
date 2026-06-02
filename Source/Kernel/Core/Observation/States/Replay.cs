// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the replay state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="definitionState">The persistent state <see cref="ObserverDefinition"/> for the observer.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
/// <param name="logger">Logger for logging.</param>
public class Replay(
    ObserverKey observerKey,
    IPersistentState<ObserverDefinition> definitionState,
    IJobsManager jobsManager,
    ILogger<Replay> logger) : BaseObserverState
{
    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Replaying;

    /// <summary>
    /// Gets the <see cref="JobId"/> of the most recent replay job started, resumed, or found running for this observer.
    /// Returns <see cref="JobId.NotSet"/> if no replay job has been started in the current process lifetime.
    /// </summary>
    public JobId LastStartedJobId { get; private set; } = JobId.NotSet;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing),
        typeof(QuarantinedObserver),
        typeof(Disconnected),
        typeof(CatchingUpInFlight)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginReplayScope(state.Identifier, observerKey);
        logger.Entering();

        var subscription = await Observer.GetSubscription();

        LastStartedJobId = await jobsManager.StartOrResumeObserverJobFor<IReplayObserver, ReplayObserverRequest>(
            logger,
            new(observerKey, definitionState.State.Type, definitionState.State.EventTypes),
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
        return state with
        {
            IsReplaying = true,
            HandledEventCount = EventCount.Zero,
            HandledEventCountPerEventType = ImmutableDictionary<EventTypeId, EventCount>.Empty,
            HandledEventCountPerPartition = ImmutableDictionary<Key, IReadOnlyDictionary<EventTypeId, EventCount>>.Empty
        };
    }
}
