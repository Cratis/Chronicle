// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents the event that gets appended to the system event sequence of the System event store when an alert
/// incident ends.
/// </summary>
/// <remarks>
/// It carries the target as well as the identifier so that a consumer can scope it to an event store without looking
/// the incident up. The time it was cleared is the occurred time of the event.
/// </remarks>
/// <param name="IncidentId">The <see cref="IncidentId"/> of the incident.</param>
/// <param name="Condition">The <see cref="AlertConditionKind"/> the incident had when it ended.</param>
/// <param name="Reason">The <see cref="AlertClearedReason"/> the incident ended for.</param>
/// <param name="Target">The <see cref="AlertTarget"/> the incident was about.</param>
[EventType]
public record AlertCleared(
    IncidentId IncidentId,
    AlertConditionKind Condition,
    AlertClearedReason Reason,
    AlertTarget Target);
