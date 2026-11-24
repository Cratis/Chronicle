// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.EventStores.for_EventStoreStorage;

public class when_resolving_namespace_incarnations : Specification
{
    IDatabase _database;
    EventStoreStorage _storage;
    NamespaceIncarnations _incarnations;
    IHaveNamespaceIncarnations _resolved;
    INamespaceMarkerStorage _marker;

    void Establish()
    {
        _database = Substitute.For<IDatabase>();
        _storage = new(
            "orders",
            _database,
            Substitute.For<IInstancesOf<ISinkFactory>>(),
            Substitute.For<Choreography.Storage.IJobsStorage>(),
            new JsonSerializerOptions(),
            Substitute.For<Json.IExpandoObjectConverter>());
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("orders").Returns(_storage);
        _incarnations = new(storage, Substitute.For<IGrainFactory>(), Options.Create(new ChronicleOptions { Features = new() { NamespaceIncarnations = true } }));
    }

    void Because()
    {
        _resolved = _incarnations.GetStorage("orders");
        _marker = _resolved.GetNamespaceMarker("production");
    }

    [Fact] void should_advertise_the_capability() => _storage.ShouldBeAssignableFrom<IHaveNamespaceIncarnations>();
    [Fact] void should_resolve_the_provider_from_core() => _resolved.ShouldEqual(_storage);
    [Fact] void should_expose_the_existing_lifecycle_storage() => _resolved.NamespaceLifecycles.ShouldBeOfExactType<NamespaceLifecycleStorage>();
    [Fact] void should_expose_the_existing_marker_storage() => _marker.ShouldBeOfExactType<NamespaceMarkerStorage>();
    [Fact] void should_not_open_or_provision_a_database_when_resolving() => _database.ReceivedCalls().ShouldBeEmpty();
}
