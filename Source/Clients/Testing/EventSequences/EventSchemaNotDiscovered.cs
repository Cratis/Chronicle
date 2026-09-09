// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// The exception that is thrown when a scenario cannot resolve an event generation's schema.
/// </summary>
/// <param name="eventTypeId">The event type identifier.</param>
/// <param name="generation">The requested generation.</param>
public sealed class EventSchemaNotDiscovered(string eventTypeId, uint generation)
    : Exception($"Event type '{eventTypeId}' generation '{generation}' was not discovered. The scenario cannot substitute another generation's schema or compliance metadata.");
