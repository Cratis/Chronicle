// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for registering projections.
/// </summary>
[ProtoContract]
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the owner of the projection.
    /// </summary>
    [ProtoMember(2)]
    public ProjectionOwner Owner { get; set; } = ProjectionOwner.None;

    /// <summary>
    /// Gets or sets the collection of <see cref="ProjectionDefinition"/> instances to register.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<ProjectionDefinition> Projections { get; set; } = [];

    /// <summary>
    /// Gets or sets whether <see cref="Projections"/> is the complete set of projections for <see cref="Owner"/>.
    /// When <see langword="true"/>, any registered projection with the same owner that is not in the set is retired:
    /// its observer stops consuming events and its definition is removed, while its sink container is left untouched.
    /// Leave <see langword="false"/> for a partial registration, which never retires anything - including when the
    /// client could not build a definition for every discovered artifact.
    /// </summary>
    [ProtoMember(4)]
    public bool FullSet { get; set; }
}
