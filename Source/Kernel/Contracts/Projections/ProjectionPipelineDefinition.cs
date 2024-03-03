// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Projections;

/// <summary>
/// Represents the definition of a projection pipeline.
/// </summary>
[ProtoContract]
public class ProjectionPipelineDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the projection the pipeline is for.
    /// </summary>
    [ProtoMember(1)]
    public string ProjectionId { get; set; }

    /// <summary>
    /// Gets or sets the collection of <see cref="ProjectionSinkDefinition"/>.
    /// </summary>
    [ProtoMember(2)]
    public IEnumerable<ProjectionSinkDefinition> Sinks { get; set; }
}
