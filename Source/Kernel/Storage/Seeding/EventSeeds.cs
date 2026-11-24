// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Seeding;

/// <summary>
/// Represents the events to be seeded.
/// </summary>
/// <param name="ByEventType">Events grouped by event type.</param>
/// <param name="ByEventSource">Events grouped by event source.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Seeding.EventSeeds")]
public record EventSeeds(
    [Id(0)] IDictionary<EventTypeId, IEnumerable<SeededEventEntry>> ByEventType,
    [Id(1)] IDictionary<EventSourceId, IEnumerable<SeededEventEntry>> ByEventSource);
