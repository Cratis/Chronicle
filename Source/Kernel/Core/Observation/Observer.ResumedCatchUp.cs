// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    CatchUpOwnership? _catchUpOwner;

    /// <inheritdoc/>
    /// <remarks>
    /// Raises the blanket hold only. The flag is in-memory, so nothing is written. It comes down again when the
    /// job registers the partitions it owns, or when catch-up concludes. The generation returned is what the job
    /// presents when it registers, so a registration from a job that no longer owns catch-up is recognized.
    /// </remarks>
    public Task<Guid> ResumingCatchUp(JobId jobId)
    {
        if (IsRetired || _removed || _concludedCatchUpJobs.Contains(jobId)) return Task.FromResult(Guid.Empty);

        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);
        logger.CatchUpJobIsResuming(jobId);
        _isPreparingCatchup = true;
        return Task.FromResult(MintCatchUpOwnership(jobId));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Unlike <see cref="RegisterCatchingUpPartitions"/> this is not forward progress, so the consecutive-stranding
    /// count is left alone: a resume loop must not escape quarantine by registering again and again. The blanket
    /// hold comes down only after the per-partition holds are durable, and only while they are still installed.
    /// </remarks>
    public async Task RegisterResumedCatchingUpPartitions(JobId jobId, Guid ownershipGeneration, IEnumerable<Key> partitions)
    {
        if (IsRetired || _removed || !IsCurrentCatchUpOwner(jobId, ownershipGeneration))
        {
            logger.IgnoringStaleCatchUpRegistration(jobId);
            return;
        }

        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);

        // A partition that failed since the job planned its steps belongs to its recovery episode, not to catch-up.
        var accepted = partitions.Where(partition => !Failures.TryGet(partition, out _)).ToArray();
        var registered = 0;
        foreach (var partition in accepted)
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

        // Conclusion, a stop or a successor owns the flag from here on.
        if (!IsCurrentCatchUpOwner(jobId, ownershipGeneration))
        {
            logger.CatchUpOwnershipLostDuringRegistration(jobId);
            return;
        }

        // Routing releases holds nobody owns; the blanket hold only comes down while the exact ones are in place.
        if (!accepted.All(State.CatchingUpPartitions.Contains))
        {
            logger.CatchUpHoldsLostDuringRegistration(jobId);
            return;
        }

        _isPreparingCatchup = false;
    }

    Guid MintCatchUpOwnership(JobId jobId)
    {
        var generation = Guid.NewGuid();
        _catchUpOwner = new(jobId, generation);
        _catchUpOwnershipEpoch++;
        return generation;
    }

    void EndCatchUpOwnership(JobId jobId)
    {
        if (jobId == JobId.NotSet || _catchUpOwner?.JobId == jobId)
        {
            _catchUpOwner = null;
        }
    }

    void InvalidateCatchUpOwnership() => _catchUpOwner = null;

    bool IsCurrentCatchUpOwner(JobId jobId, Guid generation) =>
        generation != Guid.Empty && _catchUpOwner is { } owner && owner.JobId == jobId && owner.Generation == generation;

    sealed record CatchUpOwnership(JobId JobId, Guid Generation);
}
