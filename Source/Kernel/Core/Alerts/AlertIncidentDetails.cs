// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents an open incident.
/// </summary>
/// <param name="Id">Incident identity.</param>
/// <param name="Target">Affected target.</param>
/// <param name="Condition">Recorded condition.</param>
/// <param name="Severity">Recorded severity.</param>
/// <param name="Evidence">Recorded evidence.</param>
/// <param name="RaisedAt">Persisted raise time.</param>
/// <param name="LastChangedAt">Last transition time.</param>
/// <param name="RaisedSequenceNumber">Raise position.</param>
/// <param name="LastTransitionSequenceNumber">Last applied position.</param>
public record AlertIncidentDetails(
    IncidentId Id,
    AlertTarget Target,
    AlertConditionKind Condition,
    AlertSeverity Severity,
    AlertEvidence Evidence,
    DateTimeOffset RaisedAt,
    DateTimeOffset LastChangedAt,
    EventSequenceNumber RaisedSequenceNumber,
    EventSequenceNumber LastTransitionSequenceNumber);
