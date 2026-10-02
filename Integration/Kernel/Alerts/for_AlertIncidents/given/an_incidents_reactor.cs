// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;
using MongoDB.Driver;

using stored_observer_state = Cratis.Chronicle.Storage.MongoDB.Observation.ObserverState;

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
    readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable CA2213 // The specification lifecycle disposes the watcher in Destroy().
    readonly CancellationTokenSource _watchCancellation = new();
    IChangeStreamCursor<ChangeStreamDocument<stored_observer_state>>? _cursor;
#pragma warning restore CA2213
    Task _watch = Task.CompletedTask;
    volatile bool _waiting;

    async Task Establish()
    {
        _factory = Services.GetRequiredService<IGrainFactory>();
        _storage = Services.GetRequiredService<IStorage>();
        _serializer = Services.GetRequiredService<IEventSerializer>();
        // The fixture omits the server startup task; bootstrap the kernel artifacts this spec exercises.
        await Services.GetRequiredService<EventTypes.IEventTypes>().DiscoverAndRegister(Concepts.EventStoreName.System);
        await Services.GetRequiredService<Observation.Reactors.Kernel.IReactors>()
            .DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default);
        var ns = _storage.GetEventStore(Concepts.EventStoreName.System).GetNamespace(Concepts.EventStoreNamespaceName.Default);
        _incidents = ns.AlertIncidents;
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _raised = new(new IncidentId(Guid.NewGuid()), AlertConditionKind.PartitionFailing, AlertSeverity.Warning, new("affected", "tenant", "observer", Concepts.EventSequences.EventSequenceId.Log, "partition"), new(3, now, now, Concepts.Observation.FailureKind.Handling, "recorded"));

        // This Orleans-only host does not configure Arc's observable-query runtime. Watch the
        // persisted observer state directly, starting before append/registration to avoid missing delivery.
        var collection = Services.GetRequiredService<Storage.MongoDB.IDatabase>()
            .GetEventStoreDatabase(Concepts.EventStoreName.System)
            .GetNamespaceDatabase(Concepts.EventStoreNamespaceName.Default)
            .GetObserverStateCollection();
        _cursor = await collection.WatchAsync(
            new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup },
            _watchCancellation.Token);
        _watch = WatchMaterialization();
    }

    protected async Task Append(object recorded)
    {
        var result = await _factory.GetSystemEventSequence().Append(EventSourceType.Default, "incident-spec-observer", EventStreamType.All, EventStreamId.Default, recorded.GetType().GetEventType(), _serializer.Serialize(recorded), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
        result.Errors.ShouldBeEmpty();
        result.IsSuccess.ShouldBeTrue();
        _last = result.SequenceNumber;
    }

    protected async Task WaitForMaterialization()
    {
        _waiting = true;
        var observer = await _factory.GetGrain<IObserver>(AlertIncidentsReactor.ObserverKey).GetState();
        if (observer.LastHandledEventSequenceNumber.IsActualValue && observer.LastHandledEventSequenceNumber >= _last) _completed.TrySetResult();
        await _completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        _row = await _incidents.GetOpen(new("affected", "tenant"), _raised.IncidentId);
    }

    async Task WatchMaterialization()
    {
        try
        {
            while (await _cursor!.MoveNextAsync(_watchCancellation.Token))
            {
                foreach (var change in _cursor.Current)
                {
                    var observer = change.FullDocument;
                    if (_waiting && observer is not null && observer.Id == AlertIncidentsReactor.ObserverKey.ObserverId &&
                        observer.LastHandledEventSequenceNumber.IsActualValue && observer.LastHandledEventSequenceNumber >= _last)
                    {
                        _completed.TrySetResult();
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_watchCancellation.IsCancellationRequested)
        {
            // Destroy cancels the infrastructure watcher after the specification finishes.
        }
        catch (Exception error)
        {
            _completed.TrySetException(error);
        }
    }

    async Task Destroy()
    {
        await _watchCancellation.CancelAsync();
        await _watch;
        _cursor?.Dispose();
        _watchCancellation.Dispose();
    }
}
