// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnations.given;

public class incarnation_routing : Specification
{
    protected static readonly EventStoreName _eventStore = "store";
    protected static readonly EventStoreNamespaceName _namespace = "Tenant";
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected IHaveNamespaceIncarnations _capability;
    protected INamespaceLifecycleStorage _lifecycle;
    protected INamespaceStorage _names;
    protected IGrainFactory _grains;
    protected INamespaceIncarnation _owner;
    protected NamespaceIncarnations _routing;
    protected NamespaceLifecycle _state;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _store = Substitute.For<IEventStoreStorage, IHaveNamespaceIncarnations>();
        _capability = (IHaveNamespaceIncarnations)_store;
        _lifecycle = Substitute.For<INamespaceLifecycleStorage>();
        _lifecycle.Get(Arg.Any<EventStoreNamespaceName>()).Returns(_ => Task.FromResult<NamespaceLifecycle?>(_state));
        _capability.NamespaceLifecycles.Returns(_lifecycle);
        _names = Substitute.For<INamespaceStorage>();
        _names.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([new(_namespace, DateTimeOffset.UtcNow)]));
        _store.Namespaces.Returns(_names);
        _storage.GetEventStore(_eventStore).Returns(_store);
        _grains = Substitute.For<IGrainFactory>();
        _owner = Substitute.For<INamespaceIncarnation>();
        _grains.GetGrain<INamespaceIncarnation>(new NamespaceIncarnationKey(_eventStore.Value, _namespace.Value).ToString()).Returns(_owner);
        _routing = new(_storage, _grains, Options.Create(new ChronicleOptions { Features = new() { NamespaceIncarnations = true } }));
    }
}
