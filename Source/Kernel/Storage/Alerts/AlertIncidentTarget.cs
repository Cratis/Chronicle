// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Identifies the affected target of an incident.
/// </summary>
/// <param name="EventStore">Affected event store.</param>
/// <param name="Namespace">Affected namespace.</param>
/// <param name="ObserverId">Affected observer.</param>
/// <param name="EventSequenceId">Observed sequence.</param>
/// <param name="Partition">Recorded partition, including the sentinel verbatim.</param>
public record AlertIncidentTarget(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    AlertPartition Partition);
