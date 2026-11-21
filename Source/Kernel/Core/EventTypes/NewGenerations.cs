// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the generations of an event type that are not yet stored, and whether the event type itself is
/// already stored - which is what decides between an <see cref="EventTypeAdded"/> and an
/// <see cref="EventTypeGenerationAdded"/> system event.
/// </summary>
/// <param name="EventTypeId">The <see cref="EventTypeId"/> the generations belong to.</param>
/// <param name="EventTypeAlreadyStored">Whether the event type is already stored.</param>
/// <param name="Generations">The generations that are not yet stored, with their schema as it came in.</param>
sealed record NewGenerations(
    EventTypeId EventTypeId,
    bool EventTypeAlreadyStored,
    IReadOnlyList<(EventTypeGeneration Generation, string Schema)> Generations);
