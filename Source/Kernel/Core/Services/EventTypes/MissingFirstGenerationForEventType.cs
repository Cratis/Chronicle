// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// The exception that gets thrown when an event type has no migrator starting from generation 1.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingFirstGenerationForEventType"/> class.
/// </remarks>
/// <param name="eventTypeId">The identifier of the event type.</param>
/// <param name="generation">The current generation of the event type.</param>
public class MissingFirstGenerationForEventType(string eventTypeId, uint generation)
    : Exception($"Event type '{eventTypeId}' is at generation {generation} but has no migrator starting from generation 1. Every event type must begin at generation 1.");
