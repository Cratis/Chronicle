// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sinks;

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents the payload for the definition of a reducer.
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
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(2)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the event types the reducer is interested in.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventTypeWithKeyExpression> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the read model the reducer is for.
    /// </summary>
    [ProtoMember(4)]
    public string ReadModel { get; set; }

    /// <summary>
    /// Gets or sets whether or not the reducer is an actively observing reducer.
    /// </summary>
    [ProtoMember(5)]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the projection sink definition.
    /// </summary>
    [ProtoMember(6)]
    public SinkDefinition Sink { get; set; }

    /// <summary>
    /// Gets or sets the tags the reducer belongs to.
    /// </summary>
    [ProtoMember(7, IsRequired = true)]
    public IList<string> Tags { get; set; } = [];
}
