// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

/// <summary>
/// Projects the original context alongside the recorded value.
/// </summary>
/// <param name="Id">The resolved key.</param>
/// <param name="Value">The recorded value.</param>
/// <param name="Position">The original sequence number.</param>
/// <param name="Occurred">The original occurrence time.</param>
/// <param name="SourceType">The original source type.</param>
/// <param name="StreamType">The original stream type.</param>
/// <param name="StreamId">The original stream identifier.</param>
/// <param name="CorrelationId">The original correlation identifier.</param>
[FromEvent<ScenarioContextRecorded>]
public record ScenarioContextProjection(
    string Id,
    string Value,
    [SetFromContext<ScenarioContextRecorded>(nameof(EventContext.SequenceNumber))] EventSequenceNumber Position,
    [SetFromContext<ScenarioContextRecorded>(nameof(EventContext.Occurred))] DateTimeOffset Occurred,
    [SetFromContext<ScenarioContextRecorded>(nameof(EventContext.EventSourceType))] EventSourceType SourceType,
    [SetFromContext<ScenarioContextRecorded>(nameof(EventContext.EventStreamType))] EventStreamType StreamType,
    [SetFromContext<ScenarioContextRecorded>(nameof(EventContext.EventStreamId))] EventStreamId StreamId,
    [SetFromContext<ScenarioContextRecorded>(nameof(EventContext.CorrelationId))] CorrelationId CorrelationId);
