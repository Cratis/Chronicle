// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Domain.Projections;

/// <summary>
/// Represents the payload for performing an immediate projection.
/// </summary>
/// <param name="ProjectionId">The unique identifier of the projection.</param>
/// <param name="EventSequenceId">The event sequence to project from.</param>
/// <param name="ModelKey">The key of the model to project.</param>
/// <param name="Events">Events to apply.</param>
public record ImmediateProjectionWithEventsToApply(
    ProjectionId ProjectionId,
    EventSequenceId EventSequenceId,
    ModelKey ModelKey,
    IEnumerable<EventToApply> Events);
