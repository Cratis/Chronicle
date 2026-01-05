// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Reactors;

/// <summary>
/// Represents the definition of a reactor.
/// </summary>
[ProtoContract]
public class ReactorDefinition
{
    /// <summary>
    /// Gets or sets the observer identifier.
    /// </summary>
    [ProtoMember(1)]
    public string ReactorId { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(2)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets a collection of event types to observe.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventTypeWithKeyExpression> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the reactor supports replay scenarios.
    /// </summary>
    [ProtoMember(4), DefaultValue(true)]
    public bool IsReplayable { get; set; } = true;

    /// <summary>
    /// Gets or sets the tags the reactor belongs to.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public IList<string> Tags { get; set; } = [];
}
