// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for getting events for an event source id and specific event types.
/// </summary>
[ProtoContract]
public class GetForEventSourceIdAndEventTypesRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the event source type.
    /// </summary>
    [ProtoMember(4)]
    public string EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(5)]
    public string EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    [ProtoMember(6)]
    public string EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the event stream type.
    /// </summary>
    [ProtoMember(7)]
    public string EventStreamId { get; set; }

    /// <summary>
    /// Gets or sets the event types to get.
    /// </summary>
    [ProtoMember(8, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];
}
