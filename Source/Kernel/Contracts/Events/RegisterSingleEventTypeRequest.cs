// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents a request for registering a single event type to an event store.
/// </summary>
[ProtoContract]
public class RegisterSingleEventTypeRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the event type to register.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public EventTypeRegistration Type { get; set; } = new EventTypeRegistration();
}
