// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Captures the route and count of delivered events.
/// </summary>
/// <param name="SourceType">The event source type.</param>
/// <param name="StreamType">The event stream type.</param>
/// <param name="StreamId">The event stream identifier.</param>
/// <param name="Count">The number of delivered events.</param>
/// <param name="Position">The latest seeded sequence position.</param>
public record ReducedEventRoute(EventSourceType SourceType, EventStreamType StreamType, EventStreamId StreamId, int Count, EventSequenceNumber Position);
