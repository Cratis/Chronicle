// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Kernel.Contracts.Projections;

/// <summary>
/// Represents the result of an immediate projection.
/// </summary>
[ProtoContract]
public class ImmediateProjectionResult
{
    /// <summary>
    /// The JSON representation of the model.
    /// </summary>
    [ProtoMember(1)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Collection of properties that was set.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IEnumerable<string> AffectedProperties { get; set; } = [];

    /// <summary>
    /// Number of events that caused projection.
    /// </summary>
    [ProtoMember(3)]
    public int ProjectedEventsCount { get; set; }
}
