// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Claims;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Provides diagnostic logging for shadow ownership claims.
/// </summary>
internal static partial class ShadowClaimsLogging
{
    /// <summary>
    /// Reports a shadow claim disagreement without affecting delivery.
    /// </summary>
    /// <param name="logger">The observer logger.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="divergence">The disagreement kind.</param>
    /// <param name="partition">The partition, or null for observer-wide work.</param>
    [LoggerMessage(LogLevel.Information, "Observer {ObserverId} shadow claim divergence {Divergence} for partition {Partition}")]
    internal static partial void ShadowClaimDivergence(this ILogger<Observer> logger, ObserverId observerId, ShadowClaimDivergence divergence, string? partition);

    /// <summary>
    /// Reports restoration of state predating the shadow claim protocol.
    /// </summary>
    /// <param name="logger">The observer logger.</param>
    /// <param name="observerId">The observer identifier.</param>
    [LoggerMessage(LogLevel.Debug, "Observer {ObserverId} shadow claims seeded from legacy state")]
    internal static partial void ShadowClaimsSeededFromLegacyState(this ILogger<Observer> logger, ObserverId observerId);

    /// <summary>
    /// Logs a shadow diagnostic failure without changing delivery behavior.
    /// </summary>
    /// <param name="logger">The observer logger.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="exception">The diagnostic failure.</param>
    [LoggerMessage(LogLevel.Warning, "Observer {ObserverId} shadow claims failed; delivery is unchanged")]
    internal static partial void ShadowClaimsFailed(this ILogger<Observer> logger, ObserverId observerId, Exception exception);

    /// <summary>
    /// Logs that persisted shadow claims could not be restored, so the shadow ledger starts empty.
    /// </summary>
    /// <param name="logger">The observer logger.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="exception">The restore failure.</param>
    [LoggerMessage(LogLevel.Warning, "Observer {ObserverId} could not restore its shadow claims; the shadow ledger starts empty and delivery is unchanged")]
    internal static partial void ShadowClaimsRestoreFailed(this ILogger<Observer> logger, ObserverId observerId, Exception exception);

    /// <summary>
    /// Logs that a catch-up job reported back after its observer-wide claim had passed to newer work, so the claim was kept.
    /// </summary>
    /// <param name="logger">The observer logger.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="jobId">The stale job reporting back.</param>
    [LoggerMessage(LogLevel.Information, "Observer {ObserverId} ignored a stale observer-wide claim release from catch-up job {JobId}; the claim belongs to newer work")]
    internal static partial void IgnoredStaleObserverWideClaimRelease(this ILogger<Observer> logger, ObserverId observerId, JobId jobId);
}
