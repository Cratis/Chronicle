// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_getting_an_event_store_without_a_name : given.a_storage
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _storage.GetEventStore(new EventStoreName(string.Empty)));

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<InvalidEventStoreName>();
    [Fact] void should_not_register_it() => _clusterStorage.SavedEventStores.ShouldBeEmpty();
    [Fact] void should_not_create_storage_for_it() => _clusterStorage.CreatedStorages.ShouldBeEmpty();
}
