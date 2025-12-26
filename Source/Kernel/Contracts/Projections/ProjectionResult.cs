// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the result of performing a projection.
/// </summary>
[ProtoContract]
public class ProjectionResult
{
    /// <summary>
    /// Gets or sets the result of the projection as JSON.
    /// </summary>
    [ProtoMember(1)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of projected events.
    /// </summary>
    [ProtoMember(2)]
    public int ProjectedEventsCount { get; set; }

    /// <summary>
    /// Gets or sets the last handled event sequence number.
    /// </summary>
    [ProtoMember(3)]
    public ulong LastHandledEventSequenceNumber { get; set; }
}
