// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Alerts;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Alerts.for_AlertIncidentsStorage;

public class when_clearing_kernel_storage : Specification
{
    Storage _storage;
    EventStoreStorages _registry;
    AlertIncidentStoragePage _page;
    EventSequenceNumber _tail;

    async Task Establish()
    {
        _registry = new(Substitute.For<IInstancesOf<ISinkFactory>>(), Substitute.For<Orleans.Storage.IJobsStorage>());
        _storage = new(_registry, Substitute.For<ISystemStorage>());
        var ns = _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default);
        await ns.AlertIncidents.Apply(new(AlertIncidentTransitionKind.Raised, new IncidentId(Guid.NewGuid()), new("affected", "tenant", "observer", EventSequenceId.Log, "partition"), AlertConditionKind.PartitionFailing, AlertSeverity.Warning, new(3, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, FailureKind.Handling, "recorded"), null, DateTimeOffset.UnixEpoch, 1UL));
        await ns.GetEventSequence(EventSequenceId.System).Append(
            1UL,
            EventSourceType.Default,
            "observer",
            EventStreamType.All,
            EventStreamId.Default,
            EventType.Unknown,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UnixEpoch,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new() },
            new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = EventHash.NotSet });
    }

    async Task Because()
    {
        _storage.Clear();
        var ns = _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default);
        _page = await ns.AlertIncidents.EnumerateOpen(null, 100);
        _tail = await ns.GetEventSequence(EventSequenceId.System).GetTailSequenceNumber();
    }

    void Destroy() => _registry.Dispose();

    [Fact] void should_remove_incident_rows_with_system_history() => _page.Items.ShouldBeEmpty();
    [Fact] void should_remove_system_sequence_history() => _tail.ShouldEqual(EventSequenceNumber.Unavailable);
}
