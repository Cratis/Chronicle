// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents a combination of a projection and a pipeline.
/// </summary>
[ProtoContract]
public class ProjectionAndPipeline
{
    /// <summary>
    /// Gets or sets the <see cref="ProjectionDefinition"/>.
    /// </summary>
    [ProtoMember(1)]
    public ProjectionDefinition Projection { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ProjectionPipelineDefinition"/>.
    /// </summary>
    [ProtoMember(2)]
    public ProjectionPipelineDefinition Pipeline { get; set; }
}
