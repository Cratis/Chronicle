// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Models;

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents the definition of a reducer.
/// </summary>
[ProtoContract]
public class ReducerDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="ReducerId"/>.
    /// </summary>
    [ProtoMember(1)]
    public string ReducerId { get; set; }

    /// <summary>
    /// Gets or sets the event sequence the reducer is for.
    /// </summary>
    [ProtoMember(2)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the event types the reducer is interested in.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventTypeWithKeyExpression> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="ModelDefinition"/> of the read model.
    /// </summary>
    [ProtoMember(4)]
    public ModelDefinition ReadModel { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="SinkTypeId"/> of the target sink.
    /// </summary>
    [ProtoMember(5)]
    public Guid SinkTypeId { get; set; }
}
