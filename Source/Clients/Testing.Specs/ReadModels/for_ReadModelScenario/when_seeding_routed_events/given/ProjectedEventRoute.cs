// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Projects the route of the latest recorded event.
/// </summary>
/// <param name="Id">The event source identifier.</param>
/// <param name="Value">The recorded value.</param>
/// <param name="SourceType">The event source type.</param>
/// <param name="StreamType">The event stream type.</param>
/// <param name="StreamId">The event stream identifier.</param>
/// <param name="Position">The seeded sequence position.</param>
[FromEvent<RoutedEventRecorded>]
public record ProjectedEventRoute(
    Guid Id,
    string Value,
    [SetFromContext<RoutedEventRecorded>(nameof(EventContext.EventSourceType))] EventSourceType SourceType,
    [SetFromContext<RoutedEventRecorded>(nameof(EventContext.EventStreamType))] EventStreamType StreamType,
    [SetFromContext<RoutedEventRecorded>(nameof(EventContext.EventStreamId))] EventStreamId StreamId,
    [SetFromContext<RoutedEventRecorded>(nameof(EventContext.SequenceNumber))] EventSequenceNumber Position);
