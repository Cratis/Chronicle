// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Replaying;

/// <summary>
/// Represents a reason for a replay candidate when event types have changed.
/// </summary>
/// <param name="CurrentEventTypes">Collection of the current event types.</param>
/// <param name="NewEventTypes">Collection of the new event types.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Observation.Replaying.EventTypesChangedReplayCandidateReason")]
public record EventTypesChangedReplayCandidateReason(
    [Id(0)] IEnumerable<EventType> CurrentEventTypes,
    [Id(1)] IEnumerable<EventType> NewEventTypes)
    : ReplayCandidateReason(ReplayCandidateReasonType.EventTypesChanged);
