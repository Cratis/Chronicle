// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Represents the transport-independent result of receiving a batch.
/// </summary>
/// <param name="Status">The delivery status.</param>
/// <param name="Partition">The partition.</param>
/// <param name="LastSuccessfulObservation">The last successfully observed sequence number.</param>
/// <param name="ExceptionMessages">The failure messages.</param>
/// <param name="ExceptionStackTrace">The failure stack trace.</param>
/// <param name="ReadModelState">The resulting reducer state as JSON.</param>
internal record DeliveryOutcome(
    DeliveryStatus Status,
    string Partition,
    EventSequenceNumber LastSuccessfulObservation,
    IReadOnlyList<string> ExceptionMessages,
    string ExceptionStackTrace,
    string? ReadModelState)
{
    /// <summary>
    /// Creates a rejected outcome.
    /// </summary>
    /// <param name="status">The rejection status.</param>
    /// <param name="reason">The safe rejection reason.</param>
    /// <returns>The rejected outcome.</returns>
    internal static DeliveryOutcome Rejected(DeliveryStatus status, string reason) => new(status, string.Empty, EventSequenceNumber.Unavailable, [reason], string.Empty, null);

    /// <summary>
    /// Creates a cancelled outcome.
    /// </summary>
    /// <param name="partition">The partition, if known.</param>
    /// <returns>The cancelled outcome.</returns>
    internal static DeliveryOutcome Cancelled(string partition) => new(DeliveryStatus.Cancelled, partition, EventSequenceNumber.Unavailable, [], string.Empty, null);

    /// <summary>
    /// Converts a reactor result.
    /// </summary>
    /// <param name="result">The dispatcher result.</param>
    /// <returns>The delivery outcome.</returns>
    internal static DeliveryOutcome From(ReactorResult result) => new(
        result.State == ObservationState.Success ? DeliveryStatus.Succeeded : DeliveryStatus.Failed,
        result.Partition,
        result.LastSuccessfulObservation,
        [.. result.ExceptionMessages],
        result.ExceptionStackTrace,
        null);

    /// <summary>
    /// Converts a reducer result.
    /// </summary>
    /// <param name="result">The dispatcher result.</param>
    /// <returns>The delivery outcome.</returns>
    internal static DeliveryOutcome From(ReducerResult result) => new(
        result.State == ObservationState.Success ? DeliveryStatus.Succeeded : DeliveryStatus.Failed,
        result.Partition,
        result.LastSuccessfulObservation,
        [.. result.ExceptionMessages],
        result.ExceptionStackTrace,
        result.ReadModelState);
}
