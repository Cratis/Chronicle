// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Kernel.Integration.Alerts.for_AlertIncidents.given;

public class an_incidents_reactor(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
    protected IGrainFactory _factory;
    protected IStorage _storage;
    protected IAlertIncidentsStorage _incidents;
    protected IEventSerializer _serializer;
    protected AlertRaised _raised;
    protected AlertIncident? _row;
    protected EventSequenceNumber _last = EventSequenceNumber.Max;
    readonly TaskCompletionSource<ObserverState> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable CA2213 // The specification lifecycle disposes this subscription in Destroy().
    IDisposable _subscription;
#pragma warning restore CA2213
    volatile bool _waiting;

    void Establish()
    {
        _factory = Services.GetRequiredService<IGrainFactory>();
        _storage = Services.GetRequiredService<IStorage>();
        _serializer = Services.GetRequiredService<IEventSerializer>();
        var ns = _storage.GetEventStore(Concepts.EventStoreName.System).GetNamespace(Concepts.EventStoreNamespaceName.Default);
        _incidents = ns.AlertIncidents;
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _raised = new(new IncidentId(Guid.NewGuid()), AlertConditionKind.PartitionFailing, AlertSeverity.Warning, new("affected", "tenant", "observer", Concepts.EventSequences.EventSequenceId.Log, "partition"), new(3, now, now, Concepts.Observation.FailureKind.Handling, "recorded"));

        // Subscribe before append/registration; completion is observer state, never an elapsed delay.
        _subscription = ns.Observers.ObserveAll().Subscribe(
            states =>
        {
            var observer = states.FirstOrDefault(state => state.Identifier == AlertIncidentsReactor.ObserverKey.ObserverId);
            if (_waiting && observer is not null && observer.LastHandledEventSequenceNumber >= _last) _completed.TrySetResult(observer);
        },
            error => _completed.TrySetException(error));
    }

    protected async Task Append(object recorded)
    {
        var result = await _factory.GetSystemEventSequence().Append(EventSourceType.Default, "incident-spec-observer", EventStreamType.All, EventStreamId.Default, recorded.GetType().GetEventType(), _serializer.Serialize(recorded), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
        result.IsSuccess.ShouldBeTrue();
        _last = result.SequenceNumber;
    }

    protected async Task WaitForMaterialization()
    {
        _waiting = true;
        var observer = await _factory.GetGrain<IObserver>(AlertIncidentsReactor.ObserverKey).GetState();
        if (observer.LastHandledEventSequenceNumber >= _last) _completed.TrySetResult(observer);
        await _completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        _row = await _incidents.GetOpen(new("affected", "tenant"), _raised.IncidentId);
    }

    void Destroy() => _subscription.Dispose();
}
