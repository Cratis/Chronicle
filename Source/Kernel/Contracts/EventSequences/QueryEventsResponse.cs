// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response for querying a page of events out of an event sequence.
/// </summary>
[ProtoContract]
public class QueryEventsResponse
{
    /// <summary>
    /// Gets or sets the events in the page.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<AppendedEvent> Events { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of events matching the criteria, across every page.
    /// </summary>
    [ProtoMember(2)]
    public ulong TotalCount { get; set; }
}
