// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Logs shutdown outcomes of observer work.
/// </summary>
internal static partial class ObserverServiceLogging
{
    /// <summary>
    /// Logs unfinished observer work.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="observer">The observer.</param>
    [LoggerMessage(LogLevel.Error, "Shutdown drain deadline passed for observer {Observer}")]
    internal static partial void DrainDeadlinePassed(this ILogger<ObserverService> logger, ObserverKey observer);

    /// <summary>
    /// Logs the exact partition and earliest event with an unconfirmed write.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="observer">The observer.</param>
    /// <param name="partition">The partition.</param>
    /// <param name="sequenceNumber">The earliest unconfirmed event.</param>
    /// <param name="reason">The failure reason.</param>
    [LoggerMessage(LogLevel.Error, "Shutdown bulk outcome failed or unknown for observer {Observer}, partition {Partition}, sequence number {SequenceNumber}: {Reason}")]
    internal static partial void DrainPartitionFailed(this ILogger<ObserverService> logger, ObserverKey observer, Key partition, EventSequenceNumber sequenceNumber, string reason);

    /// <summary>
    /// Logs a drain or failure-recording error without blocking shutdown.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="observer">The observer.</param>
    [LoggerMessage(LogLevel.Error, "Could not finish shutdown drain or record failed partitions for observer {Observer}")]
    internal static partial void DrainFailed(this ILogger<ObserverService> logger, Exception exception, ObserverKey observer);
}
