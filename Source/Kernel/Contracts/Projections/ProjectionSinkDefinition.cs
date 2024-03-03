// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Kernel.Contracts.Projections;

/// <summary>
/// Represents the definition of where to store results from a projection.
/// </summary>
[ProtoContract]
public class ProjectionSinkDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the configuration.
    /// </summary>
    [ProtoMember(1)]
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the type of store.
    /// </summary>
    [ProtoMember(2)]
    public Guid TypeId { get; set; }
}
