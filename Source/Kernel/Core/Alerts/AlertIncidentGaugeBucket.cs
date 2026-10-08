// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Identifies one series of the open incident gauge.
/// </summary>
/// <param name="EventStore">Affected event store.</param>
/// <param name="Namespace">Affected namespace.</param>
/// <param name="ObserverId">Affected observer.</param>
/// <param name="EventSequenceId">Affected event sequence.</param>
/// <param name="Condition">Recorded condition.</param>
/// <param name="Severity">Recorded severity.</param>
public sealed record AlertIncidentGaugeBucket(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    AlertConditionKind Condition,
    AlertSeverity Severity);
