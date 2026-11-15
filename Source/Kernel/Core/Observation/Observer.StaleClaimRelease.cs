// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

#pragma warning disable RCS1075 // Shadow diagnostics are write-only: a failing lookup must never affect the observer

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Gets whether a job reporting back was started under a claim that has since passed to newer work, logging it when so.
    /// </summary>
    /// <param name="holder">The <see cref="ClaimHolderKind"/> the reporting job runs as.</param>
    /// <param name="partition">The partition it worked on, or null for observer-wide work.</param>
    /// <param name="ownershipEpoch">The epoch the job was started under, or null when it carries none.</param>
    /// <param name="job">The reporting <see cref="JobId"/>, or null when it is not known.</param>
    /// <returns>True when the report must not release anything; false to handle it as before.</returns>
    /// <remarks>
    /// A newer job of the same kind is admitted only while its epoch is the claim's. Letting an older job's report
    /// release that claim, or end the work the claim covers, refuses the newer job at its next stage and nothing
    /// finishes the work. A report carrying no epoch, or for a scope no claim of its kind holds, is handled as before.
    /// </remarks>
    bool IgnoresStaleRelease(ClaimHolderKind holder, Key? partition, ulong? ownershipEpoch, JobId? job = null)
    {
        var stale = false;
        GuardShadowClaims(() => stale = _shadowClaims.IsHeldByOtherWork(holder, partition, job, ownershipEpoch));
        if (stale)
        {
            logger.IgnoredStaleClaimRelease(_observerId, holder, partition?.ToString(), ownershipEpoch);
        }

        return stale;
    }

    /// <summary>
    /// Releases the observer-wide catch-up claim only when the job reporting back is its current holder.
    /// </summary>
    /// <param name="job">The <see cref="JobId"/> of the catch-up job reporting back.</param>
    /// <returns>True when the reporting job may conclude catch-up; false when the claim belongs to newer work and the report must change nothing.</returns>
    /// <remarks>
    /// A claim is granted and persisted before its job starts, and attached to the job only once the start returns.
    /// In that window the claim names no job, so the reporting job's own request tells whether it was started under
    /// this claim. The lookup is awaited before the claim is read, so the check sees the current claim.
    /// </remarks>
    async Task<bool> ShadowHandOverCaughtUpObserverWide(JobId job)
    {
        var epoch = _shadowClaims.ObserverWide is { Holder: ClaimHolderKind.ObserverCatchUp, Job: null }
            ? await CatchUpJobOwnershipEpoch(job)
            : null;

        var reporting = job == JobId.NotSet ? null : job;
        if (IgnoresStaleRelease(ClaimHolderKind.ObserverCatchUp, null, epoch, reporting)) return false;
        GuardShadowClaims(() => _shadowClaims.HandOverObserverWide(ClaimHolderKind.ObserverCatchUp, job));
        return true;
    }

    async Task<ulong?> CatchUpJobOwnershipEpoch(JobId job)
    {
        if (job == JobId.NotSet) return null;
        try
        {
            var result = await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Jobs.GetJob(job);
            return result.TryGetResult(out var state) && state.Request is CatchUpObserverRequest request
                ? request.OwnershipEpoch
                : null;
        }
        catch (Exception exception)
        {
            logger.ShadowClaimsFailed(_observerId, exception);
            return null;
        }
    }
}
