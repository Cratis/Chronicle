// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents a projection along with its DSL representation.
/// </summary>
[ProtoContract]
public class ProjectionWithDsl
{
    /// <summary>
    /// Gets or sets the identifier of the projection.
    /// </summary>
    [ProtoMember(1)]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model the projection projects to.
    /// </summary>
    [ProtoMember(2)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DSL representation of the projection.
    /// </summary>
    [ProtoMember(3)]
    public string Dsl { get; set; } = string.Empty;
}
