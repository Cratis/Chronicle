// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task ReleaseRefusedReplay()
    {
        if (IsRetired || _removed || (!State.IsReplaying && State.RunningState != ObserverRunningState.Replaying)) return;

        Skips.Reset();
        State = State with
        {
            IsReplaying = false,
            ReplayingPartitions = new HashSet<Key>(),

            // Full replay supersedes partition catch-ups. Routing clears these markers too and re-evaluates
            // outstanding work from the retained cursor; refusal must not fabricate any completed coverage.
            CatchingUpPartitions = new HashSet<Key>()
        };
        SeedSuspectedSkipOwnership();
        RecordedHandlingState();
        await WriteStateAsync();
        if (!IsQuarantined)
        {
            await TransitionTo<Routing>();
        }
    }

    /// <inheritdoc/>
    public async Task ReleaseRefusedPartitionReplay(Key partition)
    {
        if (IsRetired || _removed || !State.ReplayingPartitions.Contains(partition)) return;

        // Keep ownership until the catch-up decision, but claim no replay coverage or successful delivery.
        Skips.JobStarted(partition);
        var ownership = Skips.OwnershipTicket(partition);
        State.ReplayingPartitions.Remove(partition);
        RecordedHandlingState();
        await WriteStateAsync();
        try
        {
            if (!IsQuarantined)
            {
                await ProbeAndStartCatchupJobFrom(partition, State.NextEventSequenceNumber);
            }
        }
        finally
        {
            Skips.Release(partition, ownership);
        }

        EvaluateSuspectedSkips();
    }
}
