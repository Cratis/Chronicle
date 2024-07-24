// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Models;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents the payload for registering an observer.
/// </summary>
[ProtoContract]
public class RegisterReducer
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    [ProtoMember(1)]
    public string ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(2)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(3)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ReducerId"/>.
    /// </summary>
    [ProtoMember(5)]
    public string ReducerId { get; set; }

    /// <summary>
    /// Gets or sets the event types the reducer is interested in.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IList<EventTypeWithKeyExpression> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="ModelDefinition"/> of the read model.
    /// </summary>
    [ProtoMember(7)]
    public ModelDefinition ReadModel { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="SinkTypeId"/> of the target sink.
    /// </summary>
    [ProtoMember(8)]
    public Guid SinkTypeId { get; set; }
}
