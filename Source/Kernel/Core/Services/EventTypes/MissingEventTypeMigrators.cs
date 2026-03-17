// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// The exception that gets thrown when an event type is at generation 2 or higher but has no migrators defined.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingEventTypeMigrators"/> class.
/// </remarks>
/// <param name="eventTypeId">The identifier of the event type.</param>
/// <param name="generation">The current generation of the event type.</param>
public class MissingEventTypeMigrators(string eventTypeId, uint generation)
    : Exception($"Event type '{eventTypeId}' is at generation {generation} but has no migrators defined. Every event type at generation 2 or higher must have a complete migration chain starting from generation 1.");
