// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for querying a page of events out of an event sequence.
/// </summary>
[ProtoContract]
public class QueryEventsRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2, IsRequired = true)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3, IsRequired = true)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the criteria narrowing the events returned.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public EventSequenceQueryCriteria Criteria { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of matching events to skip before the page starts.
    /// </summary>
    [ProtoMember(5)]
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of events in the page.
    /// </summary>
    [ProtoMember(6)]
    public int Take { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to order from the newest event down rather than from the oldest up.
    /// </summary>
    [ProtoMember(7)]
    public bool Descending { get; set; }
}
