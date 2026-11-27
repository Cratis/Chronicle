// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage;

public class when_resolving_namespace_incarnations : given.an_event_store_storage
{
    NamespaceIncarnations _incarnations;
    IHaveNamespaceIncarnations _resolved;
    INamespaceMarkerStorage _marker;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(_eventStore).Returns(_storage);
        _incarnations = new(storage, Substitute.For<IGrainFactory>(), Options.Create(new ChronicleOptions { Features = new() { NamespaceIncarnations = true } }));
    }

    void Because()
    {
        _resolved = _incarnations.GetStorage(_eventStore);
        _marker = _resolved.GetNamespaceMarker("production");
    }

    [Fact] void should_advertise_the_capability() => _storage.ShouldBeAssignableFrom<IHaveNamespaceIncarnations>();
    [Fact] void should_resolve_the_provider_from_core() => _resolved.ShouldEqual(_storage);
    [Fact] void should_expose_the_existing_lifecycle_storage() => _resolved.NamespaceLifecycles.ShouldBeOfExactType<NamespaceLifecycleStorage>();
    [Fact] void should_expose_the_existing_marker_storage() => _marker.ShouldBeOfExactType<NamespaceMarkerStorage>();
    [Fact] void should_only_obtain_the_namespace_database_handle() => _eventStoreDatabase.NamespaceDatabaseCalls.ShouldEqual(1);
}
