// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
#pragma warning disable CA1819 // Allow arrays on properties

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Represents the state used for failed observers.
/// </summary>
public class FailedObserverPartition
{
    /// <summary>
    /// Gets the event source identifier.
    /// </summary>
    public EventSourceId EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceNumber"/> of the failure - if any.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the occurred time of the failure - if any.
    /// </summary>
    public DateTimeOffset Occurred { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the occurred time of the failure - if any.
    /// </summary>
    public DateTimeOffset LastAttempt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the number of retry attempts it has had.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the message from the failure - if any.
    /// </summary>
    public string[] Messages { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the stack trace from the failure - if any.
    /// </summary>
    public string StackTrace { get; set; } = string.Empty;
}
