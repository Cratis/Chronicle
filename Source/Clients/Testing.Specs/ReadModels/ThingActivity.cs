// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a <c>[property: FromEvery]</c> context mapping on a positional record parameter, used
/// to verify the harness fires <c>[FromEvery]</c> — the sibling of <c>[FromAll]</c> — for every event.
/// </summary>
/// <param name="Id">Thing identifier.</param>
/// <param name="Name">The thing's name.</param>
/// <param name="LastActivityAt">When the thing last saw ANY event — projected from every event.</param>
[Passive]
[FromEvent<ThingOpened>]
[FromEvent<ThingTouched>]
public record ThingActivity(
    Guid Id,
    string Name,

    [property: FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset? LastActivityAt);
