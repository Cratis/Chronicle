// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the observer's ownership claims as seen by the code starting its jobs: the observer itself and its states.
/// </summary>
/// <remarks>
/// Not a grain interface: the observer's states reach it through the state machine they run in, within the
/// observer's own activation, never across grains.
/// A claim is granted and persisted before the job is started, and its epoch is stamped on the job's request so
/// <see cref="ObserverOwnershipEpochRule"/> can refuse the job once the claim has moved on.
/// </remarks>
public interface IObserverOwnershipClaims
{
    /// <summary>
    /// Grants and persists an observer-wide claim for a job about to be started.
    /// </summary>
    /// <param name="holder">The <see cref="ClaimHolderKind"/> of the job.</param>
    /// <param name="resumePosition">The position the job resumes from.</param>
    /// <returns>The epoch to stamp on the job's request, or null if no claim was persisted and the job runs unchecked.</returns>
    Task<ulong?> GrantObserverWideBeforeStart(ClaimHolderKind holder, EventSequenceNumber resumePosition);

    /// <summary>
    /// Holds partitions, grants and persists a claim for each, in one write, for the jobs about to be started for them.
    /// </summary>
    /// <param name="partitions">The partitions.</param>
    /// <param name="holder">The <see cref="ClaimHolderKind"/> of the jobs.</param>
    /// <param name="resumePosition">The position the jobs resume from.</param>
    /// <returns>The epoch to stamp on each partition's request; null for a partition whose job runs unchecked.</returns>
    Task<IReadOnlyDictionary<Key, ulong?>> GrantPartitionsBeforeStart(IEnumerable<Key> partitions, ClaimHolderKind holder, EventSequenceNumber resumePosition);

    /// <summary>
    /// Records the job started under the observer-wide claim with the given epoch, persisted by the next write.
    /// </summary>
    /// <param name="epoch">The epoch the job was started under.</param>
    /// <param name="job">The <see cref="JobId"/> of the job.</param>
    void AttachObserverWideJob(ulong epoch, JobId job);

    /// <summary>
    /// Gets whether a job's request carries an epoch its scope's persisted claim no longer holds.
    /// </summary>
    /// <param name="request">The <see cref="IObserverJobRequest"/>.</param>
    /// <returns>True if the job will be refused when it starts or resumes.</returns>
    bool IsObsolete(IObserverJobRequest request);

    /// <summary>
    /// Gets whether a partition is still catching up under a partition catch-up claim granted after the one with the given epoch.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="epoch">The epoch a refused catch-up start carried.</param>
    /// <returns>True if a later catch-up grant owns the partition, so the refused start's work is not lost; a newer claim of another kind does not cover it.</returns>
    bool IsHeldByNewerClaim(Key partition, ulong? epoch);
}
