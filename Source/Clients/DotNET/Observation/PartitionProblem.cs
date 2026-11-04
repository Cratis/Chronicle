// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the first event of a partition that one method of an observer did not handle.
/// </summary>
/// <param name="Partition">The partition.</param>
/// <param name="At">The sequence number of the event.</param>
/// <param name="State">The <see cref="ObservationState"/>: failed or deferred.</param>
/// <param name="ExceptionMessages">The exception messages of a failure.</param>
/// <param name="ExceptionStackTrace">The exception stack trace of a failure.</param>
/// <param name="RetryAfter">How long to wait before delivering again, for a deferral.</param>
internal sealed record PartitionProblem(
    string Partition,
    EventSequenceNumber At,
    ObservationState State,
    IEnumerable<string> ExceptionMessages,
    string ExceptionStackTrace,
    TimeSpan RetryAfter)
{
    /// <summary>
    /// Creates a failure.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="at">The sequence number of the event it failed at.</param>
    /// <param name="exception">The <see cref="Exception"/> describing it.</param>
    /// <returns>The <see cref="PartitionProblem"/>.</returns>
    public static PartitionProblem Failed(string partition, EventSequenceNumber at, Exception exception) =>
        new(partition, at, ObservationState.Failed, exception.GetAllMessages(), exception.StackTrace ?? string.Empty, TimeSpan.Zero);
}
