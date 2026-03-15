// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Exception that gets thrown when an event type migration chain has a gap — i.e. there is no
/// migrator that transforms from generation N to generation N+1 for some N in the chain.
/// </summary>
/// <param name="eventType">The event type with a gap in its migration chain.</param>
/// <param name="fromGeneration">The source generation that has no corresponding migrator.</param>
/// <param name="toGeneration">The expected target generation that is not covered.</param>
public class MissingMigrationForEventTypeGeneration(Type eventType, uint fromGeneration, uint toGeneration)
    : Exception($"Event type '{eventType.Name}' is missing a migrator from generation {fromGeneration} to generation {toGeneration}. Generations must be sequential with no gaps in the migration chain.");
