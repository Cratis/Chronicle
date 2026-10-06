// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents one recorded incident mutation.
/// </summary>
/// <param name="Kind">Transition kind.</param>
/// <param name="Id">Incident identity.</param>
/// <param name="Target">Recorded target.</param>
/// <param name="Condition">Recorded condition.</param>
/// <param name="Severity">Recorded severity.</param>
/// <param name="Evidence">Recorded evidence.</param>
/// <param name="ClearedReason">Recorded clear reason.</param>
/// <param name="Occurred">Persisted event time.</param>
/// <param name="SequenceNumber">Persisted event position.</param>
public record AlertIncidentTransition(
    AlertIncidentTransitionKind Kind,
    IncidentId Id,
    AlertIncidentTarget Target,
    AlertConditionKind Condition,
    AlertSeverity? Severity,
    AlertIncidentEvidence? Evidence,
    AlertClearedReason? ClearedReason,
    DateTimeOffset Occurred,
    EventSequenceNumber SequenceNumber);
