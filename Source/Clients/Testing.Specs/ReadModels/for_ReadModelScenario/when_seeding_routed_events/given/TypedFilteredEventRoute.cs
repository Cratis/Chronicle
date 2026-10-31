// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Counts events delivered through a declared event source and stream filter.
/// </summary>
/// <param name="Count">The number of delivered events.</param>
/// <param name="Position">The latest seeded sequence position.</param>
public record TypedFilteredEventRoute(int Count, EventSequenceNumber Position);
