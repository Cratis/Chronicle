// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Seeding;

/// <summary>
/// Represents the events to be seeded.
/// </summary>
/// <param name="ByEventType">Events grouped by event type.</param>
/// <param name="ByEventSource">Events grouped by event source.</param>
public record EventSeeds(
    IDictionary<EventTypeId, IEnumerable<SeededEventEntry>> ByEventType,
    IDictionary<EventSourceId, IEnumerable<SeededEventEntry>> ByEventSource);
