// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the replay state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="definitionState">The persistent state <see cref="ObserverDefinition"/> for the observer.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
/// <param name="storage"><see cref="IStorage"/> for resetting the per-partition handled-event counts.</param>
/// <param name="logger">Logger for logging.</param>
public class Replay(
    ObserverKey observerKey,
    IPersistentState<ObserverDefinition> definitionState,
    IJobsManager jobsManager,
    IStorage storage,
    ILogger<Replay> logger) : BaseObserverState
{
    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Replaying;

    /// <summary>
    /// Gets the <see cref="JobId"/> of the replay job started, resumed, or found running when this state was last entered.
    /// Returns <see cref="JobId.NotSet"/> if no replay job has been started in the current process lifetime, or if the
    /// last attempt did not get one.
    /// </summary>
    public JobId LastStartedJobId { get; private set; } = JobId.NotSet;

    /// <summary>
    /// Gets why starting a replay job was refused when this state was last entered, or null when it was not.
    /// </summary>
    /// <remarks>
    /// A refusal is only known at the job boundary, inside <see cref="OnEnter"/>, and nothing may throw out of a
    /// state's entry: the observer would be left recorded as replaying with nothing driving the replay. The observer
    /// settles a refused start once the state has been entered, through the same release a refused completion uses.
    /// </remarks>
    internal GenerationInitializationReplayNotSupported? StartRefusal { get; private set; }

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
        // Cleared first, so a start that throws or fails never leaves an earlier replay's job id or refusal to be reported.
        LastStartedJobId = JobId.NotSet;
        StartRefusal = null;

        using var scope = logger.BeginReplayScope(state.Identifier, observerKey);
        logger.Entering();

        try
        {
            LastStartedJobId = await StartOrResumeReplayJob(state);
        }
        catch (GenerationInitializationReplayNotSupported refusal)
        {
            // Nothing was started and nothing was reset: the observer releases the replay after entering this state.
            StartRefusal = refusal;
            return state;
        }

        await storage
            .GetEventStore(observerKey.EventStore)
            .GetNamespace(observerKey.Namespace)
            .ObserverHandledCounts
            .RemoveAllFor(state.Identifier);

        return state with
        {
            IsReplaying = true,
            HandledEventCount = EventCount.Zero,
            HandledEventCountPerEventType = ImmutableDictionary<EventTypeId, EventCount>.Empty
        };
    }

    async Task<JobId> StartOrResumeReplayJob(ObserverState state)
    {
        await Observer.GetSubscription();

        return await jobsManager.StartOrResumeObserverJobFor<IReplayObserver, ReplayObserverRequest>(
            logger,
            new ReplayObserverRequest(observerKey, definitionState.State.Type, definitionState.State.EventTypes)
            {
                ReaderScope = ObserverReaderScope.For(state.SubscribesToAllEvents, definitionState.State.EventTypes)
            },
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
            async () =>
            {
                // The only place a replay job is created, reached after live and resumable jobs were selected: while
                // generation initialization has begun a replay may be resumed, never started.
                await GenerationInitializationReplay.EnsureStartAllowed(storage, observerKey, definitionState.State.Type);
                logger.StartReplayJob();
            });
    }
}
