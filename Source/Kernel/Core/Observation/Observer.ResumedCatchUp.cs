// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    /// <remarks>
    /// Raises the blanket hold only. The flag is in-memory, so nothing is written. It comes down again when the
    /// job registers the partitions it owns, or when catch-up concludes.
    /// </remarks>
    public Task ResumingCatchUp(JobId jobId)
    {
        if (IsRetired || _removed || _concludedCatchUpJobs.Contains(jobId)) return Task.CompletedTask;

        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);
        logger.CatchUpJobIsResuming(jobId);
        _isPreparingCatchup = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Unlike <see cref="RegisterCatchingUpPartitions"/> this is not forward progress, so the consecutive-stranding
    /// count is left alone: a resume loop must not escape quarantine by registering again and again. The blanket
    /// hold comes down only after the per-partition holds are durable.
    /// </remarks>
    public async Task RegisterResumedCatchingUpPartitions(JobId jobId, IEnumerable<Key> partitions)
    {
        if (IsRetired || _removed || _concludedCatchUpJobs.Contains(jobId)) return;

        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);
        var registered = 0;
        foreach (var partition in partitions)
        {
            if (State.CatchingUpPartitions.Add(partition))
            {
                registered++;
            }
        }

        if (registered > 0)
        {
            _partitionHoldsVersion++;
        }

        logger.RegisteredResumedCatchingUpPartitions(jobId, registered);
        await WriteStateAsync();

        // The job may have concluded while the state was written; its handover owns the flag from then on.
        if (_concludedCatchUpJobs.Contains(jobId)) return;
        _isPreparingCatchup = false;
    }
}
