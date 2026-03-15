// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// The exception that gets thrown when there is a gap in the migration chain for an event type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingMigrationForEventTypeGeneration"/> class.
/// </remarks>
/// <param name="eventTypeId">The identifier of the event type.</param>
/// <param name="currentGeneration">The current generation of the event type.</param>
/// <param name="missingFromGeneration">The generation that is missing a migrator.</param>
public class MissingMigrationForEventTypeGeneration(string eventTypeId, uint currentGeneration, uint missingFromGeneration)
    : Exception($"Event type '{eventTypeId}' is at generation {currentGeneration} but is missing a migrator from generation {missingFromGeneration} to {missingFromGeneration + 1}.");
