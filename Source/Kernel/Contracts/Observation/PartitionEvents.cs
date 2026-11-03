// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the events of one partition within a page of events delivered to an observer.
/// </summary>
[ProtoContract]
public class PartitionEvents
{
    /// <summary>
    /// Gets or sets the partition the events belong to.
    /// </summary>
    [ProtoMember(1)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets the events of the partition, in sequence order.
    /// </summary>
    [ProtoMember(2)]
    public IList<AppendedEvent> Events { get; set; } = [];
}
