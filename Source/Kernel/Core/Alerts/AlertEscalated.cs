// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents the event that gets appended to the system event sequence of the System event store when an open alert
/// incident becomes more serious, for example when a failing partition runs out of retries.
/// </summary>
/// <remarks>
/// The incident keeps its identifier. The condition and severity are the new ones, so the current state of an incident
/// is the last transition recorded for it. The time it was escalated is the occurred time of the event.
/// </remarks>
/// <param name="IncidentId">The <see cref="IncidentId"/> of the incident.</param>
/// <param name="Condition">The <see cref="AlertConditionKind"/> the incident was escalated to.</param>
/// <param name="Severity">The <see cref="AlertSeverity"/> it was escalated to.</param>
/// <param name="Target">The <see cref="AlertTarget"/> the incident is about.</param>
/// <param name="Evidence">The <see cref="AlertEvidence"/> known when it was escalated.</param>
[EventType]
public record AlertEscalated(
    IncidentId IncidentId,
    AlertConditionKind Condition,
    AlertSeverity Severity,
    AlertTarget Target,
    AlertEvidence Evidence);
