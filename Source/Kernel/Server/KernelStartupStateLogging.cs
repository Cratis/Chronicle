// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

internal static partial class KernelStartupStateLogging
{
    [LoggerMessage(LogLevel.Warning, "Startup has not completed {HeldFor} after taking the single-writer lease, exceeding Clustering.SingleWriterLease.StandbyStartupTimeout ({Timeout}). Readiness now reports unhealthy instead of standby; check the startup logs for the step that has not completed")]
    internal static partial void StandbyStartupTimedOut(this ILogger<KernelStartupState> logger, TimeSpan timeout, TimeSpan heldFor);
}
