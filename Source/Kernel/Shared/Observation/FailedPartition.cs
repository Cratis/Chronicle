// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Observation;

public class FailedPartition
{
    public FailedPartition(EventSourceId eventSourceId, EventSequenceNumber tail, DateTimeOffset? occurred = null)
    {
        Partition = eventSourceId;
        Tail = tail;
        Occurred = occurred ?? DateTimeOffset.UtcNow;
    }

    public EventSourceId Partition { get; }

    /// <summary>
    /// <para>Gets the <see cref="EventSequenceNumber"/> of the event that triggered the partition failure.</para>
    /// <para>The partition will be retried from this event. If this event succeeds but subsequent events fail, the partition will be retried from the last failed event
    /// but the Tail will continue to be reported as the event that first sparked a failure. This will be removed when the partition has successfully caught up.</para>
    /// 
    /// </summary>
    public EventSequenceNumber Tail { get; init; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets the occurred time of the failure.
    /// </summary>
    public DateTimeOffset Occurred { get; }
    
    /// <summary>
    /// Gets or sets the Head of the partition.
    /// </summary>
    public EventSequenceNumber? Head { get; set; }
    
    /// <summary>
    /// Gets the Event Log Sequence Number of the event where the recovery process believes it has caught up.
    /// This is set by <see cref="IRecoverFailedPartition"/> when it has successfully processed the event that triggered the failure
    /// and all subsequent events in the parition until it has caught up with the Head..
    /// </summary>
    public EventSequenceNumber? RecoveredTo { get; private set; }
    
    /// <summary>
    /// Indicates whether the partition has been recovered.
    /// A recovered partition is one that has been successfully processed from the event that triggered the failure to the Head for the partition.
    /// </summary>
    public bool IsRecovered => Head is null || Head.Value <= (RecoveredTo?.Value ?? EventSequenceNumber.First);

    public bool SetRecoveredTo(EventSequenceNumber sequenceNumber)
    {
        RecoveredTo = sequenceNumber;
        return IsRecovered;
    }
}