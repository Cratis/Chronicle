// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Defines the result of a change as the result of a projection.
/// </summary>
[ProtoContract]
public class ProjectionChangeset
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(1)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the key for the model that was affected.
    /// </summary>
    [ProtoMember(2)]
    public string ModelKey { get; set; }

    /// <summary>
    /// Gets or sets the model that was affected as JSON.
    /// </summary>
    [ProtoMember(3)]
    public string Model { get; set; }
}
