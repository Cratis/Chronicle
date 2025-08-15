// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
///  Represents the request for getting all partitions.
/// </summary>
[ProtoContract]
public class GetFailedPartitionsRequest
{
    /// <summary>
    /// The event store to get failed partitions for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// The namespace to get failed partitions for.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Optional observer id to filter down which observer it is for.
    /// </summary>
    [ProtoMember(3)]
    public string? ObserverId { get; set; }
}
