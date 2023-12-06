// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Observation.Replaying;

/// <summary>
/// Represents a reason for a replay candidate when event types have changed.
/// </summary>
/// <param name="CurrentEventTypes">Collection of the current event types.</param>
/// <param name="NewEventTypes">Collection of the new event types.</param>
public record EventTypesChangedReplayCandidateReason(
    IEnumerable<EventType> CurrentEventTypes,
    IEnumerable<EventType> NewEventTypes)
    : ReplayCandidateReason(ReplayCandidateReasonType.EventTypesChanged);
