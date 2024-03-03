// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections.Definitions;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents the message for a projection that has changed its definition.
/// </summary>
/// <param name="RuntimeIdentity">The Orleans Silo runtime identity the message was published from.</param>
/// <param name="MicroserviceId">The <see cref="MicroserviceId"/> for the projection.</param>
/// <param name="Projection">The <see cref="ProjectionDefinition"/> for the changed projection.</param>
/// <param name="Pipeline">The <see cref="ProjectionPipelineDefinition"/> for the changed projection.</param>
/// <param name="IsNew">Whether or not the projection is new.</param>
public record ProjectionChanged(string RuntimeIdentity, MicroserviceId MicroserviceId, ProjectionDefinition Projection, ProjectionPipelineDefinition Pipeline, bool IsNew);
