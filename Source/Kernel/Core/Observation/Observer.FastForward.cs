// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    /// <remarks>
    /// Skips everything between where the observer is and the tail of the event sequence without invoking its
    /// subscriber. Catch-up and retry jobs for the observer are dropped, since the events they would handle are being
    /// skipped, and failed partitions are discarded for the same reason. Events appended after the tail was read are
    /// delivered as normal once the observer is routed again.
    /// </remarks>
    public async Task<FastForwardResult> FastForward()
    {
        ThrowIfSealed();
        await ThrowIfKeyDoesNotOwnDefinition();
        var previous = State.NextEventSequenceNumber;
        if (IsRetired || _removed)
        {
            return new(FastForwardOutcome.NotAvailable, previous, previous);
        }

        if (State.RunningState == ObserverRunningState.Replaying || State.IsReplaying)
        {
            return new(FastForwardOutcome.Replaying, previous, previous);
        }

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        var tail = await _eventSequence.GetTailSequenceNumber();
        if (!tail.IsActualValue || (previous.IsActualValue && previous > tail && !State.CatchingUpPartitions.Any() && !Failures.HasFailedPartitions))
        {
            return new(FastForwardOutcome.AlreadyAtTail, previous, previous);
        }

        var next = tail.Next();
        await DropJobsSkippedByFastForward();
        if (Failures.HasFailedPartitions)
        {
            await DiscardFailedPartitions(AlertClearedReason.Cleared);
        }

        State.CatchingUpPartitions.Clear();
        _isPreparingCatchup = false;
        State = State with
        {
            NextEventSequenceNumber = next,
            TailEventSequenceNumber = tail
        };
        await WriteStateAsync();

        if (!IsQuarantined)
        {
            await TransitionTo<Routing>();
        }

        return new(FastForwardOutcome.FastForwarded, previous, next);
    }

    async Task DropJobsSkippedByFastForward()
    {
        var jobs = await _jobsManager.GetUnfinishedJobs();
        var skipped = jobs
            .Where(job => job is { Request: IObserverJobRequest request } &&
                          request is not ReplayObserverRequest &&
                          request.ObserverKey == _observerKey)
            .ToArray();

        await Task.WhenAll(skipped.Select(job => _jobsManager.Stop(job.Id)));
        foreach (var job in skipped)
        {
            await _jobsManager.Delete(job.Id);
        }
    }
}
