// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a request for registering a collection of constraints to an event store.
/// </summary>
[ProtoContract]
public class RegisterConstraintsRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the collection of constraints to register.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IList<Constraint> Constraints { get; set; } = [];
}
