// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

public partial class ReplayObserver
{
    async Task StopReducerReplay()
    {
        if (State.ReducerReplayPhase != ReducerReplayPhase.Building)
        {
            // A swap with an unknown outcome cannot be treated as unpublished, even during removal.
            return;
        }

        await GrainFactory.GetGrain<IReducerReplay>(Request.ObserverKey).Abandon(JobId);
        await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
    }

    async Task CompleteReducerReplay()
    {
        var replay = GrainFactory.GetGrain<IReducerReplay>(Request.ObserverKey);
        if (State.Progress.TotalSteps == 0)
        {
            // Orleans bypasses OnBeforeStartingJobSteps for zero steps. This is a legacy/no-work job,
            // not permission to finalize somebody else's context or clear the published model.
            _ = NotifyReducerObserver(preservePosition: true);
            return;
        }

        if (State.ReducerReplayPhase is not (ReducerReplayPhase.Publishing or ReducerReplayPhase.Published) &&
            (State.Status == JobStatus.Removing || !AllStepsCompletedSuccessfully))
        {
            await replay.Abandon(JobId);
            State.ReducerReplayPhase = ReducerReplayPhase.Abandoned;
            await WriteStateAsync();
            await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
            if (State.Status == JobStatus.Removing)
            {
                _ = NotifyReducerObserver(preservePosition: true);
                return;
            }

            // Failed starts have no step result. Unexpected stopped steps under Running must also remain
            // visibly failed, rather than Orleans clearing them as CompletedSuccessfully. Observation stays
            // paused; a retry rebuilds a fresh attempt, not a partial model or a redelivered deletion.
            throw new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown);
        }

        var context = State.ReducerReplayContext;
        if (context is null || !State.HandledAllEvents)
        {
            throw new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext);
        }

        if (!State.LastHandledEventSequenceNumber.IsActualValue)
        {
            await replay.Abandon(JobId);
            await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
            _ = NotifyReducerObserver(preservePosition: true);
            return;
        }

        // Building -> Publishing is durable before the RPC. If its response is lost, no catch block
        // rewinds the observer or abandons the target. Resume/new admission reconciles the idempotent swap.
        State.ReducerReplayPhase = ReducerReplayPhase.Publishing;
        await WriteStateAsync();
        var publication = await replay.Publish(context with { AllowEmptyResult = true });
        if (publication == ReplayPublication.Superseded)
        {
            return;
        }

        State.ReducerReplayPhase = ReducerReplayPhase.Published;
        Exception? bookkeepingFailure = null;
        try
        {
            await WriteStateAsync();
            await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
        }
        catch (Exception exception)
        {
            logger.ReplayFinalizationFailed(exception);
            bookkeepingFailure = exception;
        }

        // The observer may still be awaiting Start/Resume on its own turn. Its persisted Replaying state
        // is the recovery obligation if delivery fails: it must rebuild again, never route at the old watermark.
        _ = NotifyReducerObserver(preservePosition: false);
        if (bookkeepingFailure is not null || publication == ReplayPublication.PublishedWithBookkeepingFailure)
        {
            throw new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown);
        }
    }

    async Task NotifyReducerObserver(bool preservePosition)
    {
        try
        {
            var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
            var covered = preservePosition
                ? new Dictionary<Key, EventSequenceNumber>()
                : State.FailedPartitionKeys.ToDictionary(_ => _, _ => State.LastHandledEventSequenceNumber);
            await observer.ReplayedFor(
                JobId,
                State.LastHandledEventSequenceNumber,
                covered,
                preservePosition ? [] : Request.EventTypes.ToArray(),
                State.ReplayStartedAt,
                preservePosition);
        }
        catch (Exception exception)
        {
            logger.ReplayCompletionNotificationFailed(exception);
        }
    }
}
