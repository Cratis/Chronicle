// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the definition of an event store subscription.
/// </summary>
[ProtoContract]
public class EventStoreSubscriptionDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the subscription.
    /// </summary>
    [ProtoMember(1)]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the source event store name whose outbox to subscribe to.
    /// </summary>
    [ProtoMember(2)]
    public string SourceEventStore { get; set; }

    /// <summary>
    /// Gets or sets the collection of event types to subscribe to.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventType> EventTypes { get; set; } = [];
}
