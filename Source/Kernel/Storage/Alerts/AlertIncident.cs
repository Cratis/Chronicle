// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents a retained incident row, including closed tombstones.
/// </summary>
/// <param name="Id">Incident identity.</param>
/// <param name="Target">Affected target.</param>
/// <param name="Condition">Recorded condition.</param>
/// <param name="Severity">Severity, absent only for an orphan clear.</param>
/// <param name="Evidence">Evidence, absent only for an orphan clear.</param>
/// <param name="RaisedAt">Raise time, absent for an orphan clear.</param>
/// <param name="RaisedSequenceNumber">Raise position, absent for an orphan clear.</param>
/// <param name="LastChangedAt">Last applied transition time.</param>
/// <param name="LastTransitionSequenceNumber">Last applied transition position.</param>
/// <param name="IsOpen">Whether the incident is open.</param>
/// <param name="ClearedReason">Recorded clear reason.</param>
public record AlertIncident(
    IncidentId Id,
    AlertIncidentTarget Target,
    AlertConditionKind Condition,
    AlertSeverity? Severity,
    AlertIncidentEvidence? Evidence,
    DateTimeOffset? RaisedAt,
    EventSequenceNumber? RaisedSequenceNumber,
    DateTimeOffset LastChangedAt,
    EventSequenceNumber LastTransitionSequenceNumber,
    bool IsOpen,
    AlertClearedReason? ClearedReason);
