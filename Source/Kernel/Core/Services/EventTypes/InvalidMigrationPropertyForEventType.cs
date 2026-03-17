// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// The exception that gets thrown when a migration references a property that does not exist in the expected schema.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidMigrationPropertyForEventType"/> class.
/// </remarks>
/// <param name="eventTypeId">The identifier of the event type.</param>
/// <param name="propertyName">The property name that was referenced.</param>
/// <param name="generation">The generation the property was expected to belong to.</param>
/// <param name="direction">Whether this was an upcast or downcast migration.</param>
public class InvalidMigrationPropertyForEventType(string eventTypeId, string propertyName, uint generation, string direction)
    : Exception($"Event type '{eventTypeId}' migration {direction} references property '{propertyName}' which does not exist in the generation {generation} schema.");
