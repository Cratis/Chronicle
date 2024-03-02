// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Kernel.Domain.Projections;

/// <summary>
/// Represents a single projection registration.
/// </summary>
/// <param name="Projection">Json representation of <see cref="ProjectionDefinition"/> to register.</param>
/// <param name="Pipeline">Json representation of <see cref="ProjectionPipelineDefinition"/> to associate.</param>
public record ProjectionRegistration(JsonNode Projection, JsonNode Pipeline);
