// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections.Api;

/// <summary>
/// Represents a single projection registration.
/// </summary>
/// <param name="Projection"><see cref="ProjectionDefinition"/> to register.</param>
/// <param name="Pipeline"><see cref="ProjectionPipelineDefinition"/> to associate.</param>
public record ProjectionRegistration(ProjectionDefinition Projection, ProjectionPipelineDefinition Pipeline);
