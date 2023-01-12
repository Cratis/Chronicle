// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Shared.EventSequences;
using Aksio.Cratis.Shared.Projections;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents the payload for performing an immediate projection.
/// </summary>
/// <param name="ProjectionId">The unique identifier of the projection.</param>
/// <param name="EventSequenceId">The event sequence to project from.</param>
/// <param name="ModelKey">The key of the model to project.</param>
/// <param name="Projection">The projection definition.</param>
public record ImmediateProjection(ProjectionId ProjectionId, EventSequenceId EventSequenceId, ModelKey ModelKey, JsonNode Projection);
