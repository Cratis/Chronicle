// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.Copy;

/// <summary>
/// Represents the request for copying events from one event source to another.
/// </summary>
/// <param name="EventSequenceId">The event sequence holding both event sources.</param>
/// <param name="SourceId">The event source to copy events from.</param>
/// <param name="TargetId">The event source to append the copies to.</param>
/// <param name="EventTypes">The event types to copy. When empty, events of every type are copied.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.EventSequences.Copy.CopyEventSourceEventsRequest")]
public record CopyEventSourceEventsRequest(
    [Id(0)] EventSequenceId EventSequenceId,
    [Id(1)] EventSourceId SourceId,
    [Id(2)] EventSourceId TargetId,
    [Id(3)] IReadOnlyCollection<EventTypeId> EventTypes) : IJobRequest;
