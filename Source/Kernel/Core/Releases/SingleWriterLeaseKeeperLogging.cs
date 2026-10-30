// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.SingleWriter;

internal static partial class SingleWriterLeaseKeeperLogging
{
    [LoggerMessage(LogLevel.Information, "Acquiring single-writer lease {Owner}")]
    internal static partial void Acquiring(this ILogger<SingleWriterLeaseKeeper> logger, string owner);

    [LoggerMessage(LogLevel.Information, "Waiting for single-writer lease held by {Owner} on {HostName}, version {Version}, expires {ExpiresAt}")]
    internal static partial void WaitingForLease(this ILogger<SingleWriterLeaseKeeper> logger, string? owner, string? hostName, string? version, DateTimeOffset? expiresAt);

    [LoggerMessage(LogLevel.Information, "Single-writer lease acquired after {Waited}")]
    internal static partial void Acquired(this ILogger<SingleWriterLeaseKeeper> logger, TimeSpan waited);

    [LoggerMessage(LogLevel.Error, "Single-writer lease was not acquired before the startup timeout")]
    internal static partial void NotAcquired(this ILogger<SingleWriterLeaseKeeper> logger);

    [LoggerMessage(LogLevel.Warning, "Single-writer lease storage operation failed; retrying")]
    internal static partial void RenewalFailed(this ILogger<SingleWriterLeaseKeeper> logger, Exception exception);

    [LoggerMessage(LogLevel.Critical, "Single-writer lease lost for {Owner}; stopping before storage can admit another writer")]
    internal static partial void LeaseLost(this ILogger<SingleWriterLeaseKeeper> logger, string owner);

    [LoggerMessage(LogLevel.Information, "Single-writer lease released")]
    internal static partial void Released(this ILogger<SingleWriterLeaseKeeper> logger);

    [LoggerMessage(LogLevel.Warning, "Could not release single-writer lease; successor will wait for expiry")]
    internal static partial void ReleaseFailed(this ILogger<SingleWriterLeaseKeeper> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Single-writer lease half TTL {HalfTtl} does not exceed host shutdown timeout {ShutdownTimeout}")]
    internal static partial void TtlShorterThanShutdown(this ILogger<SingleWriterLeaseKeeper> logger, TimeSpan halfTtl, TimeSpan shutdownTimeout);
}
