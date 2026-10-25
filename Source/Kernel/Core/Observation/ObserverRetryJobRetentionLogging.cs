// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverRetryJobRetentionLogging
{
    [LoggerMessage(LogLevel.Information, "Dropping retry job {JobId} for partition {Partition} because it can never run")]
    internal static partial void DroppingRetryJobThatCanNeverRun(this ILogger<Observer> logger, JobId jobId, Key partition);

    [LoggerMessage(LogLevel.Warning, "Could not look up retry jobs that can never run")]
    internal static partial void CouldNotLookUpRetryJobsThatCanNeverRun(this ILogger<Observer> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Could not drop retry job {JobId} that can never run")]
    internal static partial void CouldNotDropRetryJobThatCanNeverRun(this ILogger<Observer> logger, JobId jobId, Exception exception);
}
