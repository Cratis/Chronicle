// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Represents an event sequence query a user saved so it can be reopened later.
/// </summary>
[ProtoContract]
public class SequenceQueryDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the query.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name the user gave the query.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who the query is visible to.
    /// </summary>
    [ProtoMember(3)]
    public SequenceQueryScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the identity that saved the query.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace the query runs against.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence the query runs against.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source to narrow to. Empty means every event source.
    /// </summary>
    [ProtoMember(7, IsRequired = true)]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type identifiers to narrow to. Empty means every event type.
    /// </summary>
    [ProtoMember(8, IsRequired = true)]
    public IList<string> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags to narrow to - an event matches when it carries any of them. Empty means every event.
    /// </summary>
    [ProtoMember(9, IsRequired = true)]
    public IList<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the inclusive lower bound on when the event occurred. Null means unbounded.
    /// </summary>
    [ProtoMember(10)]
    public DateTimeOffset? OccurredFrom { get; set; }

    /// <summary>
    /// Gets or sets the exclusive upper bound on when the event occurred. Null means unbounded.
    /// </summary>
    [ProtoMember(11)]
    public DateTimeOffset? OccurredTo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether results are ordered newest first.
    /// </summary>
    [ProtoMember(12)]
    public bool Descending { get; set; }
}
