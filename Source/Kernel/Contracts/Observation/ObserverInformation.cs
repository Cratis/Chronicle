// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the information about an observer.
/// </summary>
[ProtoContract]
public class ObserverInformation
{
    /// <summary>
    /// Gets or sets the unique identifier of the observer.
    /// </summary>
    [ProtoMember(1)]
    public Guid ObserverId { get; set; }

    /// <summary>
    /// Gets or sets the event sequence the observer is observing.
    /// </summary>
    [ProtoMember(2)]
    public Guid EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the type of observer.
    /// </summary>
    [ProtoMember(3)]
    public ObserverType Type { get; set; }

    /// <summary>
    /// Gets or sets the types of events the observer is observing.
    /// </summary>
    [ProtoMember(4)]
    public IEnumerable<EventType> EventTypes { get; set; }

    /// <summary>
    /// Gets or sets the next event sequence number the observer will observe.
    /// </summary>
    [ProtoMember(5)]
    public ulong NextEventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the running state of the observer.
    /// </summary>
    [ProtoMember(6)]
    public ObserverRunningState RunningState { get; set; }
}
