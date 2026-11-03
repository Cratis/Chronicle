// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the outcome of observing the events of one partition of a delivery.
/// </summary>
/// <param name="Partition">The partition the outcome is for.</param>
/// <param name="State">The <see cref="ObservationState"/> of the partition.</param>
/// <param name="LastSuccessfulObservation">The last event of the partition that was observed successfully.</param>
/// <param name="ExceptionMessages">The exception messages when the partition failed.</param>
/// <param name="ExceptionStackTrace">The exception stack trace when the partition failed.</param>
internal sealed record PartitionOutcome(
    string Partition,
    ObservationState State,
    EventSequenceNumber LastSuccessfulObservation,
    IEnumerable<string> ExceptionMessages,
    string ExceptionStackTrace)
{
    /// <summary>
    /// Gets how long to wait before the partition is delivered again, when it was deferred.
    /// </summary>
    public TimeSpan RetryAfter { get; init; }

    /// <summary>
    /// Gets the reduced read model state as JSON, for reducers.
    /// </summary>
    public string? ReadModelState { get; init; }

    /// <summary>
    /// Gets the sequence number of the event the partition failed at, or <see cref="EventSequenceNumber.Unavailable"/>.
    /// </summary>
    public EventSequenceNumber FailedAt { get; init; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Creates an outcome where every event of the partition failed.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="exception">The <see cref="Exception"/> that failed it.</param>
    /// <returns>A failed <see cref="PartitionOutcome"/>.</returns>
    public static PartitionOutcome Failed(string partition, Exception exception) =>
        new(partition, ObservationState.Failed, EventSequenceNumber.Unavailable, exception.GetAllMessages(), exception.StackTrace ?? string.Empty);

    /// <summary>
    /// Converts the outcome to a <see cref="PartitionResult"/>.
    /// </summary>
    /// <returns>The <see cref="PartitionResult"/>.</returns>
    public PartitionResult ToContract() => new()
    {
        Partition = Partition,
        State = State,
        LastSuccessfulObservation = LastSuccessfulObservation,
        ExceptionMessages = [.. ExceptionMessages],
        ExceptionStackTrace = ExceptionStackTrace,
        RetryAfterMilliseconds = (ulong)Math.Max(0, RetryAfter.TotalMilliseconds),
        ReadModelState = ReadModelState
    };
}
