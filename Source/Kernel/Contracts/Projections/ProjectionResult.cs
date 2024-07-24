// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the result of performing a projection.
/// </summary>
[ProtoContract]
public class ProjectionResult
{
    /// <summary>
    /// Gets or sets the result of the projection.
    /// </summary>
    [ProtoMember(1)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected properties.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IEnumerable<string> AffectedProperties { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of projected events.
    /// </summary>
    [ProtoMember(3)]
    public int ProjectedEventsCount { get; set; }
}
