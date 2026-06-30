// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model that mixes an explicitly-subscribed mapping (<see cref="OpenedAt"/> via
/// <c>[SetFromContext]</c>) with a global <c>[FromAll]</c> mapping (<see cref="LastUpdatedAt"/>), used to
/// verify the in-memory harness fires <c>[FromAll]</c> for every event the same way the real runtime does.
/// </summary>
/// <param name="Id">Thing identifier.</param>
/// <param name="Name">The thing's name.</param>
/// <param name="OpenedAt">When the thing was opened — set from the opening event's context.</param>
/// <param name="LastUpdatedAt">When the thing was last touched by ANY event — projected from all events.</param>
[Passive]
[FromEvent<ThingOpened>]
[FromEvent<ThingTouched>]
public record ThingSummary(
    Guid Id,
    string Name,

    [property: SetFromContext<ThingOpened>(nameof(EventContext.Occurred))]
    DateTimeOffset OpenedAt,

    [property: FromAll(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset? LastUpdatedAt);
