// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_getting_an_unregistered_event_store_with_a_name_that_is_not_allowed : Specification
{
    IClusterStorage _clusterStorage;
    IResourceNameReservations _reservations;
    Storage _storage;
    Exception _error;

    void Establish()
    {
        _clusterStorage = Substitute.For<IClusterStorage>();
        _clusterStorage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([new EventStoreName("orders")]));
        _reservations = Substitute.For<IResourceNameReservations>();
        var snapshot = ResourceNameSnapshot.NotReady();
        snapshot.Publish([], []);
        var gate = new ResourceNameGate(snapshot, _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
        _storage = new Storage(_clusterStorage, Substitute.For<ISystemStorage>(), new KnownInstancesOf<ISinkFactory>([]), gate);
    }

    void Because() => _error = Catch.Exception(() => _storage.GetEventStore("a#b"));

    [Fact] void should_refuse_the_name() => _error.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_only_have_read_the_registry() => _clusterStorage.Received(1).GetEventStores();
    [Fact] void should_not_register_the_event_store() => _clusterStorage.DidNotReceive().SaveEventStore(Arg.Any<EventStoreName>());
    [Fact] void should_not_create_any_database_or_collection() => _clusterStorage.DidNotReceive().CreateStorageForEventStore(Arg.Any<EventStoreName>(), Arg.Any<SinksFactory>());
    [Fact] void should_not_reserve_the_name() => _reservations.DidNotReceive().Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
}
