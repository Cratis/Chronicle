// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation;

/// <summary>
/// Generation 1 of OrderPlaced — carries a single Description field.
/// </summary>
/// <param name="Description">The order description.</param>
[EventType("OrderPlaced", generation: 1)]
public record OrderPlacedV1(string Description);
