// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents the event that gets appended to the system event sequence of the System event store when an alert
/// incident is raised.
/// </summary>
/// <remarks>
/// Declared without <c language="csharp">[AllEventStores]</c>, like <see cref="EventStoreAdded"/>, so it exists only
/// in the System event store. The affected event store is carried in <paramref name="Target"/>. The time it was raised
/// is the occurred time of the event.
/// </remarks>
/// <param name="IncidentId">The <see cref="IncidentId"/> of the incident.</param>
/// <param name="Condition">The <see cref="AlertConditionKind"/> the incident was raised for.</param>
/// <param name="Severity">The <see cref="AlertSeverity"/> it was raised with.</param>
/// <param name="Target">The <see cref="AlertTarget"/> the incident is about.</param>
/// <param name="Evidence">The <see cref="AlertEvidence"/> known when it was raised.</param>
[EventType]
public record AlertRaised(
    IncidentId IncidentId,
    AlertConditionKind Condition,
    AlertSeverity Severity,
    AlertTarget Target,
    AlertEvidence Evidence);
