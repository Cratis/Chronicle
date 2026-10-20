// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents a failed partition in a bulk operation.
/// </summary>
/// <param name="EventSourceId">The event source identifier for the failed partition.</param>
/// <param name="EventSequenceNumber">The sequence number of the first event that failed.</param>
public record FailedPartition(Key EventSourceId, EventSequenceNumber EventSequenceNumber)
{
    /// <summary>
    /// Gets why the partition failed, as reported by the underlying store, or empty when the store gave no reason.
    /// </summary>
    /// <remarks>
    /// Carries error codes and the store's own error message only - never the content of the document being written,
    /// which can hold personal data.
    /// </remarks>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the store could not say whether the partition's writes were applied.
    /// </summary>
    /// <remarks>
    /// An ordinary write failure is known not to have been applied, and is retried from the event that failed. When the
    /// outcome is unknown - the write raised an error other than a per-document write failure, or a deadline cut it
    /// short - some of the partition's writes may have been applied and others lost, so progress past them cannot be
    /// trusted.
    /// </remarks>
    public bool OutcomeUnknown { get; init; }
}
