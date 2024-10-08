// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the request for rewinding a partition in an observer.
/// </summary>
[ProtoContract]
public class RewindPartitionRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the observer identifier.
    /// </summary>
    [ProtoMember(3)]
    public Guid ObserverId { get; set; }

    /// <summary>
    /// Gets or sets the partition to rewind.
    /// </summary>
    [ProtoMember(4)]
    public string Partition { get; set; }
}
