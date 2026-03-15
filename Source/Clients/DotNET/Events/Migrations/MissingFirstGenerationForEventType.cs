// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Exception that gets thrown when an event type is registered at generation 2 or higher
/// but no migrator covering the path from generation 1 to the current generation is present.
/// </summary>
/// <param name="eventType">The event type that is missing first-generation coverage.</param>
/// <param name="generation">The current generation of the event type.</param>
public class MissingFirstGenerationForEventType(Type eventType, uint generation)
    : Exception($"Event type '{eventType.Name}' is at generation {generation} but there is no migrator that starts from generation 1. Migrators must cover every step from generation 1 to the current generation.");
