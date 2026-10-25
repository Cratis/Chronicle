// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class ResumedCatchUpLogging
{
    [LoggerMessage(LogLevel.Debug, "Catch-up job {JobId} is resuming; holding back live delivery until its partitions are registered")]
    internal static partial void CatchUpJobIsResuming(this ILogger<Observer> logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Catch-up job {JobId} registered {Count} partitions it owns")]
    internal static partial void RegisteredResumedCatchingUpPartitions(this ILogger<Observer> logger, JobId jobId, int count);
}
