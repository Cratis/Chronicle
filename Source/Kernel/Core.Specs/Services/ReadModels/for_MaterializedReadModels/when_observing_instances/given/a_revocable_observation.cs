// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_observing_instances.given;

public class a_revocable_observation : for_ReadModels.given.all_dependencies
{
    protected readonly TaskCompletionSource _subscribed = new();
    protected readonly TaskCompletionSource _releaseStarted = new();
    protected readonly TaskCompletionSource _countStarted = new();
    protected readonly TaskCompletionSource<IEnumerable<ExpandoObject>> _release = new();
    protected readonly TaskCompletionSource<ReadModelInstances> _count = new();
    protected readonly TaskCompletionSource<Exception> _ended = new();
    protected readonly Subject<IEnumerable<ExpandoObject>> _source = new();
    protected ISinkProvenanceStorage _cached;
    protected int _delivered;
    SinkProvenanceCache _cache;
    IDisposable _subscription;

    async Task Establish()
    {
        var raw = new InMemorySinkProvenanceStorage();
        await raw.CompleteInventory(false);
        _cache = new(new global::Cratis.Chronicle.ReadModels.for_SinkProvenanceCache.given.ManualClock());
        _cached = _cache.Bind("database", () => raw);
        var binding = new ReadModelReadBinding(_sink, null) { Lease = await _cached.GetLease() };
        _readBindings.Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>(), Arg.Any<ReadModelContainerName?>()).Returns(binding);
        _sink.ObserveInstances(Arg.Any<ReadModelContainerName?>(), 0, 10).Returns(Observable.Defer(() =>
        {
            _subscribed.TrySetResult();
            return _source;
        }));
        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), 0, 10).Returns(_ => { _countStarted.TrySetResult(); return _count.Task; });
        _complianceHelper.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<IEnumerable<ExpandoObject>>())
            .Returns(_ => { _releaseStarted.TrySetResult(); return _release.Task; });
        var service = new MaterializedReadModels(_grainFactory, _readBindings, _complianceHelper, NullLogger<MaterializedReadModels>.Instance, NamespaceCasingContext.Create());
        _subscription = service.ObserveInstances(new()
        {
            EventStore = "test-store", Namespace = "test-namespace", ReadModel = _readModelDefinition.Identifier, PageSize = 10
        }).Subscribe(_ => _delivered++, error => _ended.TrySetResult(error));
        await _subscribed.Task;
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _source.Dispose();
        _cache?.Dispose();
    }
}
