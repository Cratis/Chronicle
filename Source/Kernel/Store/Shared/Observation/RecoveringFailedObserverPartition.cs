// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
#pragma warning disable CA1819 // Allow arrays on properties

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Represents the state used when recovering a failed observer partition.
/// </summary>
public class RecoveringFailedObserverPartition
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
    public DateTimeOffset StartedRecoveryAt { get; set; } = DateTimeOffset.UtcNow;
}
