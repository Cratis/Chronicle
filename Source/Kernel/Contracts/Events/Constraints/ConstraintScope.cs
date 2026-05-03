// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents the scope of a constraint.
/// </summary>
[ProtoContract]
public class ConstraintScope
{
    /// <summary>
    /// Gets or sets the optional event source type to scope to.
    /// </summary>
    [ProtoMember(1)]
    public string? EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the optional event stream type to scope to.
    /// </summary>
    [ProtoMember(2)]
    public string? EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the optional event stream identifier to scope to.
    /// </summary>
    [ProtoMember(3)]
    public string? EventStreamId { get; set; }
}
