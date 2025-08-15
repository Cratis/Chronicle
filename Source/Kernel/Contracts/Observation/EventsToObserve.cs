// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the payload when observing an event sequence.
/// </summary>
[ProtoContract]
public class EventsToObserve
{
    /// <summary>
    /// Gets or sets the partition to observe.
    /// </summary>
    [ProtoMember(1)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets a collection of <see cref="AppendedEvent"/>.
    /// </summary>
    [ProtoMember(2)]
    public IList<AppendedEvent> Events { get; set; }
}
