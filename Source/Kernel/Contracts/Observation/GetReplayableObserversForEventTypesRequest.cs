// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the request for getting all replayable observers for specific event types.
/// </summary>
[ProtoContract]
public class GetReplayableObserversForEventTypesRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event types to get replayable observers for.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];
}
