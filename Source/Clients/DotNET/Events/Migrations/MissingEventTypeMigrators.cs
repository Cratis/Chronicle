// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Exception that gets thrown when an event type is not at generation 1 and has no migrators defined.
/// </summary>
/// <param name="eventType">The event type that is missing migrators.</param>
/// <param name="generation">The current generation of the event type.</param>
public class MissingEventTypeMigrators(Type eventType, uint generation)
    : Exception($"Event type '{eventType.Name}' is at generation {generation} but has no migrators defined. Event types beyond generation 1 must have migrators.");
