// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents an alert incident that has been raised and not yet cleared.
/// </summary>
/// <param name="Id">The <see cref="IncidentId"/> of the incident.</param>
/// <param name="Condition">The <see cref="AlertConditionKind"/> the incident currently has, which changes when it is escalated.</param>
/// <param name="Severity">The <see cref="AlertSeverity"/> the incident currently has.</param>
/// <param name="Partition">The <see cref="AlertPartition"/> the incident is about, or <see cref="AlertPartition.None"/>.</param>
public record OpenIncident(IncidentId Id, AlertConditionKind Condition, AlertSeverity Severity, AlertPartition Partition);
