// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

#pragma warning disable RCS1075 // Shadow diagnostics are write-only: a failing lookup must never affect the observer

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static bool IsStaleObserverWideRelease(ObserverWideClaim? claim, JobId job, ulong? epoch) =>
        claim is { Holder: ClaimHolderKind.ObserverCatchUp } &&
        (claim.Job is not null ? claim.Job != job : epoch is not null && epoch != claim.Epoch.Value);

    /// <summary>
    /// Releases the observer-wide catch-up claim only when the job reporting back is its current holder.
    /// </summary>
    /// <param name="job">The <see cref="JobId"/> of the catch-up job reporting back.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A claim is granted and persisted before its job starts, and attached to the job only once the start returns.
    /// In that window the claim names no job, so the reporting job's own request tells whether it was started under
    /// this claim: an older epoch means the claim already belongs to newer work, and releasing it would refuse that
    /// work at its next stage. The lookup is awaited before the claim is read, so the check sees the current claim.
    /// </remarks>
    async Task ShadowHandOverCaughtUpObserverWide(JobId job)
    {
        var epoch = _shadowClaims.ObserverWide is { Holder: ClaimHolderKind.ObserverCatchUp, Job: null }
            ? await CatchUpJobOwnershipEpoch(job)
            : null;

        GuardShadowClaims(() =>
        {
            if (IsStaleObserverWideRelease(_shadowClaims.ObserverWide, job, epoch))
            {
                logger.IgnoredStaleObserverWideClaimRelease(_observerId, job);
                return;
            }

            _shadowClaims.HandOverObserverWide(ClaimHolderKind.ObserverCatchUp, job);
        });
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
