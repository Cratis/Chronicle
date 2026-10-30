// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_getting_an_invalid_event_store : given.a_storage_with_legacy_event_store_names
{
    Exception?[] _errors;

    void Because() => _errors = new EventStoreName[] { null!, "", " ", EventStoreName.NotSet }
        .Select(name => Catch.Exception(() => _storage.GetEventStore(name))).ToArray();

    [Fact] void should_refuse_each_invalid_name() => _errors.All(_ => _ is InvalidEventStoreName).ShouldBeTrue();
    [Fact] void should_not_read_the_cluster() => _clusterStorage.DidNotReceive().GetEventStores();
    [Fact] void should_not_save_any_event_store() => _clusterStorage.DidNotReceive().SaveEventStore(Arg.Any<EventStoreName>());
    [Fact] void should_not_create_storage() => _clusterStorage.DidNotReceive().CreateStorageForEventStore(Arg.Any<EventStoreName>(), Arg.Any<SinksFactory>());
}
