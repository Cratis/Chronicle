// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents the events and current read model state of one partition within a page delivered to a reducer.
/// </summary>
[ProtoContract]
public class ReducerPartition
{
    /// <summary>
    /// Gets or sets the partition the events belong to.
    /// </summary>
    [ProtoMember(1)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets the current read model state of the partition as JSON, if any.
    /// </summary>
    [ProtoMember(2)]
    public string? InitialState { get; set; }

    /// <summary>
    /// Gets or sets the events of the partition, in sequence order.
    /// </summary>
    [ProtoMember(3)]
    public IList<AppendedEvent> Events { get; set; } = [];
}
